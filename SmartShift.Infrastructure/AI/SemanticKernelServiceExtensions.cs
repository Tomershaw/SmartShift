using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace SmartShift.Infrastructure.AI;

public static class SemanticKernelServiceExtensions
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        // שלב 1: קריאת ההגדרות מה-appsettings.json
        var semanticKernelOptions = new SemanticKernelOptions();
        configuration.GetSection(SemanticKernelOptions.SectionName).Bind(semanticKernelOptions);

        // שלב 2: שמירת ההגדרות במערכת ה-DI
        services.Configure<SemanticKernelOptions>(configuration.GetSection(SemanticKernelOptions.SectionName));

        // שלב 3: יצירת ה-Kernel (המוח של Semantic Kernel)
        var kernelBuilder = Kernel.CreateBuilder();

        // שלב 4: הגדרת חיבור ל-OpenAI או Azure OpenAI
        if (!string.IsNullOrEmpty(semanticKernelOptions.AzureOpenAI?.Endpoint))
        {
            // אם יש הגדרות Azure OpenAI - השתמש בהן
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: semanticKernelOptions.AzureOpenAI.DeploymentName,
                endpoint: semanticKernelOptions.AzureOpenAI.Endpoint,
                apiKey: semanticKernelOptions.AzureOpenAI.ApiKey);
        }
        else if (!string.IsNullOrEmpty(semanticKernelOptions.OpenAI?.ApiKey))
        {
            // אם יש הגדרות OpenAI רגיל - השתמש בהן
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: semanticKernelOptions.OpenAI.ModelId,
                apiKey: semanticKernelOptions.OpenAI.ApiKey);
        }
        else
        {
            // אם אין הגדרות בכלל - זרוק שגיאה
            throw new InvalidOperationException("Either OpenAI or AzureOpenAI configuration must be provided");
        }

        // שלב 5: יצירת ה-Kernel הסופי ורישומו במערכת ה-DI
        var kernel = kernelBuilder.Build();
        services.AddSingleton(kernel);

        return services;
    }
}