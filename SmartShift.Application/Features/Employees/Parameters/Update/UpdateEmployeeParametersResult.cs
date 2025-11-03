public record UpdateEmployeeParametersResult(
    bool Success,
    string Message,
    Guid EmployeeId,
    string[] Errors
);
