namespace Solar.Domain.Enums;

/// <summary>
/// Status de entrega / avaliação de uma atividade pelo aluno ou grupo (AcademicAllocationUser).
/// Mapeado a partir de AcademicAllocationUser::STATUS em app/models/academic_allocation_user.rb.
/// </summary>
public enum SubmissionStatus
{
    Empty = 0,          // Não submetido
    Sent = 1,           // Submetido / aguardando correção
    Evaluated = 2,      // Avaliado pelo professor ou autocorreção
    WithoutGroup = 3    // Trabalho em grupo onde o aluno está sem grupo
}
