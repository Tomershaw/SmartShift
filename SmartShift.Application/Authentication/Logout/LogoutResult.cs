namespace SmartShift.Application.Authentication.Logout;

public class LogoutResult
{
    public bool Success { get; set; }                     // האם הפעולה הצליחה
    public string Message { get; set; } = string.Empty;   // הודעה כללית (למשל: "3 tokens revoked")
    public int RevokedCount { get; set; }                 // כמה טוקנים בוטלו בפועל
}
