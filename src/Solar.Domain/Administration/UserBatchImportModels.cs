namespace Solar.Domain.Administration;

public class UserImportRow
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Aluno";
    public string Location { get; set; } = "Fortaleza";
    public bool IsValid { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public class BatchImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
    public List<UserImportRow> ImportedRows { get; set; } = new();
    public List<UserImportRow> FailedRows { get; set; } = new();
    public string SummaryMessage => $"Processamento concluído: {SuccessCount} usuários importados com sucesso, {SkippedCount} existentes ignorados, {ErrorCount} erros encontrados em {TotalRows} linhas.";
}
