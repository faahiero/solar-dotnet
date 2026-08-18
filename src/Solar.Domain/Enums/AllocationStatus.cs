namespace Solar.Domain.Enums;

/// <summary>
/// Status da matrícula / alocação de um usuário em uma turma ou oferta.
/// Mapeado a partir de Allocation_Pending, Allocation_Activated, etc. em config/environment.rb.
/// </summary>
public enum AllocationStatus
{
    Pending = 0,            // Solicitação de matrícula pendente
    Activated = 1,          // Matrícula ativa
    Cancelled = 2,          // Matrícula cancelada
    PendingReactivate = 3,  // Solicitação de reativação pendente
    Rejected = 4,           // Solicitação de matrícula rejeitada
    Merged = 5              // Matrícula aglutinada em outra turma
}
