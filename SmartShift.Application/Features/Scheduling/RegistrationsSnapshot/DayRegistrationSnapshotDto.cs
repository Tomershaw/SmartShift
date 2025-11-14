namespace SmartShift.Application.Features.Scheduling.RegistrationsSnapshot;

public sealed record DayRegistrationSnapshotDto(
    DateOnly Date,
    int RequiredCount,
    int RegisteredCount,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount,
    int CancelledCount
);
