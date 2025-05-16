using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

public class AuthenticationTests
{
    private readonly HttpClient _client = new HttpClient
    {
        BaseAddress = new Uri("https://localhost:7002/") // שנה את ה-port בהתאם להרצה שלך
    };

   private async Task<string> GetJwtTokenAsync()
{
    var requestBody = new
    {
        Email = "testuser@example.com", // ודא שהמשתמש הזה קיים במערכת
        Password = "Test@123"            // ודא שהסיסמה נכונה
    };

    var content = new StringContent(
        JsonSerializer.Serialize(requestBody),
        Encoding.UTF8,
        "application/json");

    var response = await _client.PostAsync("api/account/login", content);
    var responseContent = await response.Content.ReadAsStringAsync();

    // הדפסת התגובה המלאה במידה ויש שגיאה, כדי להבין מה מוחזר
    if (!response.IsSuccessStatusCode)
    {
        throw new Exception($"Login failed with status code: {response.StatusCode}\nResponse Body:\n{responseContent}");
    }

    try
    {
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var token = jsonDoc.RootElement.GetProperty("token").GetString();
        return token ?? throw new Exception("Token property found but value is null.");
    }
    catch (KeyNotFoundException)
    {
        throw new Exception($"The 'token' key was not found in the response JSON. Response Body:\n{responseContent}");
    }
    catch (Exception ex)
    {
        throw new Exception($"Error parsing JWT token from response. Response Body:\n{responseContent}\nError: {ex.Message}");
    }
}

}
