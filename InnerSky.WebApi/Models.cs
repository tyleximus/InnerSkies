namespace InnerSky.WebApi;

public sealed class EmotionProfileEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public int MomentId { get; set; }
    public int SortOrder { get; set; }
    public EmotionMomentEntity? Moment { get; set; }
    public List<EmotionProfileComponentEntity> Components { get; set; } = [];
}

public sealed class EmotionMomentEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime MomentUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public List<EmotionProfileEntity> Profiles { get; set; } = [];
}

public sealed class EmotionProfileComponentEntity
{
    public long Id { get; set; }
    public int ProfileId { get; set; }
    public string EmotionId { get; set; } = string.Empty;
    public byte IntensityLevel { get; set; }

    public EmotionProfileEntity? Profile { get; set; }
}

public sealed class EmotionMomentQuery
{
    public string? Search { get; set; }
    public string? Emotion { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public sealed record EmotionProfileRequest(string Name, IReadOnlyList<EmotionProfileComponentRequest> Components, int? MomentId, EmotionMomentRequest? NewMoment);
public sealed record EmotionMomentRequest(string? Title, string? Comment, DateTime MomentUtc);
public sealed record EmotionProfileComponentRequest(string Emotion, int Level);
public sealed record EmotionMomentUpdateRequest(string Title, string? Comment, DateTime MomentUtc);
public sealed record EmotionMomentBlendOrderRequest(IReadOnlyList<int> BlendIds);
public sealed record EmotionMomentListResponse(IReadOnlyList<EmotionMomentResponse> Items, int TotalCount, int Page, int PageSize, int TotalPages);
public sealed record EmotionProfileResponse(int Id, string Name, DateTime CreatedUtc, int MomentId, int SortOrder, IReadOnlyList<EmotionProfileComponentResponse> Components);
public sealed record EmotionProfileComponentResponse(string Emotion, int Level);
public sealed record EmotionMomentResponse(int Id, string Title, string? Comment, DateTime MomentUtc, DateTime CreatedUtc, IReadOnlyList<EmotionProfileResponse> Blends);
