namespace PlaywrightMasterToolkit.Models;

/// <summary>
/// Ações suportadas para interação com elementos de interface via UiHelper.
/// </summary>
public enum UiAction
{
    /// <summary>
    /// Executa um clique padrão no elemento.
    /// </summary>
    Click,

    /// <summary>
    /// Preenche o texto no elemento (Fill ou PressSequentially).
    /// </summary>
    Type,

    /// <summary>
    /// Limpa o valor contido no campo.
    /// </summary>
    Clear,

    /// <summary>
    /// Passa o cursor do mouse sobre o elemento (hover).
    /// </summary>
    MouseOver,

    /// <summary>
    /// Clica no elemento filtrando por texto correspondente.
    /// </summary>
    ContainsClick,

    /// <summary>
    /// Clica em um seletor filho correspondente dentro do elemento pai.
    /// </summary>
    ContainsSelectorClick
}
