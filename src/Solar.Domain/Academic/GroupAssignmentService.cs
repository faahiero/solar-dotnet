using Solar.Domain.Entities;

namespace Solar.Domain.Academic;

/// <summary>
/// Serviço de domínio para gestão de Trabalhos em Grupo (Assignments).
/// Mapeado a partir de app/models/group_assignment.rb e app/controllers/group_assignments_controller.rb.
/// </summary>
public class GroupAssignmentService
{
    /// <summary>
    /// Divide automaticamente alunos não alocados em grupos de tamanho especificado.
    /// </summary>
    public IReadOnlyList<GroupAssignment> AutoDistributeStudents(
        IEnumerable<long> unallocatedUserIds,
        int maxMembersPerGroup,
        long academicAllocationId,
        int existingGroupCount = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMembersPerGroup, 1);

        var students = unallocatedUserIds.ToList();
        var groups = new List<GroupAssignment>();
        int groupIndex = existingGroupCount + 1;

        for (int i = 0; i < students.Count; i += maxMembersPerGroup)
        {
            var chunk = students.Skip(i).Take(maxMembersPerGroup).ToList();
            var group = new GroupAssignment
            {
                GroupName = $"Grupo {groupIndex++}",
                AcademicAllocationId = academicAllocationId,
                GroupUpdatedAt = DateTime.UtcNow,
                Participants = chunk.Select(userId => new GroupParticipant
                {
                    UserId = userId,
                    ParticipantUpdatedAt = DateTime.UtcNow
                }).ToList()
            };
            groups.Add(group);
        }

        return groups;
    }
}
