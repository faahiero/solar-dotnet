using Solar.Domain.Common;
using Solar.Domain.Enums;

namespace Solar.Domain.Events;

public record GradeUpdatedDomainEvent(
    long AllocationId,
    long UserId,
    double FinalGrade,
    GradeSituation Situation
) : DomainEvent;

public record UserBlacklistedDomainEvent(
    string Cpf,
    string Reason,
    long? UserId
) : DomainEvent;

public record ExamAttemptCompletedDomainEvent(
    long AttemptId,
    long ExamId,
    long UserId,
    double Score,
    bool Passed
) : DomainEvent;
