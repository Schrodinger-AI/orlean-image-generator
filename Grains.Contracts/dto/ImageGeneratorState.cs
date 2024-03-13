namespace Grains.Contracts
{

    public class ImageGenerationState
    {
        public string RequestId { get; set; }

        public string ParentRequestId { get; set; }

        public ImageGenerationStatus Status { get; set; } = ImageGenerationStatus.Dormant;

        public string Prompt { get; set; }
        public string? ImageUrl { get; set; }

        public ImageDescription? Image { get; set; } = null;

        public string? Error { get; set; } = null;

    }

    public class ImageDescription
    {
        public string? Image { get; set; } = null;

        public List<Attribute> Attributes { get; set; } = [];

        public string? ExtraData { get; set; } = null;
    }


}