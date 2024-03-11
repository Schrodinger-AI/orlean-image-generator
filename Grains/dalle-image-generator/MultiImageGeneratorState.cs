using Shared;

namespace Grains;

public class MultiImageGenerationState
{
    public string RequestId { get; set; }

   public List<string> ImageGenerationRequestIds = new List<string>();
}
