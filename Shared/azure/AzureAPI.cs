namespace Shared;

public class AzureImageData
{
    public string Url { get; set; }
    public string RevisedPrompt { get; set; }
}

public class AzureImageGenerationResponse
{
    public string Created { get; set; }
    public List<AzureImageData> Data { get; set; }
}