using InnerSkies.Wasm.Data;
using InnerSkies.Wasm.Models;

namespace InnerSkies.Wasm.Services;

/// <summary>
/// The single owner of the panel's state. All the selection rules live here (not in
/// the UI), so they can be unit-tested without rendering anything, and so the future
/// WebAPI call has one obvious place to serialize from (<see cref="ToProfile"/>).
///
/// Rules implemented:
///  - The image is only a *seed*. Choosing an ellipse resets all toggles, then sets a
///    fresh selection, and always overwrites the Name.
///  - Dyad chosen      -> both constituent emotions enabled at Base; Name = dyad label.
///  - Intensity chosen -> that one emotion enabled at the chosen level; Name = intensity label.
///  - Tapping a bare base circle in the image commits nothing (it only expands the
///    three intensity options); the commit happens when an intensity circle is tapped.
///  - Manual toggle ON  -> enable at Base. Manual toggle OFF -> reset that row to Base.
///    Manual toggling does NOT reset other rows and does NOT rename.
/// </summary>
public sealed class EmotionStateService
{
    private readonly Dictionary<string, EmotionComponent> _components;

    public EmotionStateService()
    {
        _components = PlutchikData.BaseEmotions.ToDictionary(
            e => e.Id,
            e => new EmotionComponent { Id = e.Id, Label = e.Label });
    }

    /// <summary>The current emotion name (defaults to the dyad/intensity that seeded it; user-editable).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The eight components in canonical wheel order.</summary>
    public IReadOnlyList<EmotionComponent> Components =>
        PlutchikData.BaseEmotions.Select(e => _components[e.Id]).ToList();

    /// <summary>Raised after any state change so subscribed components can re-render.</summary>
    public event Action? OnChange;

    // --- Image-driven selections (always reset + rename) -------------------------------

    /// <summary>A dyad ellipse was chosen: enable both emotions at Base, Name = dyad label.</summary>
    public void SelectDyad(string dyadId)
    {
        if (!PlutchikData.Dyads.TryGetValue(dyadId, out var dyad))
            return;

        ResetAll();
        Enable(dyad.EmotionA, IntensityLevel.Base);
        Enable(dyad.EmotionB, IntensityLevel.Base);
        Name = dyad.Label;
        Notify();
    }

    /// <summary>An intensity circle was chosen: enable that one emotion at the chosen level.</summary>
    public void SelectIntensity(string emotionId, IntensityLevel level)
    {
        if (!_components.ContainsKey(emotionId))
            return;

        ResetAll();
        Enable(emotionId, level);
        Name = PlutchikData.LevelName(emotionId, level);
        Notify();
    }

    // --- Manual panel edits (no reset, no rename) --------------------------------------

    /// <summary>User flipped a row's toggle. ON lands on Base; OFF resets that row to Base.</summary>
    public void ToggleComponent(string emotionId, bool enabled)
    {
        if (!_components.TryGetValue(emotionId, out var c))
            return;

        c.Enabled = enabled;
        c.Level = IntensityLevel.Base; // ON -> Base; OFF -> reset to Base.
        Notify();
    }

    /// <summary>User moved an (enabled) row's slider.</summary>
    public void SetLevel(string emotionId, IntensityLevel level)
    {
        if (!_components.TryGetValue(emotionId, out var c) || !c.Enabled)
            return;

        c.Level = level;
        Notify();
    }

    /// <summary>User edited the Name textbox.</summary>
    public void Rename(string name)
    {
        Name = name ?? string.Empty;
        Notify();
    }

    // --- Serialization for the (future) WebAPI -----------------------------------------

    public EmotionProfile ToProfile()
    {
        var enabled = Components
            .Where(c => c.Enabled)
            .Select(c => new EmotionProfileComponent(c.Id, c.Level))
            .ToList();

        return new EmotionProfile(Name, enabled);
    }

    // --- Internals ---------------------------------------------------------------------

    private void ResetAll()
    {
        foreach (var c in _components.Values)
        {
            c.Enabled = false;
            c.Level = IntensityLevel.Base;
        }
    }

    private void Enable(string emotionId, IntensityLevel level)
    {
        if (_components.TryGetValue(emotionId, out var c))
        {
            c.Enabled = true;
            c.Level = level;
        }
    }

    private void Notify() => OnChange?.Invoke();
}
