using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using PlaywrightMasterToolkit.Config;
using PlaywrightMasterToolkit.Helpers;

namespace PlaywrightMasterToolkit.Base;

/// <summary>
/// Classe base abstrata para testes com Playwright e NUnit.
/// Fornece instâncias gerenciadas de UiHelper, AuthHelper e ApiHelper, além de captura automática de screenshots e traces em falhas.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public abstract class BaseTest : PageTest
{
    private IAPIRequestContext? _apiRequestContext;
    private ApiHelper? _apiHelper;

    /// <summary>
    /// Helper para interações com elementos de tela e resiliência de UI.
    /// </summary>
    public UiHelper Ui { get; private set; } = null!;

    /// <summary>
    /// Helper para autenticação resiliente e controle de sessão.
    /// </summary>
    public AuthHelper Auth { get; private set; } = null!;

    /// <summary>
    /// Helper lazy para operações de API e injeção de massa (Data Seeding).
    /// Lança exceção caso API_BASE_URL não esteja definida ao ser acessada.
    /// </summary>
    public ApiHelper Api
    {
        get
        {
            if (_apiHelper != null)
            {
                return _apiHelper;
            }

            string baseUrl = TestConfig.ApiBaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("A propriedade 'Api' requer a definição da variável de ambiente 'API_BASE_URL'.");
            }

            _apiRequestContext = Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
            {
                BaseURL = baseUrl,
                IgnoreHTTPSErrors = TestConfig.IgnoreHttpsErrors
            }).GetAwaiter().GetResult();

            _apiHelper = new ApiHelper(_apiRequestContext, baseUrl);
            return _apiHelper;
        }
    }

    /// <summary>
    /// Configurações do contexto de navegação para execução dos testes.
    /// </summary>
    public override BrowserNewContextOptions ContextOptions()
    {
        string baseUrl = string.IsNullOrWhiteSpace(TestConfig.BaseUrl)
            ? "https://demo.playwright.dev/todomvc"
            : TestConfig.BaseUrl;

        return new BrowserNewContextOptions
        {
            BaseURL = baseUrl,
            ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
            AcceptDownloads = true,
            IgnoreHTTPSErrors = TestConfig.IgnoreHttpsErrors
        };
    }

    /// <summary>
    /// Configuração executada antes de cada teste. Inicializa helpers, timeouts e inicia o tracing.
    /// </summary>
    [SetUp]
    public async Task BaseSetUpAsync()
    {
        Context.SetDefaultTimeout(20000);
        Context.SetDefaultNavigationTimeout(30000);
        Assertions.SetDefaultExpectTimeout(15000);

        Ui = new UiHelper(Page);
        Auth = new AuthHelper(Page);

        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    /// <summary>
    /// Limpeza executada após cada teste. Salva screenshot e trace caso o teste falhe e descarta contextos.
    /// </summary>
    [TearDown]
    public async Task BaseTearDownAsync()
    {
        try
        {
            bool failed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;
            string testName = SanitizeFileName(TestContext.CurrentContext.Test.Name);

            if (failed)
            {
                string solutionRoot = GetSolutionRoot();
                string screenshotsDir = Path.Combine(solutionRoot, "screenshots");
                Directory.CreateDirectory(screenshotsDir);
                string screenshotPath = Path.Combine(screenshotsDir, $"{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                try
                {
                    await Page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = screenshotPath,
                        FullPage = true
                    });
                    TestContext.AddTestAttachment(screenshotPath, "Screenshot de falha");
                }
                catch (Exception ex)
                {
                    TestContext.Progress.WriteLine($"[AVISO] Não foi possível capturar screenshot: {ex.Message}");
                }

                string tracesDir = Path.Combine(solutionRoot, "traces");
                Directory.CreateDirectory(tracesDir);
                string tracePath = Path.Combine(tracesDir, $"{testName}.zip");

                await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
                TestContext.AddTestAttachment(tracePath, "Trace Playwright");
            }
            else
            {
                await Context.Tracing.StopAsync();
            }
        }
        finally
        {
            if (_apiRequestContext != null)
            {
                await _apiRequestContext.DisposeAsync();
                _apiRequestContext = null;
                _apiHelper = null;
            }
        }
    }

    private static string GetSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PlaywrightMasterToolkit.sln")) ||
                Directory.Exists(Path.Combine(dir.FullName, "config")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    private static string SanitizeFileName(string name)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string clean = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(clean) ? "test" : clean;
    }
}
