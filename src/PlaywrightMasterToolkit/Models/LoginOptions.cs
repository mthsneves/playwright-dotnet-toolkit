namespace PlaywrightMasterToolkit.Models;

/// <summary>
/// Opções de configuração para o fluxo de autenticação resiliente na plataforma.
/// </summary>
public record LoginOptions
{
    /// <summary>
    /// Rota ou caminho relativo da página de login. Preserva rotas baseadas em hash (ex: "/#/login").
    /// </summary>
    public string LoginPath { get; init; } = "/#/login";

    /// <summary>
    /// Seletor CSS ou XPath do campo de input para e-mail/usuário.
    /// </summary>
    public string EmailSelector { get; init; } = "#email";

    /// <summary>
    /// Seletor CSS ou XPath do campo de input para senha.
    /// </summary>
    public string PasswordSelector { get; init; } = "#password";

    /// <summary>
    /// Seletor CSS ou XPath do botão de submissão do formulário de login.
    /// </summary>
    public string SubmitSelector { get; init; } = "#btn-login";

    /// <summary>
    /// Seletor CSS ou XPath de mensagens de erro ou alerta de login inválido.
    /// </summary>
    public string ErrorMessageSelector { get; init; } = ".alert-danger, .error-message, [role='alert']";

    /// <summary>
    /// Seletor CSS ou XPath do modal ou diálogo de sessão concorrente ativa.
    /// </summary>
    public string ConcurrentSessionModalSelector { get; init; } = "#modal-sessao-ativa, .concurrent-session-modal";

    /// <summary>
    /// Seletor CSS ou XPath do botão de confirmação/derrubada de sessão concorrente.
    /// </summary>
    public string ConcurrentSessionConfirmButtonSelector { get; init; } = "#btn-confirmar-sessao, .concurrent-session-modal button.confirm";

    /// <summary>
    /// Tempo limite total em milissegundos para a conclusão do login. Padrão é 30000ms.
    /// </summary>
    public float TimeoutMs { get; init; } = 30000f;

    /// <summary>
    /// Intervalo de polling em milissegundos entre verificações de estado da página. Padrão é 250ms.
    /// </summary>
    public int PollingIntervalMs { get; init; } = 250;
}
