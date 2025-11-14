using MediatR;
using System.Collections.Generic;

namespace SmartShift.Application.Features.Scheduling.RegistrationsSnapshot;

public sealed record GetRegistrationsSnapshotQuery(DateOnly FromLocal, DateOnly ToLocal)
    : IRequest<IReadOnlyList<DayRegistrationSnapshotDto>>;
