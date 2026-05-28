namespace InnerSky.WebApi;

public sealed class EmotionProfileEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public List<EmotionProfileComponentEntity> Components { get; set; } = [];
}

public sealed class EmotionProfileComponentEntity
{
    public long Id { get; set; }
    public Guid ProfileId { get; set; }
    public string EmotionId { get; set; } = string.Empty;
    public byte IntensityLevel { get; set; }

    public EmotionProfileEntity? Profile { get; set; }
}

public sealed record EmotionProfileRequest(string Name, IReadOnlyList<EmotionProfileComponentRequest> Components);
public sealed record EmotionProfileComponentRequest(string Emotion, int Level);
public sealed record EmotionProfileResponse(Guid Id, string Name, DateTime CreatedUtc, IReadOnlyList<EmotionProfileComponentResponse> Components);
public sealed record EmotionProfileComponentResponse(string Emotion, int Level);
