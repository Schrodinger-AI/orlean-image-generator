
namespace Shared
{

    public class ImageQueryGrainResponse
    {
        public ImageDescription? Image { get; set; }

        public bool IsSuccessful { get; set; }
        public string? Error { get; set; }
    }

    public class MultiImageQueryGrainResponse
    {
        public List<ImageDescription>? Images { get; set; }

        public bool IsSuccessful { get; set; }
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

        public List<Trait> Traits { get; set; }

        public bool IsSuccessful { get; set; } = false;
        public List<string>? Errors { get; set; }
    }

}