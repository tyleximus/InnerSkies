using System.Net.Http.Json;
using InnerSky.Wasm.Models;

namespace InnerSky.Wasm.Services;

public sealed class EmotionProfilesApiClient(HttpClient httpClient)
{
    public async Task<EmotionProfileApiResponse?> CreateAsync(EmotionProfile profile, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/emotionprofiles", new EmotionProfileApiRequest(
            profile.Name,
            profile.Components.Select(c => new EmotionProfileApiComponentRequest(c.Emotion, (int)c.Level)).ToList()), cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmotionProfileApiResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<EmotionProfileApiResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await httpClient.GetFromJsonAsync<List<EmotionProfileApiResponse>>("api/emotionprofiles", cancellationToken);
        return profiles ?? [];
    }
}

public sealed record EmotionProfileApiRequest(string Name, IReadOnlyList<EmotionProfileApiComponentRequest> Components);
public sealed record EmotionProfileApiComponentRequest(string Emotion, int Level);
public sealed record EmotionProfileApiResponse(Guid Id, string Name, DateTime CreatedUtc, IReadOnlyList<EmotionProfileApiComponentResponse> Components);
public sealed record EmotionProfileApiComponentResponse(string Emotion, int Level);
