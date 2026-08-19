using System.Text.RegularExpressions;

namespace Solar.Domain.Administration;

public class UserBatchImportService
{
    public BatchImportResult ParseAndValidateCsv(string csvContent, HashSet<string>? existingCpfs = null)
    {
        var result = new BatchImportResult();
        existingCpfs ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return result;
        }

        var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return result;

        int rowNum = 0;
        bool isHeader = true;

        foreach (var rawLine in lines)
        {
            rowNum++;
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(new[] { ';', ',' }).Select(p => p.Trim().Trim('"')).ToArray();

            // Detecta cabeçalho
            if (isHeader)
            {
                isHeader = false;
                if (parts[0].Equals("nome", StringComparison.OrdinalIgnoreCase) ||
                    parts[0].Equals("name", StringComparison.OrdinalIgnoreCase) ||
                    parts.Any(p => p.Equals("cpf", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
            }

            result.TotalRows++;

            if (parts.Length < 3)
            {
                result.ErrorCount++;
                result.FailedRows.Add(new UserImportRow
                {
                    RowNumber = rowNum,
                    IsValid = false,
                    ErrorMessage = "Linha com colunas insuficientes. Esperado: Nome, CPF, Email, [Polo], [Perfil]"
                });
                continue;
            }

            string name = parts[0];
            string rawCpf = parts.Length > 1 ? parts[1] : "";
            string email = parts.Length > 2 ? parts[2] : "";
            string location = parts.Length > 3 ? parts[3] : "Fortaleza";
            string role = parts.Length > 4 ? parts[4] : "Aluno";

            string cleanCpf = Regex.Replace(rawCpf, @"[^\d]", "");

            // Validações
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("Nome é obrigatório.");
            }

            if (!IsValidCpf(cleanCpf))
            {
                errors.Add($"CPF '{rawCpf}' é inválido (deve conter 11 dígitos numéricos válidos).");
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                errors.Add($"E-mail '{email}' inválido.");
            }

            if (errors.Any())
            {
                result.ErrorCount++;
                result.FailedRows.Add(new UserImportRow
                {
                    RowNumber = rowNum,
                    Name = name,
                    Cpf = rawCpf,
                    Email = email,
                    Location = location,
                    Role = role,
                    IsValid = false,
                    ErrorMessage = string.Join(" ", errors)
                });
                continue;
            }

            if (existingCpfs.Contains(cleanCpf))
            {
                result.SkippedCount++;
                continue;
            }

            // Gera username a partir do nome
            string username = GenerateUsername(name, cleanCpf);

            var row = new UserImportRow
            {
                RowNumber = rowNum,
                Name = name,
                Username = username,
                Cpf = cleanCpf,
                Email = email,
                Location = string.IsNullOrWhiteSpace(location) ? "Fortaleza" : location,
                Role = string.IsNullOrWhiteSpace(role) ? "Aluno" : role,
                IsValid = true
            };

            result.SuccessCount++;
            result.ImportedRows.Add(row);
            existingCpfs.Add(cleanCpf);
        }

        return result;
    }

    public static bool IsValidCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;
        var clean = Regex.Replace(cpf, @"[^\d]", "");
        if (clean.Length != 11) return false;

        // Elimina CPFs com todos os dígitos iguais (ex: 111.111.111-11)
        if (new string(clean[0], 11) == clean) return false;

        int[] mult1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] mult2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCpf = clean.Substring(0, 9);
        int soma = 0;

        for (int i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * mult1[i];

        int resto = soma % 11;
        int dig1 = resto < 2 ? 0 : 11 - resto;

        tempCpf += dig1;
        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * mult2[i];

        resto = soma % 11;
        int dig2 = resto < 2 ? 0 : 11 - resto;

        return clean.EndsWith($"{dig1}{dig2}");
    }

    private static string GenerateUsername(string name, string cpf)
    {
        var cleanName = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]", "");
        var prefix = cleanName.Length > 8 ? cleanName.Substring(0, 8) : cleanName;
        var suffix = cpf.Length >= 4 ? cpf.Substring(cpf.Length - 4) : "0000";
        return $"{prefix}{suffix}";
    }
}
