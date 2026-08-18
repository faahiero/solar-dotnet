using FluentAssertions;
using Solar.Domain.Discussions;
using Solar.Domain.Entities;
using Xunit;

namespace Solar.Domain.Tests.Discussions;

public class DiscussionTreeServiceTests
{
    private readonly DiscussionTreeService _treeService = new();

    [Fact]
    public void CalculatePostLevel_Should_Return_Level_1_For_Root_Post()
    {
        // Act
        var level = _treeService.CalculatePostLevel(null);

        // Assert
        level.Should().Be(1);
    }

    [Fact]
    public void CalculatePostLevel_Should_Increment_Level_Up_To_Max_7()
    {
        // Arrange
        var parentLevel3 = new DiscussionPost { Level = 3 };
        var parentLevel7 = new DiscussionPost { Level = 7 };

        // Act
        var childLevel = _treeService.CalculatePostLevel(parentLevel3);
        var cappedLevel = _treeService.CalculatePostLevel(parentLevel7);

        // Assert
        childLevel.Should().Be(4);
        cappedLevel.Should().Be(7); // Capped at 7 (Discussion_Post_Max_Indent_Level)
    }

    [Fact]
    public void CanDeletePost_Should_Return_False_When_Post_Has_Published_Children()
    {
        // Arrange
        var post = new DiscussionPost
        {
            Id = 1,
            Children = [new() { Id = 2, Draft = false }]
        };

        // Act
        var canDelete = _treeService.CanDeletePost(post);

        // Assert
        canDelete.Should().BeFalse();
    }

    [Fact]
    public void CanDeletePost_Should_Return_True_When_Post_Has_Only_Draft_Children_Or_No_Children()
    {
        // Arrange
        var postWithDraftChild = new DiscussionPost
        {
            Id = 1,
            Children = [new() { Id = 2, Draft = true }]
        };
        var postWithoutChildren = new DiscussionPost { Id = 3 };

        // Act & Assert
        _treeService.CanDeletePost(postWithDraftChild).Should().BeTrue();
        _treeService.CanDeletePost(postWithoutChildren).Should().BeTrue();
    }

    [Fact]
    public void BuildTree_Should_Correctly_Structure_Nested_Replies()
    {
        // Arrange
        var root1 = new DiscussionPost { Id = 1, Content = "Tópico Raiz", ParentId = null, CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var reply1 = new DiscussionPost { Id = 2, Content = "Resposta 1", ParentId = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var reply1_1 = new DiscussionPost { Id = 3, Content = "Resposta da Resposta", ParentId = 2, CreatedAt = DateTime.UtcNow };

        var allPosts = new List<DiscussionPost> { root1, reply1, reply1_1 };

        // Act
        var tree = _treeService.BuildTree(allPosts);

        // Assert
        tree.Should().HaveCount(1);
        tree[0].Post.Id.Should().Be(1);
        tree[0].Replies.Should().HaveCount(1);
        tree[0].Replies[0].Post.Id.Should().Be(2);
        tree[0].Replies[0].Replies.Should().HaveCount(1);
        tree[0].Replies[0].Replies[0].Post.Id.Should().Be(3);
    }

    [Fact]
    public void CanUserInteract_Should_Allow_Responsible_In_Extra_Time_Window()
    {
        // Arrange: Prazo final foi há 3 dias (encerrado para alunos, mas dentro dos 7 dias extras para professores)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var discussion = new Discussion
        {
            Schedule = new Schedule
            {
                StartDate = today.AddDays(-10),
                EndDate = today.AddDays(-3)
            }
        };

        // Act & Assert
        _treeService.CanUserInteract(discussion, isResponsible: false, today).Should().BeFalse(); // Aluno bloqueado
        _treeService.CanUserInteract(discussion, isResponsible: true, today).Should().BeTrue();  // Professor liberado
    }
}
