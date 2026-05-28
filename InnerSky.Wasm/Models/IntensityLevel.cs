namespace InnerSky.Wasm.Models;

/// <summary>
/// The three intensity stops a base emotion can take. Backed by 0/1/2 so it maps
/// directly onto a 3-stop range slider (min 0, max 2, step 1).
/// </summary>
public enum IntensityLevel
{
    Mild = 0,
    Base = 1,
    Intense = 2
}
