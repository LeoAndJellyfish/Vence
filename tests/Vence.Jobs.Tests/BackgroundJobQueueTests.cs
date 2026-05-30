namespace Vence.Jobs.Tests;

public sealed class BackgroundJobQueueTests
{
    [Fact]
    public async Task CancelStopsQueuedOrRunningJob()
    {
        using var queue = new BackgroundJobQueue();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedCancellation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var handle = queue.Enqueue(async context =>
        {
            started.SetResult();
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                observedCancellation.SetResult();
                throw;
            }
        });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        handle.Cancel();

        await observedCancellation.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await handle.Completion.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(JobStatus.Canceled, handle.Status);
    }

    [Fact]
    public async Task EnqueueRetriesTransientFailure()
    {
        using var queue = new BackgroundJobQueue();
        var attempts = 0;

        using var handle = queue.Enqueue(
            _ =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw new InvalidOperationException("temporary");
                }

                return ValueTask.CompletedTask;
            },
            new BackgroundJobOptions { MaxRetries = 1 });

        await handle.Completion.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(2, attempts);
        Assert.Equal(JobStatus.Succeeded, handle.Status);
    }

    [Fact]
    public async Task ReportProgressPublishesProgressEvent()
    {
        using var queue = new BackgroundJobQueue();
        var progressEvent = new TaskCompletionSource<BackgroundJobProgress>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        queue.ProgressChanged += (_, progress) =>
        {
            if (progress.Message == "正在检查语法")
            {
                progressEvent.TrySetResult(progress);
            }
        };

        using var handle = queue.Enqueue(context =>
        {
            context.ReportProgress("正在检查语法", 40);
            return ValueTask.CompletedTask;
        });

        var progress = await progressEvent.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await handle.Completion.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(handle.Id, progress.JobId);
        Assert.Equal(JobStatus.Running, progress.Status);
        Assert.Equal(40, progress.Percent);
        Assert.Equal(JobStatus.Succeeded, handle.Status);
    }

    [Fact]
    public async Task DefaultQueueRunsOneJobAtATime()
    {
        using var queue = new BackgroundJobQueue();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var first = queue.Enqueue(async _ =>
        {
            firstStarted.SetResult();
            await releaseFirst.Task;
        });

        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));

        using var second = queue.Enqueue(_ =>
        {
            secondStarted.SetResult();
            return ValueTask.CompletedTask;
        });

        var earlyStart = await Task.WhenAny(secondStarted.Task, Task.Delay(150));
        Assert.NotSame(secondStarted.Task, earlyStart);

        releaseFirst.SetResult();
        await Task.WhenAll(first.Completion, second.Completion).WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(JobStatus.Succeeded, first.Status);
        Assert.Equal(JobStatus.Succeeded, second.Status);
    }
}
