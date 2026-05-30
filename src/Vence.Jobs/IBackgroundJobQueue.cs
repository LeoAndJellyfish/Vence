namespace Vence.Jobs;

public interface IBackgroundJobQueue
{
    event EventHandler<BackgroundJobProgress>? ProgressChanged;

    BackgroundJobHandle Enqueue(
        Func<BackgroundJobContext, ValueTask> job,
        BackgroundJobOptions? options = null,
        CancellationToken cancellationToken = default);
}
