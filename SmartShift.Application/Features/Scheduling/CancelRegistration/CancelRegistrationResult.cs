namespace SmartShift.Application.Features.Scheduling.CancelRegistration
{
    public enum CancelRegistrationError
    {
        None = 0,
        NotFoundOrNotPending = 1
        // אפשר להרחיב בעתיד: NotAllowed, ValidationFailed, וכו'
    }

    public sealed class CancelRegistrationResult
    {
        public bool Success { get; }
        public CancelRegistrationError Error { get; }

        public CancelRegistrationResult(bool success, CancelRegistrationError error = CancelRegistrationError.None)
        {
            Success = success;
            Error = error;
        }
    }
}
