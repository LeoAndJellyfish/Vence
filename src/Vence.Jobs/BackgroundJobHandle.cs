namespace Vence.Jobs;

public sealed class BackgroundJobHandle : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal BackgroundJobHandle(Guid id, CancellationTokenSource cancellationTokenSource)
    {
        Id = id;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public Guid Id { get; }

    public JobStatus Status { get; internal set; } = JobStatus.Queued;

    public Task Completion { get; internal set; } = Task.CompletedTask;

    public void Cancel()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
}
