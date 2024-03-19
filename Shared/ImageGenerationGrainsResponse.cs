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

    [GenerateSerializer]
    public class ImageQueryGrainResponse
    {
        [Id(0)]
        public ImageDescription? Image { get; set; }

        [Id(1)]
        public ImageGenerationStatus Status { get; set; }
        [Id(2)]
        public string? Error { get; set; }
    }

    [GenerateSerializer]
    public class MultiImageQueryGrainResponse
    {
        [Id(0)]
        public bool Uninitialized { get; set; }
        [Id(1)]
        public List<ImageDescription>? Images { get; set; }

        [Id(2)]
        public ImageGenerationStatus Status { get; set; } = ImageGenerationStatus.Dormant;

        [Id(3)]
        public List<string>? Errors { get; set; }
    }

    [GenerateSerializer]
    public class ImageGenerationGrainResponse
    {
        [Id(0)]
        public string RequestId { get; set; }

        [Id(1)]
        public long DalleRequestTimestamp { get; set; }

        [Id(2)]
        public bool IsSuccessful { get; set; }
        [Id(3)]
        public string? Error { get; set; }
        [Id(4)]
        public DalleErrorCode? ErrorCode { get; set; }
    }

    [GenerateSerializer]
    public class MultiImageGenerationGrainResponse
    {
        [Id(0)]
        public string RequestId { get; set; }

        [Id(1)]
        public string Prompt { get; set; }

        [Id(2)]
        public List<Attribute> Traits { get; set; }

        [Id(3)]
        public bool IsSuccessful { get; set; } = false;

        [Id(4)]
        public List<string>? Errors { get; set; }
    }
}