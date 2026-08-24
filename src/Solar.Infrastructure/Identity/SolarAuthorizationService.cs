using Microsoft.EntityFrameworkCore;
using Solar.Application.Auth;
using Solar.Domain.Academic;
using Solar.Domain.Enums;
using Solar.Infrastructure.Persistence;

namespace Solar.Infrastructure.Identity;

public class SolarAuthorizationService : ISolarAuthorizationService
{
    private readonly SolarDbContext _db;
    private readonly AllocationTagScopeService _scopeService;

    public SolarAuthorizationService(SolarDbContext db, AllocationTagScopeService scopeService)
    {
        _db = db;
        _scopeService = scopeService;
    }

    public async Task<bool> HasGlobalAdminAccessAsync(long userId)
    {
        if (userId <= 0) return false;

        var user = await _db.Users.FindAsync(userId);
        if (user == null || !user.Active) return false;

        var hasAdminProfile = await _db.Allocations
            .Where(a => a.UserId == userId && a.Status == AllocationStatus.Activated)
            .Include(a => a.Profile)
            .AnyAsync(a => a.Profile != null && (a.Profile.Types.HasFlag(ProfileType.Admin) || a.Profile.Name.Contains("Admin", StringComparison.OrdinalIgnoreCase)));

        return hasAdminProfile;
    }

    public async Task<bool> CanManageCurriculumUnitAsync(long userId, long curriculumUnitId)
    {
        if (await HasGlobalAdminAccessAsync(userId)) return true;

        var teacherOrCoordinatorAllocations = await _db.Allocations
            .Where(a => a.UserId == userId && a.Status == AllocationStatus.Activated)
            .Include(a => a.Profile)
            .Include(a => a.AllocationTag)
            .Where(a => a.Profile != null && (
                a.Profile.Types.HasFlag(ProfileType.ClassResponsible) ||
                a.Profile.Types.HasFlag(ProfileType.Coordinator) ||
                a.Profile.Types.HasFlag(ProfileType.Editor)
            ))
            .ToListAsync();

        if (!teacherOrCoordinatorAllocations.Any()) return false;

        var offerIds = await _db.Offers
            .Where(o => o.CurriculumUnitId == curriculumUnitId)
            .Select(o => o.Id)
            .ToListAsync();

        var tagOfferIds = teacherOrCoordinatorAllocations
            .Where(a => a.AllocationTag != null && a.AllocationTag.OfferId.HasValue)
            .Select(a => (long)a.AllocationTag!.OfferId!.Value)
            .ToHashSet();

        return offerIds.Any(id => tagOfferIds.Contains(id));
    }

    public async Task<bool> CanGradeStudentAsync(long teacherOrTutorUserId, long studentUserId, long? groupId = null)
    {
        if (await HasGlobalAdminAccessAsync(teacherOrTutorUserId)) return true;

        var staffAllocations = await _db.Allocations
            .Where(a => a.UserId == teacherOrTutorUserId && a.Status == AllocationStatus.Activated)
            .Include(a => a.Profile)
            .Include(a => a.AllocationTag)
            .Where(a => a.Profile != null && (
                a.Profile.Types.HasFlag(ProfileType.ClassResponsible) ||
                a.Profile.Types.HasFlag(ProfileType.Observer) ||
                a.Profile.Name.Contains("Tutor", StringComparison.OrdinalIgnoreCase) ||
                a.Profile.Name.Contains("Prof", StringComparison.OrdinalIgnoreCase)
            ))
            .ToListAsync();

        if (!staffAllocations.Any()) return false;

        var studentAllocations = await _db.Allocations
            .Where(a => a.UserId == studentUserId && a.Status == AllocationStatus.Activated)
            .Include(a => a.AllocationTag)
            .ToListAsync();

        if (!studentAllocations.Any()) return false;

        // Verifica intersecção de tags de alocação ou ofertas em comum
        var staffOfferIds = staffAllocations
            .Where(a => a.AllocationTag != null && a.AllocationTag.OfferId.HasValue)
            .Select(a => (long)a.AllocationTag!.OfferId!.Value)
            .ToHashSet();

        var studentOfferIds = studentAllocations
            .Where(a => a.AllocationTag != null && a.AllocationTag.OfferId.HasValue)
            .Select(a => (long)a.AllocationTag!.OfferId!.Value)
            .ToHashSet();

        return staffOfferIds.Overlaps(studentOfferIds);
    }

    public async Task<bool> CanAccessGroupAsync(long userId, long groupId, bool writeAccess = false)
    {
        if (await HasGlobalAdminAccessAsync(userId)) return true;

        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return false;

        var allocations = await _db.Allocations
            .Where(a => a.UserId == userId && a.Status == AllocationStatus.Activated)
            .Include(a => a.Profile)
            .Include(a => a.AllocationTag)
            .ToListAsync();

        if (writeAccess)
        {
            // Apenas docentes e administradores podem modificar o grupo
            allocations = allocations.Where(a => a.Profile != null && (
                a.Profile.Types.HasFlag(ProfileType.ClassResponsible) ||
                a.Profile.Types.HasFlag(ProfileType.Coordinator) ||
                a.Profile.Types.HasFlag(ProfileType.Admin)
            )).ToList();
        }

        var matchingGroup = allocations.Any(a =>
            a.AllocationTag != null && (
                a.AllocationTag.GroupId == groupId ||
                (a.AllocationTag.OfferId.HasValue && a.AllocationTag.OfferId.Value == group.OfferId)
            )
        );

        return matchingGroup;
    }

    public async Task<bool> CanSubmitAssignmentAsync(long studentUserId, long assignmentId)
    {
        if (await HasGlobalAdminAccessAsync(studentUserId)) return true;

        var isStudentActive = await _db.Allocations
            .Where(a => a.UserId == studentUserId && a.Status == AllocationStatus.Activated)
            .AnyAsync();

        return isStudentActive;
    }

    public async Task<bool> HasPermissionOnAllocationTagAsync(long userId, long allocationTagId, string action)
    {
        if (await HasGlobalAdminAccessAsync(userId)) return true;

        var userTagIds = await _db.Allocations
            .Where(a => a.UserId == userId && a.Status == AllocationStatus.Activated && a.AllocationTagId.HasValue)
            .Select(a => a.AllocationTagId!.Value)
            .ToListAsync();

        var relatedTaggables = await _db.RelatedTaggables.ToListAsync();
        var allRelatedTags = new HashSet<long>();
        foreach (var tagId in userTagIds)
        {
            var related = _scopeService.GetRelatedTagIds(relatedTaggables, tagId);
            foreach (var r in related) allRelatedTags.Add(r);
        }

        return allRelatedTags.Contains(allocationTagId);
    }

    public async Task<List<ProfileType>> GetUserProfileTypesAsync(long userId)
    {
        var profiles = await _db.Allocations
            .Where(a => a.UserId == userId && a.Status == AllocationStatus.Activated)
            .Include(a => a.Profile)
            .Select(a => a.Profile != null ? a.Profile.Types : ProfileType.NoType)
            .Distinct()
            .ToListAsync();

        return profiles;
    }
}
