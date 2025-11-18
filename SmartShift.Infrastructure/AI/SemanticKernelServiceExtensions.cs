using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace SmartShift.Infrastructure.AI;

public static class SemanticKernelServiceExtensions
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        // קודם ננסה לקרוא מה-IConfiguration כמו שצריך
        var openAiApiKey =
            configuration["SemanticKernel:OpenAI:ApiKey"]
            ?? configuration["OpenAI:ApiKey"];

        var openAiModelId =
            configuration["SemanticKernel:OpenAI:ModelId"]
            ?? configuration["OpenAI:ModelId"];

        var azureEndpoint =
            configuration["SemanticKernel:AzureOpenAI:Endpoint"]
            ?? configuration["AzureOpenAI:Endpoint"];

        var azureDeploymentName =
            configuration["SemanticKernel:AzureOpenAI:DeploymentName"]
            ?? configuration["AzureOpenAI:DeploymentName"];

        var azureApiKey =
            configuration["SemanticKernel:AzureOpenAI:ApiKey"]
            ?? configuration["AzureOpenAI:ApiKey"];

        // אם משום מה הקונפיג לא רואה את זה - נלך ישירות ל־Environment Variables
        openAiApiKey ??= Environment.GetEnvironmentVariable("SemanticKernel__OpenAI__ApiKey")
                        ?? Environment.GetEnvironmentVariable("OpenAI__ApiKey");

        openAiModelId ??= Environment.GetEnvironmentVariable("SemanticKernel__OpenAI__ModelId")
                         ?? Environment.GetEnvironmentVariable("OpenAI__ModelId");

        azureEndpoint ??= Environment.GetEnvironmentVariable("SemanticKernel__AzureOpenAI__Endpoint")
                         ?? Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint");

        azureDeploymentName ??= Environment.GetEnvironmentVariable("SemanticKernel__AzureOpenAI__DeploymentName")
                               ?? Environment.GetEnvironmentVariable("AzureOpenAI__DeploymentName");

        azureApiKey ??= Environment.GetEnvironmentVariable("SemanticKernel__AzureOpenAI__ApiKey")
                       ?? Environment.GetEnvironmentVariable("AzureOpenAI__ApiKey");

        // לוג קצר לעזרה אם עדיין יש בעיות
        Console.WriteLine($"[SK] OpenAI ApiKey length: {openAiApiKey?.Length ?? 0}");
        Console.WriteLine($"[SK] OpenAI ModelId: {openAiModelId ?? "null"}");
        Console.WriteLine($"[SK] Azure Endpoint: {azureEndpoint ?? "null"}");
        Console.WriteLine($"[SK] Azure Deployment: {azureDeploymentName ?? "null"}");

        var kernelBuilder = Kernel.CreateBuilder();

        // קודם ננסה Azure OpenAI אם מוגדר
        if (!string.IsNullOrWhiteSpace(azureEndpoint) &&
            !string.IsNullOrWhiteSpace(azureDeploymentName) &&
            !string.IsNullOrWhiteSpace(azureApiKey))
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: azureDeploymentName,
                endpoint: azureEndpoint,
                apiKey: azureApiKey);
        }
        // אחרת נלך על OpenAI רגיל
        else if (!string.IsNullOrWhiteSpace(openAiApiKey) &&
                 !string.IsNullOrWhiteSpace(openAiModelId))
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: openAiModelId,
                apiKey: openAiApiKey);
        }
        else
        {
            throw new InvalidOperationException("Either OpenAI or AzureOpenAI configuration must be provided");
        }

        var kernel = kernelBuilder.Build();
        services.AddSingleton(kernel);

        return services;
    }
}
