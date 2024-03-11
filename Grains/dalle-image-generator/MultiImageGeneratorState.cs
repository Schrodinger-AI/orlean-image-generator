using Shared;

namespace Grains;

public class MultiImageGenerationState
{

    public string RequestId { get; set; }

    public List<Trait> Traits { get; set; }

    public string Prompt { get; set; }

    public bool IsSuccessful { get; set; }


    public List<string>? Errors { get; set; }

   public List<string> ImageGenerationRequestIds = new List<string>();
}
