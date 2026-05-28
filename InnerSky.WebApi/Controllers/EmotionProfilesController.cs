using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnerSky.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EmotionProfilesController(InnerSkyDbContext db) : ControllerBase
{
    private static readonly HashSet<string> ValidEmotionIds =
    [
        "joy", "trust", "fear", "surprise", "sadness", "disgust", "anger", "anticipation"
    ];

    [HttpPost]
    public async Task<ActionResult<EmotionProfileResponse>> Create([FromBody] EmotionProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ValidationProblem("Name is required.");

        foreach (var component in request.Components)
        {
            if (!ValidEmotionIds.Contains(component.Emotion))
                return ValidationProblem($"Invalid emotion '{component.Emotion}'.");
            if (component.Level is < 0 or > 2)
                return ValidationProblem($"Invalid level '{component.Level}' for emotion '{component.Emotion}'.");
        }

        var profile = new EmotionProfileEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedUtc = DateTime.UtcNow,
            Components = request.Components
                .Select(c => new EmotionProfileComponentEntity
                {
                    EmotionId = c.Emotion,
                    IntensityLevel = (byte)c.Level
                })
                .ToList()
        };

        db.EmotionProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        var response = new EmotionProfileResponse(
            profile.Id,
            profile.Name,
            profile.CreatedUtc,
            profile.Components.Select(c => new EmotionProfileComponentResponse(c.EmotionId, c.IntensityLevel)).ToList());

        return CreatedAtAction(nameof(GetById), new { id = profile.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmotionProfileResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var profile = await db.EmotionProfiles
            .AsNoTracking()
            .Include(x => x.Components)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (profile is null)
            return NotFound();

        return new EmotionProfileResponse(
            profile.Id,
            profile.Name,
            profile.CreatedUtc,
            profile.Components.Select(c => new EmotionProfileComponentResponse(c.EmotionId, c.IntensityLevel)).ToList());
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmotionProfileResponse>>> List(CancellationToken cancellationToken)
    {
        var profiles = await db.EmotionProfiles
            .AsNoTracking()
            .Include(x => x.Components)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        return profiles
            .Select(profile => new EmotionProfileResponse(
                profile.Id,
                profile.Name,
                profile.CreatedUtc,
                profile.Components.Select(c => new EmotionProfileComponentResponse(c.EmotionId, c.IntensityLevel)).ToList()))
            .ToList();
    }

    private ActionResult ValidationProblem(string detail)
    {
        ModelState.AddModelError("request", detail);
        return base.ValidationProblem(ModelState);
    }
}
