using System.Text.Json.Serialization;

namespace Shared
{

    public abstract class AddTraitsAPIResponse { }
    
    public class AddTraitsResponseOk(Dictionary<string, TraitEntry> addedTraits) : AddTraitsAPIResponse
    {
        [JsonPropertyName("totalTraitsCount")]
        public long TotalTraitsCount { get; set; }

        [JsonPropertyName("addedTraits")]
        public Dictionary<string, TraitEntry> AddedTraits { get; set; } = addedTraits;
    }

    public class AddTraitsResponseNotOk(string error) : AddTraitsAPIResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = error;
    }

    public abstract class ClearAllTraitsAPIResponse { }

    public class ClearAllTraitsResponseOk() : ClearAllTraitsAPIResponse
    {
        [JsonPropertyName("clearedTraitsCount")]
        public long ClearedTraitsCount { get; set; }

    }

    public class ClearAllTraitsResponseNotOk(string error) : ClearAllTraitsAPIResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = error;
    }

    public abstract class AddTraitAPIResponse { }

    public class AddTraitResponseOk(TraitEntry addedTrait) : AddTraitAPIResponse
    {
        [JsonPropertyName("totalTraitsCount")]
        public long TotalTraitsCount { get; set; }

        [JsonPropertyName("addedTrait")]
        public TraitEntry AddedTrait { get; set; } = addedTrait;
    }

    public class AddTraitResponseNotOk(string error) : AddTraitAPIResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = error;
    }

    public abstract class DeleteTraitAPIResponse { }

    public class DeleteTraitResponseOk(TraitEntry deletedTrait) : DeleteTraitAPIResponse
    {
        [JsonPropertyName("totalTraitsCount")]
        public long TotalTraitsCount { get; set; }

        [JsonPropertyName("deletedTrait")]
        public TraitEntry DeletedTrait { get; set; } = deletedTrait;
    }

    public class DeleteTraitResponseNotOk(string error) : DeleteTraitAPIResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = error;
    }

    public abstract class UpdateTraitAPIResponse { }

    public class UpdateTraitResponseOk(TraitEntry updatedTrait) : UpdateTraitAPIResponse
    {
        [JsonPropertyName("totalTraitsCount")]
        public long TotalTraitsCount { get; set; }

        [JsonPropertyName("updatedTrait")]
        public TraitEntry UpdatedTrait { get; set; } = updatedTrait;
    }

    public class UpdateTraitResponseNotOk(string error) : UpdateTraitAPIResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = error;
    }

    public abstract class ClearTraitsAPIResponse { }

    public class ClearTraitsResponseOk() : ClearTraitsAPIResponse
    {
        [JsonPropertyName("totalTraitsCount")]
        public long TotalTraitsCount { get; set; }

        [JsonPropertyName("clearedTraitsCount")]
        public long ClearedTraitsCount { get; set; }

    }

    public class ClearTraitsResponseNotOk(string error) : ClearTraitsAPIResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = error;
    }


}