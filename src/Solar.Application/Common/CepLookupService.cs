using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Solar.Application.Common;

public record CepResultDto(
    bool Found,
    string Cep,
    string? Logradouro,
    string? Complemento,
    string? Bairro,
    string? Localidade,
    string? Uf,
    string? Mensagem
);

public class CepLookupService
{
    private readonly HttpClient _httpClient;

    public CepLookupService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<CepResultDto> LookupAsync(string cep, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cep))
        {
            return new CepResultDto(false, string.Empty, null, null, null, null, null, "CEP não informado.");
        }

        var cleanCep = Regex.Replace(cep, @"\D", "");
        if (cleanCep.Length != 8)
        {
            return new CepResultDto(false, cep, null, null, null, null, null, "CEP deve conter exatamente 8 dígitos.");
        }

        try
        {
            var jsonString = await _httpClient.GetStringAsync($"https://viacep.com.br/ws/{cleanCep}/json/", cancellationToken);
            var node = JsonNode.Parse(jsonString);

            if (node == null)
            {
                return new CepResultDto(false, cleanCep, null, null, null, null, null, "Resposta inválida do serviço de CEP.");
            }

            var erroNode = node["erro"];
            if (erroNode != null)
            {
                bool isErro = erroNode.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
                if (isErro)
                {
                    return new CepResultDto(false, cleanCep, null, null, null, null, null, "CEP não localizado na base nacional dos Correios.");
                }
            }

            string? foundCep = node["cep"]?.ToString();
            string? logradouro = node["logradouro"]?.ToString();
            string? complemento = node["complemento"]?.ToString();
            string? bairro = node["bairro"]?.ToString();
            string? localidade = node["localidade"]?.ToString();
            string? uf = node["uf"]?.ToString();

            return new CepResultDto(
                Found: true,
                Cep: foundCep ?? cleanCep,
                Logradouro: logradouro,
                Complemento: complemento,
                Bairro: bairro,
                Localidade: localidade,
                Uf: uf,
                Mensagem: "Endereço localizado com sucesso."
            );
        }
        catch (Exception ex)
        {
            return new CepResultDto(false, cleanCep, null, null, null, null, null, $"Não foi possível consultar o CEP no momento: {ex.Message}");
        }
    }
}
