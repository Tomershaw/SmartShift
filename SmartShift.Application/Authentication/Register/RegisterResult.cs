 namespace SmartShift.Application.Authentication.Register;

public class RegisterResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty; // לשימוש עתידי ל-JWT
}
