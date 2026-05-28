namespace InnerSky.Wasm.Models;

/// <summary>
/// One of the eight base-emotion rows in the panel. <see cref="Level"/> is always
/// meaningful (defaults to Base); <see cref="Enabled"/> is the only thing that gates
/// interaction with the slider.
/// </summary>
public sealed class EmotionComponent
{
    public required string Id { get; init; }
    public required string Label { get; init; }

    public bool Enabled { get; set; }
    public IntensityLevel Level { get; set; } = IntensityLevel.Base;
}
