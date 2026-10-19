namespace PlaywrightMasterToolkit.Models;

/// <summary>
/// Opções para o mecanismo de recarregamento resiliente (ReloadUntilVisibleAsync).
/// </summary>
public record ReloadOptions
{
    /// <summary>
    /// Quantidade máxima de tentativas de recarregamento. Padrão é 10.
    /// </summary>
    public int MaxAttempts { get; init; } = 10;

    /// <summary>
    /// Intervalo total em milissegundos desejado entre tentativas consecutivas. Padrão é 2000ms.
    /// </summary>
    public int IntervalMs { get; init; } = 2000;

    /// <summary>
    /// Tempo limite em milissegundos para a asserção de visibilidade do texto em cada tentativa individual. Padrão é 2000ms.
    /// </summary>
    public float TimeoutPerAttemptMs { get; init; } = 2000f;
}
