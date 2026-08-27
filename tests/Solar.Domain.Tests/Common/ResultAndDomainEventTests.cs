using FluentAssertions;
using Solar.Domain.Common;
using Solar.Domain.Enums;
using Solar.Domain.Events;
using Xunit;

namespace Solar.Domain.Tests.Common;

public class ResultAndDomainEventTests
{
    [Fact]
    public void Result_Success_Should_Have_IsSuccess_True_And_NoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Result_Failure_Should_Have_IsFailure_True_And_ContainError()
    {
        var error = Error.NotFound("Course.NotFound", "Curso não encontrado.");
        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void GenericResult_Success_Should_Return_Value()
    {
        var result = Result<string>.Success("Solar LMS");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Solar LMS");
    }

    [Fact]
    public void GenericResult_Failure_Should_Throw_On_Accessing_Value()
    {
        var error = Error.Validation("User.InvalidCpf", "CPF inválido.");
        var result = Result<string>.Failure(error);

        result.IsFailure.Should().BeTrue();
        var act = () => _ = result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void BaseEntity_Should_Collect_And_Clear_DomainEvents()
    {
        var entity = new TestEntity();
        var domainEvent = new GradeUpdatedDomainEvent(10, 1, 9.5, GradeSituation.Approved);

        entity.AddDomainEvent(domainEvent);

        entity.DomainEvents.Should().HaveCount(1);
        entity.DomainEvents.Should().Contain(domainEvent);

        entity.ClearDomainEvents();
        entity.DomainEvents.Should().BeEmpty();
    }

    private class TestEntity : BaseEntity
    {
        public long Id { get; set; }
    }
}
