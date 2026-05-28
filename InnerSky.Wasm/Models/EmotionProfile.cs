namespace InnerSky.Wasm.Models;

/// <summary>
/// The shape that will eventually be POSTed to the WebAPI. Per the design decision,
/// nothing from the image itself travels to the API — only the user-facing
/// <see cref="Name"/> label and the set of enabled emotions with their levels.
/// This lives in the WASM client for now; when the API arrives it can graduate into
/// a shared class library referenced by both the client and the API project.
/// </summary>
public sealed record EmotionProfile(string Name, IReadOnlyList<EmotionProfileComponent> Components);

/// <summary>A single enabled emotion and the intensity the user settled on.</summary>
public sealed record EmotionProfileComponent(string Emotion, IntensityLevel Level);
