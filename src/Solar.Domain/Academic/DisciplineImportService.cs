namespace Solar.Domain.Academic;

/// <summary>
/// Serviço de domínio para clonagem / importação de conteúdo de uma turma/oferta para outra,
/// com reajuste temporal automático de cronogramas para o novo semestre.
/// Mapeado a partir de app/services/discipline_import_service.rb.
/// </summary>
public class DisciplineImportService
{
    public static readonly HashSet<string> UnsupportedTypes = ["Webconference"];

    /// <summary>
    /// Desloca uma data da oferta de origem para o período da oferta destino.
    /// Se a data ultrapassar a data final da nova oferta, ajusta para a data final.
    /// </summary>
    public DateOnly ShiftDate(
        DateOnly sourceDate,
        DateOnly sourceOfferStart,
        DateOnly destOfferStart,
        DateOnly? destOfferEnd = null)
    {
        int dayOffset = destOfferStart.DayNumber - sourceOfferStart.DayNumber;
        var shifted = sourceDate.AddDays(dayOffset);

        if (destOfferEnd.HasValue && shifted > destOfferEnd.Value)
        {
            return destOfferEnd.Value;
        }

        return shifted;
    }

    /// <summary>
    /// Gera a prévia dos itens da disciplina com datas reajustadas e status de suporte.
    /// </summary>
    public DisciplineImportPreview GeneratePreview(
        IReadOnlyList<DisciplineImportItem> sourceItems,
        DateOnly sourceOfferStart,
        DateOnly sourceOfferEnd,
        DateOnly destOfferStart,
        DateOnly destOfferEnd,
        IReadOnlySet<string> existingDestNames)
    {
        ArgumentNullException.ThrowIfNull(sourceItems);
        ArgumentNullException.ThrowIfNull(existingDestNames);

        var previewItems = new List<DisciplineImportItem>();

        foreach (var item in sourceItems)
        {
            bool supported = !UnsupportedTypes.Contains(item.ToolType);
            bool conflict = existingDestNames.Contains(item.Name.Trim().ToLowerInvariant());

            DateOnly? shiftedStart = item.OriginalStartDate.HasValue && supported
                ? ShiftDate(item.OriginalStartDate.Value, sourceOfferStart, destOfferStart, destOfferEnd)
                : null;

            DateOnly? shiftedEnd = item.OriginalEndDate.HasValue && supported
                ? ShiftDate(item.OriginalEndDate.Value, sourceOfferStart, destOfferStart, destOfferEnd)
                : null;

            previewItems.Add(item with
            {
                IsSupported = supported,
                HasConflict = conflict,
                ShiftedStartDate = shiftedStart,
                ShiftedEndDate = shiftedEnd
            });
        }

        int sourceDays = sourceOfferEnd.DayNumber - sourceOfferStart.DayNumber;
        int destDays = destOfferEnd.DayNumber - destOfferStart.DayNumber;
        bool spanWarning = sourceDays > destDays;

        return new DisciplineImportPreview
        {
            Items = previewItems,
            HasSpanWarning = spanWarning,
            SourceDays = sourceDays,
            DestOfferDays = destDays
        };
    }

    /// <summary>
    /// Valida a consistência de pesos avaliativos pós-importação (a soma dos pesos finais distintos deve ser 100).
    /// </summary>
    public bool ValidateEvaluativeWeights(IEnumerable<double> distinctFinalWeights)
    {
        var list = distinctFinalWeights.ToList();
        if (list.Count == 0) return true;
        return Math.Abs(list.Sum() - 100.0) < 0.001;
    }
}
