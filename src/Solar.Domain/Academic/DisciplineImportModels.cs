namespace Solar.Domain.Academic;

public record DisciplineImportItem
{
    public long SourceAcademicAllocationId { get; init; }
    public string ToolType { get; init; } = string.Empty; // Exam, Assignment, Discussion, LessonModule, etc.
    public string Name { get; init; } = string.Empty;
    public bool IsEvaluative { get; init; }
    public bool IsSupported { get; init; } = true;
    public bool HasConflict { get; init; }

    public DateOnly? OriginalStartDate { get; init; }
    public DateOnly? OriginalEndDate { get; init; }
    public DateOnly? ShiftedStartDate { get; init; }
    public DateOnly? ShiftedEndDate { get; init; }
}

public record DisciplineImportPreview
{
    public IReadOnlyList<DisciplineImportItem> Items { get; init; } = [];
    public bool HasSpanWarning { get; init; }
    public int SourceDays { get; init; }
    public int DestOfferDays { get; init; }
}

public record DisciplineImportResult
{
    public int ImportedCount { get; init; }
    public int SkippedCount { get; init; }
    public bool WeightWarning { get; init; }
    public IReadOnlyList<string> ImportedToolNames { get; init; } = [];
    public IReadOnlyList<string> SkippedToolNames { get; init; } = [];
}
