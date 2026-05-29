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

        if (request.MomentId is null && request.NewMoment is null)
            return ValidationProblem("Provide either MomentId or NewMoment.");
        if (request.MomentId is not null && request.NewMoment is not null)
            return ValidationProblem("Provide only one of MomentId or NewMoment.");

        EmotionMomentEntity moment;
        int sortOrder;
        if (request.MomentId is int momentId)
        {
            var existingMoment = await db.EmotionMoments.FirstOrDefaultAsync(x => x.Id == momentId, cancellationToken);
            if (existingMoment is null)
                return ValidationProblem($"Moment '{momentId}' was not found.");

            moment = existingMoment;
            var maxOrder = await db.EmotionProfiles
                .Where(p => p.MomentId == momentId)
                .Select(p => (int?)p.SortOrder)
                .MaxAsync(cancellationToken);
            sortOrder = (maxOrder ?? -1) + 1;
        }
        else
        {
            sortOrder = 0;
            var title = string.IsNullOrWhiteSpace(request.NewMoment!.Title) ? request.Name.Trim() : request.NewMoment.Title.Trim();
            moment = new EmotionMomentEntity
            {
                
                Title = title,
                Comment = string.IsNullOrWhiteSpace(request.NewMoment.Comment) ? null : request.NewMoment.Comment.Trim(),
                MomentUtc = DateTime.SpecifyKind(request.NewMoment.MomentUtc, DateTimeKind.Utc),
                CreatedUtc = DateTime.UtcNow
            };
            db.EmotionMoments.Add(moment);
        }

        var profile = new EmotionProfileEntity
        {
            
            Name = request.Name.Trim(),
            CreatedUtc = DateTime.UtcNow,
            SortOrder = sortOrder,
            Moment = moment,
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
            profile.MomentId,
            profile.Components.Select(c => new EmotionProfileComponentResponse(c.EmotionId, c.IntensityLevel)).ToList());

        return CreatedAtAction(nameof(GetById), new { id = profile.Id }, response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmotionProfileResponse>> GetById(int id, CancellationToken cancellationToken)
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
            profile.MomentId,
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
                profile.MomentId,
                profile.Components.Select(c => new EmotionProfileComponentResponse(c.EmotionId, c.IntensityLevel)).ToList()))
            .ToList();
    }

    [HttpGet("moments")]
    public async Task<ActionResult<IReadOnlyList<EmotionMomentResponse>>> ListMoments(CancellationToken cancellationToken)
    {
        var moments = await db.EmotionMoments
            .AsNoTracking()
            .Include(x => x.Profiles)
                .ThenInclude(x => x.Components)
            .OrderByDescending(x => x.MomentUtc)
            .ToListAsync(cancellationToken);

        return moments.Select(m => new EmotionMomentResponse(
            m.Id,
            m.Title,
            m.Comment,
            m.MomentUtc,
            m.CreatedUtc,
            m.Profiles
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.CreatedUtc)
                .Select(p => new EmotionProfileResponse(
                    p.Id,
                    p.Name,
                    p.CreatedUtc,
                    p.MomentId,
                    p.Components.Select(c => new EmotionProfileComponentResponse(c.EmotionId, c.IntensityLevel)).ToList()))
                .ToList()
        )).ToList();
    }

    [HttpPut("moments/{id:int}")]
    public async Task<ActionResult> UpdateMoment(int id, [FromBody] EmotionMomentUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return ValidationProblem("Title is required.");
        if (request.Title.Length > 200)
            return ValidationProblem("Title must be 200 characters or fewer.");
        if (request.Comment is { Length: > 2000 })
            return ValidationProblem("Comment must be 2000 characters or fewer.");

        var moment = await db.EmotionMoments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (moment is null)
            return NotFound();

        moment.Title = request.Title.Trim();
        moment.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        moment.MomentUtc = DateTime.SpecifyKind(request.MomentUtc, DateTimeKind.Utc);

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteBlend(int id, CancellationToken cancellationToken)
    {
        var profile = await db.EmotionProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null)
            return NotFound();

        db.EmotionProfiles.Remove(profile);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("moments/{id:int}/blend-order")]
    public async Task<ActionResult> ReorderBlends(int id, [FromBody] BlendOrderRequest request, CancellationToken cancellationToken)
    {
        var moment = await db.EmotionMoments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (moment is null)
            return NotFound();

        var profiles = await db.EmotionProfiles
            .Where(p => p.MomentId == id)
            .ToListAsync(cancellationToken);

        var requested = request.BlendIds ?? [];
        if (requested.Count != profiles.Count || requested.Distinct().Count() != requested.Count
            || !requested.ToHashSet().SetEquals(profiles.Select(p => p.Id)))
        {
            return ValidationProblem("Blend order must list each of the moment's blends exactly once.");
        }

        var orderById = requested.Select((blendId, index) => (blendId, index))
            .ToDictionary(x => x.blendId, x => x.index);
        foreach (var profile in profiles)
            profile.SortOrder = orderById[profile.Id];

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private ActionResult ValidationProblem(string detail)
    {
        ModelState.AddModelError("request", detail);
        return base.ValidationProblem(ModelState);
    }
}
