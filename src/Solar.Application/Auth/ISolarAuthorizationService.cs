using Solar.Domain.Enums;

namespace Solar.Application.Auth;

public interface ISolarAuthorizationService
{
    Task<bool> HasGlobalAdminAccessAsync(long userId);
    Task<bool> CanManageCurriculumUnitAsync(long userId, long curriculumUnitId);
    Task<bool> CanGradeStudentAsync(long teacherOrTutorUserId, long studentUserId, long? groupId = null);
    Task<bool> CanAccessGroupAsync(long userId, long groupId, bool writeAccess = false);
    Task<bool> CanSubmitAssignmentAsync(long studentUserId, long assignmentId);
    Task<bool> HasPermissionOnAllocationTagAsync(long userId, long allocationTagId, string action);
    Task<List<ProfileType>> GetUserProfileTypesAsync(long userId);
}
