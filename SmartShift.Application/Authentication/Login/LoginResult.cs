namespace SmartShift.Application.Authentication.Login;

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty; // נשתמש כשנוסיף JWT
}
