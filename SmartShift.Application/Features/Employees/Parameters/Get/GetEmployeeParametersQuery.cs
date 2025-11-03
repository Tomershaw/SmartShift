using MediatR;

namespace SmartShift.Application.Features.Employees.Parameters.Get;

public sealed record GetEmployeeParametersQuery(Guid EmployeeId) : IRequest<EmployeeParametersDto?>;
