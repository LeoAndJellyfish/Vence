namespace Vence.Jobs;

public sealed record BackgroundJobProgress(
    Guid JobId,
    JobStatus Status,
    string? Message = null,
    int? Percent = null,
    Exception? Error = null);
