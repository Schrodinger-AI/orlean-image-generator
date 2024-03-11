using Shared;

namespace Grains;

public class MultiImageGenerationState
{
    public bool IsSuccessful { get; set; }

    public string RequestId { get; set; }

    public List<string>? Errors { get; set; }

   public List<string> ImageGenerationRequestIds = new List<string>();
}
