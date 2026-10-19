namespace PlaywrightMasterToolkit.Config;

/// <summary>
/// Gerenciador de configuração multi-ambiente para a execução de testes Playwright.
/// Realiza o carregamento de arquivos .env a partir da pasta config/ subindo a árvore de diretórios.
/// </summary>
public static class TestConfig
{
    private static readonly Lazy<bool> Initializer = new(Initialize);

    static TestConfig()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// Garante que as variáveis de ambiente do arquivo correspondente foram carregadas.
    /// </summary>
    public static void EnsureInitialized() => _ = Initializer.Value;

    private static bool Initialize()
    {
        var testEnv = Environment.GetEnvironmentVariable("TEST_ENV");
        if (string.IsNullOrWhiteSpace(testEnv))
        {
            testEnv = "qa";
        }

        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        string? foundFile = null;

        while (currentDir != null)
        {
            var candidate = Path.Combine(currentDir.FullName, "config", $".env.{testEnv}");
            if (File.Exists(candidate))
            {
                foundFile = candidate;
                break;
            }
            currentDir = currentDir.Parent;
        }

        if (foundFile != null)
        {
            DotNetEnv.Env.Load(foundFile);
        }
        else
        {
            string message = $"[AVISO] Arquivo de ambiente 'config/.env.{testEnv}' não encontrado. Prosseguindo com as variáveis já definidas no ambiente.";
            try
            {
                NUnit.Framework.TestContext.Progress.WriteLine(message);
            }
            catch
            {
                Console.Error.WriteLine(message);
            }
        }

        return true;
    }

    /// <summary>
    /// Ambiente de teste atual (ex: "qa", "dev", "prod"). Padrão é "qa".
    /// </summary>
    public static string TestEnv
    {
        get
        {
            EnsureInitialized();
            var env = Environment.GetEnvironmentVariable("TEST_ENV");
            return string.IsNullOrWhiteSpace(env) ? "qa" : env;
        }
    }

    /// <summary>
    /// URL base da aplicação para navegação nos testes. Fallback padrão é https://demo.playwright.dev/todomvc.
    /// </summary>
    public static string BaseUrl
    {
        get
        {
            EnsureInitialized();
            var url = Environment.GetEnvironmentVariable("BASE_URL");
            return string.IsNullOrWhiteSpace(url) ? "https://demo.playwright.dev/todomvc" : url;
        }
    }

    /// <summary>
    /// URL base para chamadas HTTP via API helper.
    /// </summary>
    public static string ApiBaseUrl
    {
        get
        {
            EnsureInitialized();
            return Environment.GetEnvironmentVariable("API_BASE_URL") ?? string.Empty;
        }
    }

    /// <summary>
    /// E-mail ou identificador do usuário para autenticação na aplicação.
    /// </summary>
    public static string UserEmail
    {
        get
        {
            EnsureInitialized();
            return Environment.GetEnvironmentVariable("USER_EMAIL") ?? string.Empty;
        }
    }

    /// <summary>
    /// Senha do usuário para autenticação na aplicação.
    /// </summary>
    public static string UserPassword
    {
        get
        {
            EnsureInitialized();
            return Environment.GetEnvironmentVariable("USER_PASSWORD") ?? string.Empty;
        }
    }

    /// <summary>
    /// Indica se erros de certificado HTTPS devem ser ignorados. Padrão é true.
    /// </summary>
    public static bool IgnoreHttpsErrors
    {
        get
        {
            EnsureInitialized();
            var val = Environment.GetEnvironmentVariable("IGNORE_HTTPS_ERRORS");
            if (string.IsNullOrWhiteSpace(val))
            {
                return true;
            }
            return val == "1" || string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Obtém o valor de uma variável de ambiente obrigatória ou lança exceção detalhada caso ausente.
    /// </summary>
    /// <param name="name">Nome da variável de ambiente.</param>
    /// <returns>Valor da variável de ambiente configurada.</returns>
    /// <exception cref="InvalidOperationException">Lançada quando a variável não está definida ou está em branco.</exception>
    public static string GetRequired(string name)
    {
        EnsureInitialized();
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"A variável de ambiente obrigatória '{name}' não foi configurada.");
        }
        return value;
    }
}
