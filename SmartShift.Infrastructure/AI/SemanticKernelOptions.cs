namespace SmartShift.Infrastructure.AI;

public class SemanticKernelOptions
{
    public const string SectionName = "SemanticKernel";
    
    public OpenAIOptions? OpenAI { get; set; }
    public AzureOpenAIOptions? AzureOpenAI { get; set; }
}

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gpt-4";
    public string EmbeddingModelId { get; set; } = "text-embedding-ada-002";
}

public class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";
}