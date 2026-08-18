namespace Solar.Domain.Grading;

/// <summary>
/// Dados de entrada de uma atividade acadêmica (AcademicAllocation + AcademicAllocationUser)
/// para processamento de notas e frequência.
/// </summary>
public record GradingEvaluationInput
{
    public int ActivityId { get; init; }
    public string ActivityType { get; init; } = string.Empty; // Exam, Assignment, Discussion, etc.
    public string Name { get; init; } = string.Empty;

    public bool IsEvaluative { get; init; }
    public bool IsFrequency { get; init; }
    public bool IsFinalExam { get; init; }

    public double Weight { get; init; } = 1.0;
    public double FinalWeight { get; init; } = 100.0;
    public double? MaxWorkingHours { get; init; }

    public int? EquivalentActivityId { get; init; }

    public double? StudentGrade { get; init; }
    public double? StudentWorkingHours { get; init; }
    public bool IsIgnored { get; init; }

    /// <summary>
    /// Lista de submissões em atividades equivalentes para substituição por maior nota (se houver).
    /// </summary>
    public IReadOnlyList<GradingEvaluationInput> EquivalentSubmissions { get; init; } = [];

    /// <summary>
    /// Retorna a nota efetiva considerando equivalências (MAX entre a nota atual e notas equivalentes não ignoradas).
    /// </summary>
    public double? GetEffectiveGrade()
    {
        var grades = new List<double>();

        if (!IsIgnored && StudentGrade.HasValue)
        {
            grades.Add(StudentGrade.Value);
        }

        foreach (var eq in EquivalentSubmissions)
        {
            if (!eq.IsIgnored && eq.StudentGrade.HasValue)
            {
                grades.Add(eq.StudentGrade.Value);
            }
        }

        return grades.Count > 0 ? grades.Max() : null;
    }

    /// <summary>
    /// Retorna as horas de frequência efetivas considerando equivalências.
    /// </summary>
    public double GetEffectiveWorkingHours()
    {
        var hours = new List<double>();

        if (StudentWorkingHours.HasValue)
        {
            hours.Add(StudentWorkingHours.Value);
        }

        foreach (var eq in EquivalentSubmissions)
        {
            if (eq.StudentWorkingHours.HasValue)
            {
                hours.Add(eq.StudentWorkingHours.Value);
            }
        }

        return hours.Count > 0 ? hours.Max() : 0.0;
    }
}
