using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Solar.WebApi.Logging;

public record StructuredLogEntry(
    string Id,
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? RequestMethod,
    string? RequestPath,
    int? StatusCode,
    double? ElapsedMs,
    string? Exception,
    string? TraceId,
    string? SourceContext,
    Dictionary<string, object?> Properties
);

/// <summary>
/// Sink personalizado do Serilog que armazena os últimos N eventos em um buffer circular
/// thread-safe em memória, permitindo visualização gráfica em tempo real no painel administrativo.
/// </summary>
public class SolarLogSink : ILogEventSink
{
    private static readonly ConcurrentQueue<StructuredLogEntry> _logs = new();
    private const int MaxCapacity = 2000;

    public void Emit(LogEvent logEvent)
    {
        var properties = new Dictionary<string, object?>();
        foreach (var prop in logEvent.Properties)
        {
            properties[prop.Key] = prop.Value?.ToString()?.Trim('"');
        }

        string? requestMethod = properties.TryGetValue("RequestMethod", out var rm) ? rm?.ToString() : null;
        string? requestPath = properties.TryGetValue("RequestPath", out var rp) ? rp?.ToString() : null;
        int? statusCode = properties.TryGetValue("StatusCode", out var sc) && int.TryParse(sc?.ToString(), out var parsedSc) ? parsedSc : null;
        double? elapsedMs = properties.TryGetValue("Elapsed", out var el) && double.TryParse(el?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedEl) ? parsedEl : null;
        string? sourceContext = properties.TryGetValue("SourceContext", out var src) ? src?.ToString() : null;

        var entry = new StructuredLogEntry(
            Id: Guid.NewGuid().ToString("N"),
            Timestamp: logEvent.Timestamp,
            Level: logEvent.Level.ToString(),
            Message: logEvent.RenderMessage(),
            RequestMethod: requestMethod,
            RequestPath: requestPath,
            StatusCode: statusCode,
            ElapsedMs: elapsedMs,
            Exception: logEvent.Exception?.ToString(),
            TraceId: logEvent.TraceId?.ToString(),
            SourceContext: sourceContext,
            Properties: properties
        );

        _logs.Enqueue(entry);

        while (_logs.Count > MaxCapacity)
        {
            _logs.TryDequeue(out _);
        }
    }

    public static IReadOnlyList<StructuredLogEntry> GetRecentLogs(int limit = 500, string? level = null, string? search = null)
    {
        var query = _logs.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(level) && !level.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l =>
                l.Message.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (l.RequestPath != null && l.RequestPath.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (l.RequestMethod != null && l.RequestMethod.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (l.Exception != null && l.Exception.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (l.StatusCode.HasValue && l.StatusCode.Value.ToString().Contains(search))
            );
        }

        return query.OrderByDescending(l => l.Timestamp).Take(limit).ToList();
    }

    public static void Clear()
    {
        _logs.Clear();
    }
}
