namespace Solar.Domain.Enums;

/// <summary>
/// Situação acadêmica final do aluno na turma.
/// Mapeado a partir de Allocation::Pending, Allocation::Approved, etc. em app/models/allocation.rb.
/// </summary>
public enum GradeSituation
{
    Pending = 0,            // Em andamento
    FinalExamPending = 1,   // Pendente de Prova Final (Recuperação / AF)
    Approved = 2,           // Aprovado por média direta
    FinalExamApproved = 3,  // Aprovado após Prova Final
    Failed = 4,             // Reprovado por nota
    FailedFrequency = 5,    // Reprovado por infrequência / faltas
    Undefined = 6           // Sem critérios definidos
}
