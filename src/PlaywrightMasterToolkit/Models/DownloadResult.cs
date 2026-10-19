namespace PlaywrightMasterToolkit.Models;

/// <summary>
/// Representa o resultado detalhado de um download interceptado pelo Playwright.
/// </summary>
/// <param name="FileName">Nome sugerido ou final do arquivo baixado.</param>
/// <param name="Path">Caminho físico completo do arquivo salvo no disco local.</param>
public record DownloadResult(string FileName, string? Path);
