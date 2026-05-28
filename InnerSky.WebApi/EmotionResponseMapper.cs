namespace InnerSky.WebApi;

internal static class EmotionResponseMapper
{
    public static EmotionProfileResponse ToResponse(this EmotionProfileEntity profile) =>
        new(
            profile.Id,
            profile.Name,
            profile.CreatedUtc,
            profile.MomentId,
            profile.SortOrder,
            profile.Components
                .OrderBy(c => c.Id)
                .Select(c => new EmotionProfileComponentResponse(c.EmotionId, c.IntensityLevel))
                .ToList());

    public static EmotionMomentResponse ToResponse(this EmotionMomentEntity moment) =>
        new(
            moment.Id,
            moment.Title,
            moment.Comment,
            moment.MomentUtc,
            moment.CreatedUtc,
            moment.Profiles
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.CreatedUtc)
                .ThenBy(p => p.Id)
                .Select(p => p.ToResponse())
                .ToList());
}
