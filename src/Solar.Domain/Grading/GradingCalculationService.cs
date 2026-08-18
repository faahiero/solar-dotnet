using Solar.Domain.Enums;

namespace Solar.Domain.Grading;

/// <summary>
/// Serviço de domínio puro responsável pelo cálculo determinístico de notas,
/// frequência e situação acadêmica final no Solar LMS.
/// Substitui os métodos legados em Allocation.rb (parcial_grade_calculation, set_situation, calculate_working_hours).
/// </summary>
public class GradingCalculationService
{
    public GradingCalculationResult Calculate(
        IReadOnlyList<GradingEvaluationInput> activities,
        GradingCourseCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(criteria);

        // 1. Filtrar atividades regulares (não são equivalentes filhas)
        var regularActivities = activities
            .Where(a => a.EquivalentActivityId == null)
            .ToList();

        var evaluativeActivities = regularActivities
            .Where(a => a.IsEvaluative && !a.IsFinalExam)
            .ToList();

        var finalExamActivities = regularActivities
            .Where(a => a.IsEvaluative && a.IsFinalExam)
            .ToList();

        var frequencyActivities = regularActivities
            .Where(a => a.IsFrequency && !a.IsFinalExam)
            .ToList();

        // 2. Cálculo de Horas de Frequência
        double totalWorkingHours = frequencyActivities.Sum(a => a.GetEffectiveWorkingHours());
        totalWorkingHours = Math.Round(totalWorkingHours, 2);

        bool hasFrequencyConfig = criteria.TotalWorkingHours.HasValue && criteria.TotalWorkingHours.Value > 0;
        double minRequiredHours = hasFrequencyConfig && criteria.MinHoursPercentage.HasValue
            ? (criteria.MinHoursPercentage.Value * 0.01) * criteria.TotalWorkingHours!.Value
            : 0.0;

        bool isFrequencySufficient = !hasFrequencyConfig || (totalWorkingHours >= minRequiredHours);

        // 3. Cálculo da Média Parcial (ParcialGrade)
        double parcialGrade = 0.0;

        if (evaluativeActivities.Count > 0)
        {
            if (criteria.CalculationType == CalculationType.SimpleSum)
            {
                parcialGrade = evaluativeActivities.Sum(a => a.GetEffectiveGrade().GetValueOrDefault(0.0));
            }
            else
            {
                // Agrupamento por final_weight (fórmula ponderada do Solar)
                var groupsByFinalWeight = evaluativeActivities.GroupBy(a => a.FinalWeight);

                foreach (var group in groupsByFinalWeight)
                {
                    double sumWeightedGrades = group.Sum(a => a.GetEffectiveGrade().GetValueOrDefault(0.0) * a.Weight);
                    double sumWeights = group.Sum(a => a.Weight);

                    if (sumWeights > 0)
                    {
                        double groupContribution = (group.Key / 100.0) * (sumWeightedGrades / sumWeights);
                        parcialGrade += groupContribution;
                    }
                }
            }

            parcialGrade = Math.Round(parcialGrade, 2);
        }

        // 4. Elegibilidade e Cálculo de Prova Final (AF)
        bool hasPassingGradeDefined = criteria.PassingGrade.HasValue;
        bool requiresFinalExam = hasPassingGradeDefined && (parcialGrade < criteria.PassingGrade!.Value);

        bool meetsMinGradeForAF = !criteria.MinGradeToFinalExam.HasValue ||
                                  (parcialGrade >= criteria.MinGradeToFinalExam.Value);

        bool isEligibleForFinalExam = requiresFinalExam && meetsMinGradeForAF && isFrequencySufficient;

        double? finalExamGrade = null;
        double finalGrade = parcialGrade;

        if (isEligibleForFinalExam && finalExamActivities.Count > 0)
        {
            var completedFinalExams = finalExamActivities
                .Select(a => a.GetEffectiveGrade())
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();

            if (completedFinalExams.Count > 0)
            {
                finalExamGrade = Math.Round(completedFinalExams.Sum() / finalExamActivities.Count, 2);
                finalGrade = Math.Round((parcialGrade + finalExamGrade.Value) / 2.0, 2);
            }
        }

        // 5. Determinação da Situação Acadêmica (GradeSituation)
        GradeSituation situation = DetermineSituation(
            evaluativeActivities.Count > 0,
            frequencyActivities.Count > 0,
            isFrequencySufficient,
            hasPassingGradeDefined,
            parcialGrade,
            finalExamGrade,
            finalGrade,
            criteria);

        return new GradingCalculationResult
        {
            ParcialGrade = parcialGrade,
            FinalExamGrade = finalExamGrade,
            FinalGrade = finalGrade,
            TotalWorkingHours = totalWorkingHours,
            Situation = situation,
            IsFrequencySufficient = isFrequencySufficient,
            IsEligibleForFinalExam = isEligibleForFinalExam
        };
    }

    private static GradeSituation DetermineSituation(
        bool hasEvaluativeActivities,
        bool hasFrequencyActivities,
        bool isFrequencySufficient,
        bool hasPassingGradeDefined,
        double parcialGrade,
        double? finalExamGrade,
        double finalGrade,
        GradingCourseCriteria criteria)
    {
        // Regra 1: Reprovação por infrequência tem precedência quando há atividades de frequência
        if (hasFrequencyActivities && !isFrequencySufficient)
        {
            return GradeSituation.FailedFrequency;
        }

        // Regra 2: Disciplina com nota e atividades avaliativas
        if (hasPassingGradeDefined && hasEvaluativeActivities)
        {
            // Aprovado direto por média
            if (parcialGrade >= criteria.PassingGrade!.Value)
            {
                return GradeSituation.Approved;
            }

            // Não atingiu a nota mínima para ter direito à AF
            if (criteria.MinGradeToFinalExam.HasValue && parcialGrade < criteria.MinGradeToFinalExam.Value)
            {
                return GradeSituation.Failed;
            }

            // Tem direito a AF:
            if (!finalExamGrade.HasValue)
            {
                // Se existe AF na turma, fica pendente; senão, reprovado
                return criteria.HasFinalExamInOffering
                    ? GradeSituation.FinalExamPending
                    : GradeSituation.Failed;
            }

            // Fez a AF: verificar nota mínima da AF
            if (criteria.MinFinalExamGrade.HasValue && finalExamGrade.Value < criteria.MinFinalExamGrade.Value)
            {
                return GradeSituation.Failed;
            }

            // Verificar média final pós-AF
            double requiredFinalGrade = criteria.FinalExamPassingGrade ?? criteria.PassingGrade.Value;
            return finalGrade >= requiredFinalGrade
                ? GradeSituation.FinalExamApproved
                : GradeSituation.Failed;
        }

        // Regra 3: Disciplina apenas de frequência (sem nota) e cumpriu as horas
        if (hasFrequencyActivities && !hasEvaluativeActivities && isFrequencySufficient)
        {
            return GradeSituation.Approved;
        }

        // Regra 4: Sem data atingida ou indefinido
        if (!criteria.SituationDateReached)
        {
            return GradeSituation.Pending;
        }

        return GradeSituation.Undefined;
    }
}
