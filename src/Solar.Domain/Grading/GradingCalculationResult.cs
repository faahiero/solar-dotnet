using Solar.Domain.Enums;

namespace Solar.Domain.Grading;

/// <summary>
/// Resultado consolidado do cálculo de notas, frequência e situação acadêmica.
/// </summary>
public record GradingCalculationResult
{
    public double ParcialGrade { get; init; }
    public double? FinalExamGrade { get; init; }
    public double FinalGrade { get; init; }
    public double TotalWorkingHours { get; init; }

    public GradeSituation Situation { get; init; }

    public bool IsFrequencySufficient { get; init; }
    public bool IsEligibleForFinalExam { get; init; }

    public string SituationDescription => Situation switch
    {
        GradeSituation.Pending => "Em andamento",
        GradeSituation.FinalExamPending => "Pendente de Prova Final",
        GradeSituation.Approved => "Aprovado por Média",
        GradeSituation.FinalExamApproved => "Aprovado na Prova Final",
        GradeSituation.Failed => "Reprovado por Nota",
        GradeSituation.FailedFrequency => "Reprovado por Infrequência",
        GradeSituation.Undefined => "Indefinido",
        _ => "Desconhecido"
    };
}
