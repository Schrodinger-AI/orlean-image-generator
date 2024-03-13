using Grains.Contracts;

namespace Grains;

public class MultiImageGenerationState
{

    public string RequestId { get; set; }

    public List<Contracts.Attribute> Traits { get; set; }

    public string Prompt { get; set; }

    public bool IsSuccessful { get; set; }


    public List<string>? Errors { get; set; }

   public List<string> ImageGenerationRequestIds = [];

   public Dictionary<string, ImageGenerationTracker> imageGenerationTrackers = [];
}

public class ImageGenerationTracker
{
    public string RequestId { get; set; }

    public ImageGenerationStatus Status { get; set; }

    public string? Error { get; set; }
}
