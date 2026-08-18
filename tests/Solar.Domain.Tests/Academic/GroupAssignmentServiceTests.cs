using FluentAssertions;
using Solar.Domain.Academic;
using Xunit;

namespace Solar.Domain.Tests.Academic;

public class GroupAssignmentServiceTests
{
    private readonly GroupAssignmentService _groupService = new();

    [Fact]
    public void AutoDistributeStudents_Should_Chunk_Students_Into_Balanced_Groups()
    {
        // Arrange (10 alunos divididos em grupos de no máximo 4 alunos -> 3 grupos: 4, 4, 2)
        var studentIds = Enumerable.Range(101, 10).Select(i => (long)i).ToList();
        int maxMembers = 4;
        long academicAllocationId = 50;

        // Act
        var groups = _groupService.AutoDistributeStudents(studentIds, maxMembers, academicAllocationId);

        // Assert
        groups.Should().HaveCount(3);
        groups[0].GroupName.Should().Be("Grupo 1");
        groups[0].Participants.Should().HaveCount(4);

        groups[1].GroupName.Should().Be("Grupo 2");
        groups[1].Participants.Should().HaveCount(4);

        groups[2].GroupName.Should().Be("Grupo 3");
        groups[2].Participants.Should().HaveCount(2);
    }
}
