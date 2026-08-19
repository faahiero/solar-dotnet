using System;
using System.IO;

namespace Solar.Infrastructure.Configuration;

/// <summary>
/// Carregador seguro de variáveis de ambiente a partir de arquivos .env para desenvolvimento local.
/// Respeita a precedência de variáveis já injetadas pelo sistema/nuvem.
/// </summary>
public static class DotEnvLoader
{
    public static void Load(string? directory = null)
    {
        var currentDir = directory ?? Directory.GetCurrentDirectory();
        var envPath = FindEnvFile(currentDir);
        
        if (string.IsNullOrEmpty(envPath) || !File.Exists(envPath))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            // Remove aspas simples ou duplas envolventes
            if (value.Length >= 2 && 
                ((value.StartsWith('"') && value.EndsWith('"')) || 
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            // Não sobrescreve variáveis que já foram explicitamente definidas no sistema operacional/container
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindEnvFile(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 4 && dir != null; i++)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
