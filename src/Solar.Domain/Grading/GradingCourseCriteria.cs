using Solar.Domain.Enums;

namespace Solar.Domain.Grading;

/// <summary>
/// Critérios de avaliação, média mínima e frequência definidos no Curso, Disciplina ou Oferta.
/// Mapeado a partir de Course, CurriculumUnit e Offer em app/models/allocation.rb.
/// </summary>
public record GradingCourseCriteria
{
    public double? PassingGrade { get; init; } = 7.0;             // Média de aprovação direta (ex: 7.0)
    public double? MinGradeToFinalExam { get; init; } = 3.0;      // Nota mínima para ter direito à AF (ex: 3.0 ou 4.0)
    public double? MinFinalExamGrade { get; init; }              // Nota mínima que deve tirar na AF
    public double? FinalExamPassingGrade { get; init; } = 5.0;    // Média final necessária após AF (ex: 5.0)

    public double? TotalWorkingHours { get; init; }               // Carga horária total da disciplina (ex: 64h)
    public double? MinHoursPercentage { get; init; } = 75.0;      // % mínimo de frequência (ex: 75%)

    public CalculationType CalculationType { get; init; } = CalculationType.WeightedFormula;
    public bool SituationDateReached { get; init; } = true;       // Se a data de fechamento já chegou
    public bool HasFinalExamInOffering { get; init; } = true;     // Se existe pelo menos uma Prova Final configurada
}
