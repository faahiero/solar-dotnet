namespace Solar.Domain.Enums;

/// <summary>
/// Critério de cálculo da nota final da prova quando o aluno realiza múltiplas tentativas.
/// Mapeado a partir de Exam::GREATER, Exam::AVERAGE, Exam::LAST em app/models/exam.rb.
/// </summary>
public enum AttemptsCalculationCriterion
{
    Greater = 0,    // Maior nota entre as tentativas válidas
    Average = 1,    // Média aritmética das notas das tentativas
    Last = 2        // Nota da última tentativa realizada
}
