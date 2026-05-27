using InnerSkies.Wasm.Models;

namespace InnerSkies.Wasm.Data;

/// <summary>
/// Canonical, non-visual data for the model layer: the eight base emotions (in wheel
/// order), the 24 dyads and which two emotions each combines, and the per-emotion
/// intensity names. This is the single source of truth for *naming* and *emotion
/// mapping*. The pixel coordinates that drive the image overlay are deliberately NOT
/// here — they live in wwwroot/js/plutchikOverlay.js so the existing calibration is
/// preserved and never disturbed by C# changes.
/// </summary>
public static class PlutchikData
{
    /// <summary>Base emotions in canonical wheel order (id, label).</summary>
    public static readonly IReadOnlyList<(string Id, string Label)> BaseEmotions = new[]
    {
        ("joy",          "Joy"),
        ("trust",        "Trust"),
        ("fear",         "Fear"),
        ("surprise",     "Surprise"),
        ("sadness",      "Sadness"),
        ("disgust",      "Disgust"),
        ("anger",        "Anger"),
        ("anticipation", "Anticipation"),
    };

    /// <summary>Per-emotion intensity labels: (Mild, Base, Intense).</summary>
    private static readonly IReadOnlyDictionary<string, (string Mild, string Base, string Intense)> Intensities =
        new Dictionary<string, (string, string, string)>
        {
            ["joy"]          = ("Serenity",     "Joy",          "Ecstasy"),
            ["trust"]        = ("Acceptance",   "Trust",        "Admiration"),
            ["fear"]         = ("Apprehension", "Fear",         "Terror"),
            ["surprise"]     = ("Distraction",  "Surprise",     "Amazement"),
            ["sadness"]      = ("Pensiveness",  "Sadness",      "Grief"),
            ["disgust"]      = ("Boredom",      "Disgust",      "Loathing"),
            ["anger"]        = ("Annoyance",    "Anger",        "Rage"),
            ["anticipation"] = ("Interest",     "Anticipation", "Vigilance"),
        };

    /// <summary>Dyad id -> (display label, the two base emotions it combines).</summary>
    public static readonly IReadOnlyDictionary<string, (string Label, string EmotionA, string EmotionB)> Dyads =
        new Dictionary<string, (string, string, string)>
        {
            // Primary
            ["optimism"]       = ("Optimism",       "anticipation", "joy"),
            ["love"]           = ("Love",           "joy",          "trust"),
            ["submission"]     = ("Submission",     "trust",        "fear"),
            ["awe"]            = ("Awe",            "fear",         "surprise"),
            ["disapproval"]    = ("Disapproval",    "surprise",     "sadness"),
            ["remorse"]        = ("Remorse",        "sadness",      "disgust"),
            ["contempt"]       = ("Contempt",       "disgust",      "anger"),
            ["aggressiveness"] = ("Aggressiveness", "anger",        "anticipation"),
            // Secondary
            ["hope"]           = ("Hope",           "anticipation", "trust"),
            ["guilt"]          = ("Guilt",          "joy",          "fear"),
            ["curiosity"]      = ("Curiosity",      "trust",        "surprise"),
            ["despair"]        = ("Despair",        "fear",         "sadness"),
            ["unbelief"]       = ("Unbelief",       "surprise",     "disgust"),
            ["envy"]           = ("Envy",           "sadness",      "anger"),
            ["cynicism"]       = ("Cynicism",       "disgust",      "anticipation"),
            ["pride"]          = ("Pride",          "anger",        "joy"),
            // Tertiary
            ["dominance"]      = ("Dominance",      "anger",        "trust"),
            ["anxiety"]        = ("Anxiety",        "anticipation", "fear"),
            ["delight"]        = ("Delight",        "joy",          "surprise"),
            ["sentimentality"] = ("Sentimentality", "trust",        "sadness"),
            ["shame"]          = ("Shame",          "fear",         "disgust"),
            ["outrage"]        = ("Outrage",        "surprise",     "anger"),
            ["pessimism"]      = ("Pessimism",      "sadness",      "anticipation"),
            ["morbidness"]     = ("Morbidness",     "disgust",      "joy"),
        };

    /// <summary>
    /// Per-emotion intensity colors from Plutchik's wheel: (Mild/low, Base, Intense/high).
    /// Used as solid fills (slider thumb, switch track) — never as label text, since the
    /// lightest shades are unreadable on white.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (string Mild, string Base, string Intense)> Colors =
        new Dictionary<string, (string, string, string)>
        {
            ["joy"]          = ("#FFDD9F", "#FFD27F", "#FFC75F"),
            ["trust"]        = ("#66E9B4", "#33E19B", "#00DA82"),
            ["fear"]         = ("#66BA92", "#33A36E", "#008F7A"),
            ["surprise"]     = ("#66B3E2", "#339AD9", "#0081CF"),
            ["sadness"]      = ("#B59EDA", "#9D7ECE", "#845EC2"),
            ["disgust"]      = ("#E69ED0", "#DE7DC1", "#D65DB1"),
            ["anger"]        = ("#FFA9BD", "#FF8CA7", "#FF6F91"),
            ["anticipation"] = ("#FFC0AA", "#FFAB8D", "#FF9671"),
        };

    /// <summary>The fill color for a given emotion at a given level.</summary>
    public static string LevelColor(string emotionId, IntensityLevel level)
    {
        if (!Colors.TryGetValue(emotionId, out var c))
            return "#2563eb";

        return level switch
        {
            IntensityLevel.Mild => c.Mild,
            IntensityLevel.Intense => c.Intense,
            _ => c.Base
        };
    }

    /// <summary>All three level colors for an emotion, indexed 0=Mild, 1=Base, 2=Intense.</summary>
    public static string[] LevelColors(string emotionId) =>
        Colors.TryGetValue(emotionId, out var c)
            ? new[] { c.Mild, c.Base, c.Intense }
            : new[] { "#2563eb", "#2563eb", "#2563eb" };

    /// <summary>The display name for a given emotion at a given level (e.g. joy + Intense -> "Ecstasy").</summary>
    public static string LevelName(string emotionId, IntensityLevel level)
    {
        if (!Intensities.TryGetValue(emotionId, out var names))
            return emotionId;

        return level switch
        {
            IntensityLevel.Mild => names.Mild,
            IntensityLevel.Intense => names.Intense,
            _ => names.Base
        };
    }

    /// <summary>All three level names for an emotion, indexed 0=Mild, 1=Base, 2=Intense (handy for sliders).</summary>
    public static string[] LevelNames(string emotionId) =>
        Intensities.TryGetValue(emotionId, out var n)
            ? new[] { n.Mild, n.Base, n.Intense }
            : new[] { emotionId, emotionId, emotionId };
}
