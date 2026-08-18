using System.Security.Cryptography;
using System.Text;

namespace Solar.Infrastructure.Integrations.BigBlueButton;

public record BigBlueButtonServerConfig
{
    public string ServerUrl { get; init; } = string.Empty;
    public string SharedSecret { get; init; } = string.Empty;
    public int MaxCapacity { get; init; } = 100;
}

public record CreateMeetingRequest
{
    public string MeetingId { get; init; } = string.Empty;
    public string MeetingName { get; init; } = string.Empty;
    public string ModeratorPassword { get; init; } = "mp";
    public string AttendeePassword { get; init; } = "ap";
    public bool Record { get; init; } = true;
    public string? WelcomeMessage { get; init; }
    public string? LogoutUrl { get; init; }
}

public class BigBlueButtonClient
{
    private readonly BigBlueButtonServerConfig _config;

    public BigBlueButtonClient(BigBlueButtonServerConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Gera a URL de criação de reunião com checksum de segurança SHA1.
    /// Mapeado a partir do protocolo oficial BigBlueButton e gem bigbluebutton_api.
    /// </summary>
    public string BuildCreateMeetingUrl(CreateMeetingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var queryParams = new Dictionary<string, string>
        {
            { "name", Uri.EscapeDataString(request.MeetingName) },
            { "meetingID", Uri.EscapeDataString(request.MeetingId) },
            { "attendeePW", Uri.EscapeDataString(request.AttendeePassword) },
            { "moderatorPW", Uri.EscapeDataString(request.ModeratorPassword) },
            { "record", request.Record.ToString().ToLowerInvariant() }
        };

        if (!string.IsNullOrEmpty(request.WelcomeMessage))
        {
            queryParams.Add("welcome", Uri.EscapeDataString(request.WelcomeMessage));
        }

        if (!string.IsNullOrEmpty(request.LogoutUrl))
        {
            queryParams.Add("logoutURL", Uri.EscapeDataString(request.LogoutUrl));
        }

        string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        string checksum = ComputeChecksum("create", queryString, _config.SharedSecret);

        string baseUrl = _config.ServerUrl.TrimEnd('/');
        return $"{baseUrl}/api/create?{queryString}&checksum={checksum}";
    }

    /// <summary>
    /// Gera a URL de entrada de participante na reunião.
    /// </summary>
    public string BuildJoinMeetingUrl(string meetingId, string fullName, string password)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "fullName", Uri.EscapeDataString(fullName) },
            { "meetingID", Uri.EscapeDataString(meetingId) },
            { "password", Uri.EscapeDataString(password) }
        };

        string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        string checksum = ComputeChecksum("join", queryString, _config.SharedSecret);

        string baseUrl = _config.ServerUrl.TrimEnd('/');
        return $"{baseUrl}/api/join?{queryString}&checksum={checksum}";
    }

    /// <summary>
    /// Calcula o checksum SHA-1 padrão do BigBlueButton: SHA1(callName + queryString + sharedSecret).
    /// </summary>
    public static string ComputeChecksum(string callName, string queryString, string sharedSecret)
    {
        string raw = $"{callName}{queryString}{sharedSecret}";
        byte[] hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hashBytes);
    }
}
