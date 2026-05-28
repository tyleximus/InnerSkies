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

        if (request.Components.Count == 0)
            return ValidationProblem("At least one emotion component is required.");

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
        var sortOrder = 0;
        if (request.MomentId is int momentId)
        {
            var existingMoment = await db.EmotionMoments
                .Include(x => x.Profiles)
                .FirstOrDefaultAsync(x => x.Id == momentId, cancellationToken);
            if (existingMoment is null)
                return ValidationProblem($"Moment '{momentId}' was not found.");

            moment = existingMoment;
            sortOrder = moment.Profiles.Count == 0 ? 0 : moment.Profiles.Max(x => x.SortOrder) + 1;
        }
        else
        {
            var title = string.IsNullOrWhiteSpace(request.NewMoment!.Title) ? request.Name.Trim() : request.NewMoment.Title.Trim();
            moment = new EmotionMomentEntity
            {
                Title = title,
                Comment = string.IsNullOrWhiteSpace(request.NewMoment.Comment) ? null : request.NewMoment.Comment.Trim(),
                MomentUtc = ToUtcMinute(request.NewMoment.MomentUtc),
                CreatedUtc = DateTime.UtcNow
            };
            db.EmotionMoments.Add(moment);
        }

        var profile = new EmotionProfileEntity
        {
            Name = request.Name.Trim(),
            CreatedUtc = DateTime.UtcNow,
            Moment = moment,
            SortOrder = sortOrder,
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

        return CreatedAtAction(nameof(GetById), new { id = profile.Id }, profile.ToResponse());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmotionProfileResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var profile = await db.EmotionProfiles
            .AsNoTracking()
            .Include(x => x.Components)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return profile is null ? NotFound() : profile.ToResponse();
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

        return profiles.Select(p => p.ToResponse()).ToList();
    }

    [HttpGet("moments")]
    public async Task<ActionResult<EmotionMomentListResponse>> ListMoments([FromQuery] EmotionMomentQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 48);

        var momentsQuery = db.EmotionMoments
            .AsNoTracking()
            .Include(x => x.Profiles)
                .ThenInclude(x => x.Components)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            momentsQuery = momentsQuery.Where(m =>
                EF.Functions.Like(m.Title, $"%{search}%") ||
                (m.Comment != null && EF.Functions.Like(m.Comment, $"%{search}%")) ||
                m.Profiles.Any(p => EF.Functions.Like(p.Name, $"%{search}%") ||
                    p.Components.Any(c => EF.Functions.Like(c.EmotionId, $"%{search}%"))));
        }

        if (!string.IsNullOrWhiteSpace(query.Emotion))
        {
            var emotion = query.Emotion.Trim();
            if (!ValidEmotionIds.Contains(emotion))
                return ValidationProblem($"Invalid emotion '{emotion}'.");

            momentsQuery = momentsQuery.Where(m =>
                m.Profiles.Any(p => p.Components.Any(c => c.EmotionId == emotion)));
        }

        if (query.FromUtc is DateTime fromUtc)
        {
            var from = ToUtcMinute(fromUtc);
            momentsQuery = momentsQuery.Where(m => m.MomentUtc >= from);
        }

        if (query.ToUtc is DateTime toUtc)
        {
            var to = ToUtcMinute(toUtc);
            momentsQuery = momentsQuery.Where(m => m.MomentUtc <= to);
        }

        var totalCount = await momentsQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var moments = await momentsQuery
            .OrderByDescending(x => x.MomentUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new EmotionMomentListResponse(
            moments.Select(m => m.ToResponse()).ToList(),
            totalCount,
            page,
            pageSize,
            totalPages);
    }

    [HttpGet("moments/{id:int}")]
    public async Task<ActionResult<EmotionMomentResponse>> GetMomentById(int id, CancellationToken cancellationToken)
    {
        var moment = await db.EmotionMoments
            .AsNoTracking()
            .Include(x => x.Profiles)
                .ThenInclude(x => x.Components)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return moment is null ? NotFound() : moment.ToResponse();
    }

    [HttpPut("moments/{id:int}")]
    public async Task<ActionResult<EmotionMomentResponse>> UpdateMoment(int id, [FromBody] EmotionMomentUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return ValidationProblem("Title is required.");

        var moment = await db.EmotionMoments
            .Include(x => x.Profiles)
                .ThenInclude(x => x.Components)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (moment is null)
            return NotFound();

        moment.Title = request.Title.Trim();
        moment.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        moment.MomentUtc = ToUtcMinute(request.MomentUtc);

        await db.SaveChangesAsync(cancellationToken);

        return moment.ToResponse();
    }

    [HttpPut("moments/{id:int}/blend-order")]
    public async Task<IActionResult> UpdateBlendOrder(int id, [FromBody] EmotionMomentBlendOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.BlendIds.Count == 0)
            return ValidationProblem("At least one blend id is required.");

        if (request.BlendIds.Count != request.BlendIds.Distinct().Count())
            return ValidationProblem("Blend ids must be unique.");

        var moment = await db.EmotionMoments
            .Include(x => x.Profiles)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (moment is null)
            return NotFound();

        var existingIds = moment.Profiles.Select(p => p.Id).Order().ToList();
        var requestedIds = request.BlendIds.Order().ToList();
        if (!existingIds.SequenceEqual(requestedIds))
            return ValidationProblem("Blend ids must exactly match the blends attached to this moment.");

        var orderLookup = request.BlendIds.Select((blendId, index) => new { blendId, index })
            .ToDictionary(x => x.blendId, x => x.index);

        foreach (var profile in moment.Profiles)
            profile.SortOrder = orderLookup[profile.Id];

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("moments/{momentId:int}/blends/{blendId:int}")]
    public async Task<IActionResult> DeleteBlend(int momentId, int blendId, CancellationToken cancellationToken)
    {
        var profile = await db.EmotionProfiles
            .FirstOrDefaultAsync(x => x.Id == blendId && x.MomentId == momentId, cancellationToken);

        if (profile is null)
            return NotFound();

        db.EmotionProfiles.Remove(profile);
        await db.SaveChangesAsync(cancellationToken);

        var remainingProfiles = await db.EmotionProfiles
            .Where(x => x.MomentId == momentId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < remainingProfiles.Count; i++)
            remainingProfiles[i].SortOrder = i;

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static DateTime ToUtcMinute(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, DateTimeKind.Utc);
    }

    private ActionResult ValidationProblem(string detail)
    {
        ModelState.AddModelError("request", detail);
        return base.ValidationProblem(ModelState);
    }
}
