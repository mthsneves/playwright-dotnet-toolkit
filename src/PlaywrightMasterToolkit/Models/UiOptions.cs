namespace PlaywrightMasterToolkit.Models;

/// <summary>
/// Opções configuráveis para ações de interação com elementos de UI.
/// </summary>
public record UiOptions
{
    /// <summary>
    /// Índice do elemento na lista correspondente ao seletor (base 0). Padrão é 0.
    /// </summary>
    public int Eq { get; init; } = 0;

    /// <summary>
    /// Se verdadeiro, força a execução da ação ignorando checagens de acionabilidade do Playwright.
    /// O padrão foi alterado para false para garantir confiabilidade.
    /// </summary>
    public bool Force { get; init; } = false;

    /// <summary>
    /// Tempo limite máximo em milissegundos para a ação ser concluída. Padrão é 30000ms (30s).
    /// </summary>
    public float Timeout { get; init; } = 30000f;

    /// <summary>
    /// Texto a ser preenchido ou utilizado como filtro para localização do elemento.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Se verdadeiro, digita o texto caractere a caractere disparando eventos de teclado (PressSequentially).
    /// Ideal para inputs com máscaras ou autocompletes. Padrão é false.
    /// </summary>
    public bool Sequentially { get; init; } = false;

    /// <summary>
    /// Atraso opcional em milissegundos entre o pressionamento de teclas quando Sequentially for verdadeiro.
    /// </summary>
    public float? Delay { get; init; }

    /// <summary>
    /// Tempo de espera adicional opcional em milissegundos. Padrão é 0.
    /// </summary>
    public float WaitTime { get; init; } = 0;

    /// <summary>
    /// Indica se o autocomplete deve digitar o valor antes de aguardar as opções. Padrão é false.
    /// </summary>
    public bool Type { get; init; } = false;
}
