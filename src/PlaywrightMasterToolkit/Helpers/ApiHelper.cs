using System.Text.Json;
using Microsoft.Playwright;

namespace PlaywrightMasterToolkit.Helpers;

/// <summary>
/// Helper para execução de requisições HTTP e operações de teste híbrido (API seeding) via Playwright.
/// </summary>
public class ApiHelper
{
    private readonly IAPIRequestContext _requestContext;
    private readonly string _apiBaseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Inicializa uma nova instância de ApiHelper com o contexto de requisições e a URL base da API.
    /// </summary>
    /// <param name="requestContext">Contexto de requisições HTTP do Playwright.</param>
    /// <param name="apiBaseUrl">URL base do endpoint da API.</param>
    public ApiHelper(IAPIRequestContext requestContext, string apiBaseUrl)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _apiBaseUrl = apiBaseUrl ?? string.Empty;
    }

    /// <summary>
    /// Realiza a autenticação direta via API e extrai o token JWT retornado no payload.
    /// </summary>
    /// <param name="user">Nome de usuário ou e-mail de acesso.</param>
    /// <param name="pass">Senha de acesso.</param>
    /// <param name="loginEndpoint">Caminho relativo do endpoint de login. Padrão é "/api/v1/auth/login".</param>
    /// <returns>Token de autenticação retornado pela API.</returns>
    /// <exception cref="HttpRequestException">Lançada caso a API retorne código de erro HTTP com corpo da resposta.</exception>
    /// <exception cref="InvalidOperationException">Lançada caso o token não seja localizado na estrutura da resposta.</exception>
    public async Task<string> LoginViaApiAsync(string user, string pass, string loginEndpoint = "/api/v1/auth/login")
    {
        string url = CombineUrl(_apiBaseUrl, loginEndpoint);
        var payload = new Dictionary<string, string>
        {
            ["username"] = user,
            ["email"] = user,
            ["password"] = pass
        };

        var response = await _requestContext.PostAsync(url, new()
        {
            DataObject = payload,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        });

        string bodyText = await response.TextAsync();
        if (!response.Ok)
        {
            throw new HttpRequestException($"Falha no LoginViaApiAsync ({response.Status} {response.StatusText}): {bodyText}");
        }

        using var doc = JsonDocument.Parse(bodyText);
        var root = doc.RootElement;

        if (TryExtractToken(root, out string? token))
        {
            return token!;
        }

        throw new InvalidOperationException($"Não foi possível localizar o campo de token na resposta da API: {bodyText}");
    }

    /// <summary>
    /// Injeta um registro de teste diretamente via API (Data Seeding) com desserialização genérica da resposta.
    /// </summary>
    /// <typeparam name="T">Tipo do objeto retornado pela API.</typeparam>
    /// <param name="endpoint">Endpoint relativo do recurso.</param>
    /// <param name="payload">Objeto contendo a carga útil a ser serializada no corpo da requisição.</param>
    /// <param name="token">Token Bearer opcional para requisições autenticadas.</param>
    /// <returns>Objeto retornado e desserializado pela API.</returns>
    /// <exception cref="HttpRequestException">Lançada caso a API retorne código de erro HTTP com corpo da resposta.</exception>
    /// <exception cref="InvalidOperationException">Lançada se a resposta não puder ser convertida para o tipo T.</exception>
    public async Task<T> SeedRecordViaApiAsync<T>(string endpoint, object payload, string? token = null)
    {
        string url = CombineUrl(_apiBaseUrl, endpoint);
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            headers["Authorization"] = $"Bearer {token}";
        }

        var response = await _requestContext.PostAsync(url, new()
        {
            DataObject = payload,
            Headers = headers
        });

        string bodyText = await response.TextAsync();
        if (!response.Ok)
        {
            throw new HttpRequestException($"Falha no SeedRecordViaApiAsync ao chamar '{url}' ({response.Status} {response.StatusText}): {bodyText}");
        }

        var result = JsonSerializer.Deserialize<T>(bodyText, JsonOptions);
        if (result == null)
        {
            throw new InvalidOperationException($"Falha ao desserializar a resposta da API para '{typeof(T).Name}': {bodyText}");
        }

        return result;
    }

    /// <summary>
    /// Injeta um registro via API retornando o corpo bruto da resposta como texto.
    /// </summary>
    /// <param name="endpoint">Endpoint relativo do recurso.</param>
    /// <param name="payload">Objeto contendo a carga útil.</param>
    /// <param name="token">Token Bearer opcional.</param>
    /// <returns>Corpo textual retornado pela API.</returns>
    public async Task<string> SeedRecordViaApiAsync(string endpoint, object payload, string? token = null)
    {
        string url = CombineUrl(_apiBaseUrl, endpoint);
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            headers["Authorization"] = $"Bearer {token}";
        }

        var response = await _requestContext.PostAsync(url, new()
        {
            DataObject = payload,
            Headers = headers
        });

        string bodyText = await response.TextAsync();
        if (!response.Ok)
        {
            throw new HttpRequestException($"Falha no SeedRecordViaApiAsync ao chamar '{url}' ({response.Status} {response.StatusText}): {bodyText}");
        }

        return bodyText;
    }

    /// <summary>
    /// Combina a URL base e o caminho relativo garantindo formatação correta sem barras duplicadas ou ausentes.
    /// </summary>
    public static string CombineUrl(string baseUrl, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return endpoint;
        if (string.IsNullOrWhiteSpace(endpoint)) return baseUrl;
        return $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
    }

    private static bool TryExtractToken(JsonElement element, out string? token)
    {
        token = null;
        if (element.ValueKind != JsonValueKind.Object) return false;

        string[] candidateProps = ["token", "access_token", "accessToken", "jwt"];
        foreach (var prop in candidateProps)
        {
            if (element.TryGetProperty(prop, out var tokenEl) && tokenEl.ValueKind == JsonValueKind.String)
            {
                token = tokenEl.GetString();
                return true;
            }
        }

        string[] nestedContainers = ["result", "data", "body"];
        foreach (var container in nestedContainers)
        {
            if (element.TryGetProperty(container, out var containerEl) && containerEl.ValueKind == JsonValueKind.Object)
            {
                if (TryExtractToken(containerEl, out token))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
