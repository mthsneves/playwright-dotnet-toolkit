namespace PlaywrightMasterToolkit.Models;

/// <summary>
/// Representa a definição de um campo a ser preenchido ou selecionado em um formulário de busca.
/// </summary>
public record SearchField
{
    /// <summary>
    /// Seletor do campo de entrada ou controle de dropdown.
    /// </summary>
    public string Selector { get; init; } = string.Empty;

    /// <summary>
    /// Valor de texto a ser preenchido ou selecionado.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Indica se a seleção deve utilizar o índice especificado em OptionIndex (retrocompatibilidade).
    /// </summary>
    public bool UseOptionIndex { get; init; } = false;

    /// <summary>
    /// Índice numérico da opção a ser selecionada no dropdown (base 0). Padrão é 0.
    /// </summary>
    public int OptionIndex { get; init; } = 0;

    /// <summary>
    /// Se verdadeiro, preenche o valor utilizando digitação sequencial de teclas (PressSequentially).
    /// </summary>
    public bool Sequentially { get; init; } = false;
}
