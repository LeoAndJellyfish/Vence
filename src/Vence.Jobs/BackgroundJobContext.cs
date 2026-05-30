namespace Vence.Jobs;

public sealed class BackgroundJobContext
{
    private readonly Action<BackgroundJobProgress> _publishProgress;

    internal BackgroundJobContext(Guid jobId, CancellationToken cancellationToken, Action<BackgroundJobProgress> publishProgress)
    {
        JobId = jobId;
        CancellationToken = cancellationToken;
        _publishProgress = publishProgress;
    }

    public Guid JobId { get; }

    public CancellationToken CancellationToken { get; }

    public void ReportProgress(string? message = null, int? percent = null)
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "Progress percent must be between 0 and 100.");
        }

        _publishProgress(new BackgroundJobProgress(JobId, JobStatus.Running, message, percent));
    }
}
