# InnerSkies.Wasm — Plutchik Emotion panel (Blazor WASM, Approach B)

Drop-in components that add the **Emotion** panel beneath your existing calibrated
Plutchik image. The image overlay is rendered by a JS-interop module that is a
near-verbatim port of your `preview.html` — coordinates, sizes, rotations, the
expand-on-base-tap behavior, and the *show tap regions* debug mode are all preserved.
The only behavioral change is that taps now notify a C# state service instead of
calling `alert()`.

## Architecture

```
PlutchikOverlay.razor  ──(JS interop)──► wwwroot/js/plutchikOverlay.js   (calibration lives here)
        │  [JSInvokable] OnDyadSelected / OnIntensitySelected
        ▼
EmotionStateService    ◄── canonical state + ALL selection rules (testable, no UI)
        │  OnChange event
        ▼
EmotionPanel.razor  ──► EmotionSlider.razor × 8   (presentational; params in, callbacks out)
```

- **`Data/PlutchikData.cs`** — single source of truth for emotion↔dyad mapping and
  intensity naming (e.g. `joy + Intense → "Ecstasy"`). The pixel coordinates are *not*
  here; they stay in the JS module so your calibration is never disturbed by C# edits.
- **`Services/EmotionStateService.cs`** — owns `Name` + the 8 components and every
  rule:
  - Image = seed only. Choosing an ellipse **resets all toggles** and **overwrites the
    name**.
  - Dyad → both emotions enabled at **Base**; name = dyad label.
  - Intensity circle → that one emotion enabled at the chosen level; name = intensity
    label.
  - A bare base-circle tap commits nothing (it only expands the three options).
  - Manual toggle **ON → Base**; manual toggle **OFF → reset that row to Base**; manual
    toggling does not reset others and does not rename.
  - `ToProfile()` produces the `EmotionProfile` DTO (name + enabled emotions + levels)
    — the only thing destined for the future WebAPI.

## Wiring it up

1. **Namespace** — everything uses the root namespace `InnerSkies.Wasm`. If your
   project differs, do a find/replace on `InnerSkies.Wasm` (in the `.cs` files and the
   `@using` lines of the `.razor` files), or move the files under a matching folder.

2. **Register the service** (scoped) in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<InnerSkies.Wasm.Services.EmotionStateService>();
   ```
   Scoped (not singleton) so each user/session starts with a clean panel; in WASM
   scoped behaves like singleton-per-app, which is what you want here.

3. **Add Bootstrap 5.3 + the global stylesheets.** In `wwwroot/index.html`, inside
   `<head>`, in this order (Bootstrap first, then the brand theme, then the overlay):
   ```html
   <!-- Bootstrap 5.3 (grab the exact <link>/<script> tags incl. integrity hashes
        from https://getbootstrap.com/docs/5.3/getting-started/introduction/ ) -->
   <link rel="stylesheet"
         href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />

   <!-- InnerSkies brand layer — MUST load after Bootstrap so its overrides win -->
   <link rel="stylesheet" href="css/innerskies-theme.css" />

   <!-- Plutchik wheel overlay styles -->
   <link rel="stylesheet" href="css/plutchikOverlay.css" />
   ```
   The default Blazor WASM template ships an older Bootstrap under
   `wwwroot/lib/bootstrap/` and links it in `index.html`; replace that `<link>` with
   the 5.3 one above (or update the local copy) so the new components render correctly.

   Bootstrap's JS bundle is **not required** for this panel — switches, `form-range`,
   and badges are CSS-only. Add `bootstrap.bundle.min.js` before `</body>` only if you
   later use JS-driven components (tooltips, dropdowns, modals).

   `innerskies-theme.css` and `plutchikOverlay.css` are intentionally **not** Blazor
   scoped CSS: the theme overrides Bootstrap globally, and the overlay's `.tap-zone` /
   `.intensity-*` elements are injected into the SVG at runtime and wouldn't carry a
   scope attribute. (`EmotionPanel` and `EmotionSlider` still use normal scoped
   `.razor.css` for their brand tweaks.)

4. **Assets** — `wwwroot/img/plutchik-wheel-of-emotion-dyads-web.webp` and
   `wwwroot/js/plutchikOverlay.js` are included. The overlay component references the
   image at `img/plutchik-wheel-of-emotion-dyads-web.webp`.

5. **(Optional) `_Imports.razor`** — to drop the per-file `@using` lines you can add:
   ```razor
   @using InnerSkies.Wasm.Components
   @using InnerSkies.Wasm.Models
   @using InnerSkies.Wasm.Services
   @using InnerSkies.Wasm.Data
   ```

6. Browse to **`/emotion-builder`** (route defined in `Pages/EmotionBuilder.razor`).

## Files

```
Models/IntensityLevel.cs          enum Mild=0 / Base=1 / Intense=2 (maps to a 0–2 slider)
Models/EmotionComponent.cs        one of the 8 rows (Id, Label, Enabled, Level)
Models/EmotionProfile.cs          DTO for the future WebAPI POST
Data/PlutchikData.cs              emotion/dyad mapping + intensity names + colors
Services/EmotionStateService.cs   canonical state + all rules + ToProfile()
Components/PlutchikOverlay.razor  JS-interop host for the calibrated image overlay
Components/EmotionPanel.razor     "Emotion" panel: name textbox + 8 sliders
Components/EmotionSlider.razor    reusable per-emotion slider row
Pages/EmotionBuilder.razor        composes overlay + panel at /emotion-builder
wwwroot/js/plutchikOverlay.js     ported overlay logic (calibration preserved)
wwwroot/css/plutchikOverlay.css   ported overlay styles (global, non-scoped)
wwwroot/css/innerskies-theme.css  Bootstrap brand override (global; load after Bootstrap)
wwwroot/img/...webp               the wheel image
```

## When the WebAPI arrives

Add a `SubmitAsync` to the service (or a thin client) that calls
`ToProfile()` and POSTs it. Because every rule already lives in the service and the DTO
is defined, no component changes are needed — and the `EmotionProfile` records can move
into a shared class library referenced by both this client and the API project.
