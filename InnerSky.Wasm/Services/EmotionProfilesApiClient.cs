using System.Net.Http.Json;
using InnerSky.Wasm.Models;

namespace InnerSky.Wasm.Services;

public sealed class EmotionProfilesApiClient(HttpClient httpClient)
{
    public async Task<EmotionProfileApiResponse?> CreateAsync(EmotionProfile profile, int? momentId, EmotionMomentApiRequest? newMoment, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/emotionprofiles", new EmotionProfileApiRequest(
            profile.Name,
            profile.Components.Select(c => new EmotionProfileApiComponentRequest(c.Emotion, (int)c.Level)).ToList(),
            momentId,
            newMoment), cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmotionProfileApiResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<EmotionProfileApiResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await httpClient.GetFromJsonAsync<List<EmotionProfileApiResponse>>("api/emotionprofiles", cancellationToken);
        return profiles ?? [];
    }

    public async Task<IReadOnlyList<EmotionMomentApiResponse>> ListMomentsAsync(CancellationToken cancellationToken = default)
    {
        var moments = await httpClient.GetFromJsonAsync<List<EmotionMomentApiResponse>>("api/emotionprofiles/moments", cancellationToken);
        return moments ?? [];
    }

    public async Task UpdateMomentAsync(int momentId, EmotionMomentUpdateApiRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/emotionprofiles/moments/{momentId}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteBlendAsync(int blendId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/emotionprofiles/{blendId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReorderBlendsAsync(int momentId, IReadOnlyList<int> blendIds, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/emotionprofiles/moments/{momentId}/blend-order",
            new BlendOrderApiRequest(blendIds),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public sealed record EmotionProfileApiRequest(string Name, IReadOnlyList<EmotionProfileApiComponentRequest> Components, int? MomentId, EmotionMomentApiRequest? NewMoment);
public sealed record EmotionMomentApiRequest(string? Title, string? Comment, DateTime MomentUtc);
public sealed record EmotionProfileApiComponentRequest(string Emotion, int Level);
public sealed record EmotionProfileApiResponse(int Id, string Name, DateTime CreatedUtc, int MomentId, IReadOnlyList<EmotionProfileApiComponentResponse> Components);
public sealed record EmotionProfileApiComponentResponse(string Emotion, int Level);
public sealed record EmotionMomentApiResponse(int Id, string Title, string? Comment, DateTime MomentUtc, DateTime CreatedUtc, IReadOnlyList<EmotionProfileApiResponse> Blends);
public sealed record EmotionMomentUpdateApiRequest(string Title, string? Comment, DateTime MomentUtc);
public sealed record BlendOrderApiRequest(IReadOnlyList<int> BlendIds);
