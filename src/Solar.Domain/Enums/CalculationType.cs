namespace Solar.Domain.Enums;

/// <summary>
/// Tipo de cálculo de notas configurado na AllocationTag / Turma.
/// Mapeado a partir de allocation_tag.calculation_type em app/models/allocation.rb.
/// </summary>
public enum CalculationType
{
    WeightedFormula = 0,    // (final_weight / 100) * SUM(grade * weight) / SUM(weight)
    SimpleSum = 1           // SUM(grade)
}
