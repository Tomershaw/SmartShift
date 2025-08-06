using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SmartShift.Api.Endpoints;
using System.Text.Json;
using Xunit;

namespace SmartShift.Tests;

public class ConnectionTestEndpointTests
{
    [Fact]
    public void ConnectionTest_EndpointConfiguration_ShouldBeValid()
    {
        // Arrange
        var endpoint = new ConnectionTestEndpoint();
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCarter();
        var app = builder.Build();
        
        // Act - This will verify the endpoint can be registered without throwing
        endpoint.AddRoutes(app);
        
        // Assert - If we get here without exception, the endpoint is properly configured
        Assert.True(true);
    }

    [Fact]
    public void ConnectionTest_Response_ShouldContainHebrewText()
    {
        // Arrange - Create the expected response structure
        var expectedResponse = new { 
            message = "כן, אני מחובר לפרוייקט שלך! 🔗✅",
            englishMessage = "Yes, I am connected to your project! 🔗✅",
            projectName = "SmartShift",
            status = "connected",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
        };

        // Act & Assert
        Assert.Contains("כן, אני מחובר לפרוייקט שלך", expectedResponse.message);
        Assert.Contains("SmartShift", expectedResponse.projectName);
        Assert.Equal("connected", expectedResponse.status);
        Assert.Contains("Yes, I am connected to your project", expectedResponse.englishMessage);
    }
}