namespace Vence.Jobs;

public sealed class BackgroundJobQueue : IBackgroundJobQueue, IDisposable
{
    private readonly SemaphoreSlim _concurrencyGate;

    public BackgroundJobQueue(int maxConcurrency = 1)
    {
        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Concurrency must be greater than zero.");
        }

        _concurrencyGate = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public event EventHandler<BackgroundJobProgress>? ProgressChanged;

    public BackgroundJobHandle Enqueue(
        Func<BackgroundJobContext, ValueTask> job,
        BackgroundJobOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        options ??= new BackgroundJobOptions();

        if (options.MaxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Max retries cannot be negative.");
        }

        var jobId = Guid.NewGuid();
        var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var handle = new BackgroundJobHandle(jobId, linkedCancellation);

        Publish(handle, JobStatus.Queued);
        handle.Completion = RunAsync(handle, job, options, linkedCancellation.Token);

        return handle;
    }

    public void Dispose()
    {
        _concurrencyGate.Dispose();
    }

    private async Task RunAsync(
        BackgroundJobHandle handle,
        Func<BackgroundJobContext, ValueTask> job,
        BackgroundJobOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            await _concurrencyGate.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Publish(handle, JobStatus.Canceled);
            return;
        }

        try
        {
            var attempts = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    Publish(handle, JobStatus.Running);
                    var context = new BackgroundJobContext(
                        handle.Id,
                        cancellationToken,
                        progress => Publish(handle, progress));

                    await job(context);

                    cancellationToken.ThrowIfCancellationRequested();
                    Publish(handle, JobStatus.Succeeded);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Publish(handle, JobStatus.Canceled);
                    return;
                }
                catch (Exception exception) when (attempts < options.MaxRetries)
                {
                    attempts++;
                    Publish(handle, new BackgroundJobProgress(
                        handle.Id,
                        JobStatus.Queued,
                        $"Retrying job after failure ({attempts}/{options.MaxRetries}).",
                        Error: exception));
                }
                catch (Exception exception)
                {
                    Publish(handle, JobStatus.Failed, exception);
                    return;
                }
            }
        }
        finally
        {
            _concurrencyGate.Release();
        }
    }

    private void Publish(BackgroundJobHandle handle, JobStatus status, Exception? error = null)
    {
        Publish(handle, new BackgroundJobProgress(handle.Id, status, Error: error));
    }

    private void Publish(BackgroundJobHandle handle, BackgroundJobProgress progress)
    {
        handle.Status = progress.Status;
        ProgressChanged?.Invoke(this, progress);
    }
}
