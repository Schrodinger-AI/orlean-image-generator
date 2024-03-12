namespace Shared
{
    public enum ImageGenerationStatus
    {
        Dormant,

        InProgress,
        FailedCompletion,

        SuccessfulCompletion,

        ReScheduled
    }

    public class ImageQueryGrainResponse
    {
        public ImageDescription? Image { get; set; }

        public ImageGenerationStatus Status { get; set; }
        public string? Error { get; set; }
    }

    public class MultiImageQueryGrainResponse
    {
        public bool Initialized { get; set; }
        public List<ImageDescription>? Images { get; set; }

        public ImageGenerationStatus Status { get; set; } = ImageGenerationStatus.Dormant;

        public List<string>? Errors { get; set; }
    }


    public class ImageGenerationGrainResponse
    {
        public string RequestId { get; set; }

        public bool IsSuccessful { get; set; }
        public string? Error { get; set; }
    }

    public class MultiImageGenerationGrainResponse
    {
        public string RequestId { get; set; }

        public string Prompt { get; set; }

        public List<Attribute> Traits { get; set; }

        public bool IsSuccessful { get; set; } = false;

        public List<string>? Errors { get; set; }
    }
}