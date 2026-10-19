using System.Diagnostics;
using Microsoft.Playwright;
using PlaywrightMasterToolkit.Config;
using PlaywrightMasterToolkit.Models;

namespace PlaywrightMasterToolkit.Helpers;

/// <summary>
/// Helper para gerenciamento de autenticação e sessão resiliente em testes automatizados.
/// Trata sessões ativas concorrentes, erros de credenciais e limpeza de armazenamento e cookies.
/// </summary>
public class AuthHelper
{
    private readonly IPage _page;

    /// <summary>
    /// Inicializa uma nova instância de AuthHelper com a página ativa.
    /// </summary>
    /// <param name="page">Instância de IPage do Playwright.</param>
    public AuthHelper(IPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
    }

    /// <summary>
    /// Executa o fluxo completo de login resiliente na plataforma.
    /// Limpa cookies e storage, preenche as credenciais, trata modais de sessão concorrente e valida o redirecionamento.
    /// </summary>
    /// <param name="email">E-mail do usuário. Se nulo ou vazio, utiliza TestConfig.UserEmail.</param>
    /// <param name="password">Senha do usuário. Se nula ou vazia, utiliza TestConfig.UserPassword.</param>
    /// <param name="options">Opções personalizadas de seletores, rotas e timeouts de login.</param>
    /// <exception cref="InvalidOperationException">Lançada caso as credenciais não estejam definidas ou a página exiba erro de login.</exception>
    /// <exception cref="TimeoutException">Lançada caso o login não seja concluído dentro do tempo limite especificado.</exception>
    public async Task LoginPlatformAsync(string? email = null, string? password = null, LoginOptions? options = null)
    {
        options ??= new LoginOptions();

        string resolvedEmail = !string.IsNullOrWhiteSpace(email) ? email : TestConfig.UserEmail;
        string resolvedPassword = !string.IsNullOrWhiteSpace(password) ? password : TestConfig.UserPassword;

        if (string.IsNullOrWhiteSpace(resolvedEmail))
        {
            throw new InvalidOperationException("E-mail para autenticação não informado e USER_EMAIL não está configurado nas variáveis de ambiente.");
        }

        if (string.IsNullOrWhiteSpace(resolvedPassword))
        {
            throw new InvalidOperationException("Senha para autenticação não informada e USER_PASSWORD não está configurado nas variáveis de ambiente.");
        }

        // Limpa cookies previamente existentes
        await _page.Context.ClearCookiesAsync();

        // Navega para a página de login preservando eventuais hash routings (ex: /#/login)
        await _page.GotoAsync(options.LoginPath);

        // Limpa dados de sessões passadas do armazenamento local e de sessão
        try
        {
            await _page.EvaluateAsync("() => { try { localStorage.clear(); sessionStorage.clear(); } catch {} }");
        }
        catch
        {
            // Tolera falhas caso o contexto de segurança impeça a execução imediata
        }

        // Preenchimento e validação do e-mail
        var emailLoc = _page.Locator(options.EmailSelector);
        await emailLoc.FillAsync(resolvedEmail, new() { Timeout = options.TimeoutMs });
        await Assertions.Expect(emailLoc).ToHaveValueAsync(resolvedEmail, new() { Timeout = options.TimeoutMs });

        // Preenchimento da senha
        var passwordLoc = _page.Locator(options.PasswordSelector);
        await passwordLoc.FillAsync(resolvedPassword, new() { Timeout = options.TimeoutMs });

        // Submissão com validação de habilitado prévio
        var submitLoc = _page.Locator(options.SubmitSelector);
        await Assertions.Expect(submitLoc).ToBeEnabledAsync(new() { Timeout = options.TimeoutMs });
        await submitLoc.ClickAsync(new() { Timeout = options.TimeoutMs });

        // Define o caminho limpo (sem #) para verificar o desvio da rota de login
        string cleanPath = options.LoginPath.Replace("#", "").TrimEnd('/');
        if (string.IsNullOrEmpty(cleanPath))
        {
            cleanPath = "/login";
        }

        var sw = Stopwatch.StartNew();
        var errorLoc = _page.Locator(options.ErrorMessageSelector);
        var modalLoc = _page.Locator(options.ConcurrentSessionModalSelector);
        var confirmBtnLoc = _page.Locator(options.ConcurrentSessionConfirmButtonSelector);

        while (sw.ElapsedMilliseconds < options.TimeoutMs)
        {
            // 1. Sucesso: A URL atual não contém mais a rota de login
            if (!_page.Url.Contains(cleanPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // 2. Erro de credenciais/bloqueio: Lança exceção com a mensagem visível
            if (await errorLoc.IsVisibleAsync())
            {
                string errorText = await errorLoc.InnerTextAsync();
                throw new InvalidOperationException($"Falha no login: {errorText}");
            }

            // 3. Modal de sessão concorrente ativa: Clica para derrubar e aguarda navegação
            if (await modalLoc.IsVisibleAsync())
            {
                await confirmBtnLoc.ClickAsync(new() { Timeout = options.TimeoutMs });
                float remainingMs = Math.Max(1000f, options.TimeoutMs - sw.ElapsedMilliseconds);
                await _page.WaitForURLAsync(url => !url.Contains(cleanPath, StringComparison.OrdinalIgnoreCase), new() { Timeout = remainingMs });
                return;
            }

            await _page.WaitForTimeoutAsync(options.PollingIntervalMs);
        }

        throw new TimeoutException($"Login não concluiu em {options.TimeoutMs}ms. URL atual: {_page.Url}");
    }

    /// <summary>
    /// Executa o logout na plataforma clicando no menu do usuário, aguardando o botão de logout e limpando os estados de sessão.
    /// </summary>
    /// <param name="userMenuSelector">Seletor do menu do usuário.</param>
    /// <param name="logoutButtonSelector">Seletor da opção de logout.</param>
    public async Task LogoutAsync(string userMenuSelector = "#user-menu", string logoutButtonSelector = "#btn-logout")
    {
        var menu = _page.Locator(userMenuSelector);
        await menu.ClickAsync(new() { Timeout = 10000 });

        try
        {
            await _page.Locator(logoutButtonSelector).WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });
        }
        catch
        {
            // Tolera falha caso o botão já esteja pronto sem transição
        }

        var logoutBtn = _page.Locator(logoutButtonSelector);
        await logoutBtn.ClickAsync(new() { Timeout = 10000 });

        await _page.Context.ClearCookiesAsync();
        try
        {
            await _page.EvaluateAsync("() => { try { localStorage.clear(); sessionStorage.clear(); } catch {} }");
        }
        catch
        {
            // Tolera falha em contextos restritos
        }
    }
}
