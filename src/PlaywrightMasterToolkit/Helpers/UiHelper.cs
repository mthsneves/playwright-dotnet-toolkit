using System.Diagnostics;
using Microsoft.Playwright;
using PlaywrightMasterToolkit.Models;
using PlaywrightMasterToolkit.Utils;

namespace PlaywrightMasterToolkit.Helpers;

/// <summary>
/// Helper de resiliência e ergonomia para interações com elementos de UI via Playwright.
/// </summary>
public class UiHelper
{
    private readonly IPage _page;

    /// <summary>
    /// Construtor que recebe a instância ativa do Playwright IPage.
    /// </summary>
    /// <param name="page">Instância de IPage.</param>
    public UiHelper(IPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
    }

    /// <summary>
    /// Aguarda o ciclo de vida de carregamento da página (DOMContentLoaded e Load).
    /// </summary>
    /// <param name="waitForNetworkIdle">
    /// Se verdadeiro, também aguarda o estado NetworkIdle.
    /// NOTA: O uso de NetworkIdle é desaconselhado pela documentação oficial do Playwright para aplicações com websockets ou polling contínuo.
    /// </param>
    public async Task WaitForPageLoadAsync(bool waitForNetworkIdle = false)
    {
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _page.WaitForLoadStateAsync(LoadState.Load);

        if (waitForNetworkIdle)
        {
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    /// <summary>
    /// Executa uma ação de interação em um elemento especificado por seletor com suporte a opções de resiliência.
    /// Remove esperas redundantes e define Force = false por padrão para respeitar verificações de ação do Playwright.
    /// </summary>
    /// <param name="selector">Seletor CSS ou XPath do elemento.</param>
    /// <param name="action">Ação a ser executada (Click, Type, Clear, MouseOver, ContainsClick, ContainsSelectorClick).</param>
    /// <param name="options">Opções de configuração como Timeout, Force, Eq, Text e Sequentially.</param>
    public async Task InteractWithElementAsync(string selector, UiAction action, UiOptions? options = null)
    {
        options ??= new UiOptions();
        var locator = ResolveLocator(selector, action, options);
        await ExecuteActionAsync(locator, action, options);
    }

    /// <summary>
    /// Executa uma ação de interação em uma instância de ILocator.
    /// </summary>
    /// <param name="locator">Instância de ILocator.</param>
    /// <param name="action">Ação a ser executada.</param>
    /// <param name="options">Opções de configuração.</param>
    public async Task InteractWithElementAsync(ILocator locator, UiAction action, UiOptions? options = null)
    {
        options ??= new UiOptions();
        if (options.Eq != 0)
        {
            locator = locator.Nth(options.Eq);
        }
        await ExecuteActionAsync(locator, action, options);
    }

    /// <summary>
    /// Aguarda que um elemento esteja visível e habilitado antes de realizar o clique.
    /// </summary>
    /// <param name="selector">Seletor do elemento.</param>
    /// <param name="options">Opções adicionais de clique e timeout.</param>
    public async Task ClickWhenVisibleAndEnabledAsync(string selector, UiOptions? options = null)
    {
        options ??= new UiOptions();
        var locator = _page.Locator(selector);
        if (options.Eq != 0)
        {
            locator = locator.Nth(options.Eq);
        }

        await ClickWhenVisibleAndEnabledAsync(locator, options);
    }

    /// <summary>
    /// Aguarda que um ILocator esteja visível e habilitado antes de realizar o clique.
    /// </summary>
    /// <param name="locator">Instância do ILocator a ser acionado.</param>
    /// <param name="options">Opções adicionais de clique e timeout.</param>
    public async Task ClickWhenVisibleAndEnabledAsync(ILocator locator, UiOptions? options = null)
    {
        options ??= new UiOptions();
        await Assertions.Expect(locator).ToBeVisibleAsync(new() { Timeout = options.Timeout });
        await Assertions.Expect(locator).ToBeEnabledAsync(new() { Timeout = options.Timeout });
        await locator.ClickAsync(new() { Force = options.Force, Timeout = options.Timeout });
    }

    /// <summary>
    /// Seleciona uma opção em um componente de autocomplete/dropdown.
    /// Dispara digitação sequencial de teclas caso options.Type seja verdadeiro e aguarda a visibilidade da opção.
    /// </summary>
    /// <param name="selector">Seletor do campo de entrada ou controle do autocomplete.</param>
    /// <param name="value">Texto da opção a ser pesquisada e selecionada.</param>
    /// <param name="options">Opções adicionais (Type, Delay, WaitTime, Timeout).</param>
    public async Task SelectAutocompleteAsync(string selector, string value, UiOptions? options = null)
    {
        options ??= new UiOptions();
        var inputLocator = _page.Locator(selector);
        if (options.Eq != 0)
        {
            inputLocator = inputLocator.Nth(options.Eq);
        }

        await inputLocator.ClickAsync(new() { Force = options.Force, Timeout = options.Timeout });

        if (options.Type)
        {
            await inputLocator.PressSequentiallyAsync(value, new()
            {
                Delay = options.Delay ?? 50f,
                Timeout = options.Timeout
            });
        }

        if (options.WaitTime > 0)
        {
            await _page.WaitForTimeoutAsync(options.WaitTime);
        }

        var optionLoc = _page.GetByRole(AriaRole.Option, new() { Name = value, Exact = false });
        var fallbackLoc = _page.Locator("[role='option'], li.MuiAutocomplete-option, .ant-select-item-option-content, li")
            .Filter(new() { HasText = value });

        var targetOption = await optionLoc.CountAsync() > 0 ? optionLoc.First : fallbackLoc.First;

        await Assertions.Expect(targetOption).ToBeVisibleAsync(new() { Timeout = options.Timeout });
        await targetOption.ClickAsync(new() { Force = options.Force, Timeout = options.Timeout });
    }

    /// <summary>
    /// Sobrecarga ergonômica para seleção de autocomplete indicando diretamente se deve digitar o valor.
    /// </summary>
    /// <param name="selector">Seletor do campo de entrada.</param>
    /// <param name="value">Valor da opção.</param>
    /// <param name="type">Se verdadeiro, digita sequencialmente o valor no campo antes de selecionar a opção.</param>
    /// <param name="options">Opções adicionais.</param>
    public Task SelectAutocompleteAsync(string selector, string value, bool type, UiOptions? options = null)
    {
        options = (options ?? new UiOptions()) with { Type = type };
        return SelectAutocompleteAsync(selector, value, options);
    }

    /// <summary>
    /// Abre um dropdown e seleciona uma opção correspondente ao texto informado, pressionando Escape ao final para fechar popups suspensos.
    /// </summary>
    /// <param name="selector">Seletor do dropdown.</param>
    /// <param name="text">Texto visível da opção.</param>
    /// <param name="timeout">Tempo limite máximo em milissegundos.</param>
    public async Task SelectOptionWithTextAsync(string selector, string text, float timeout = 30000)
    {
        var trigger = _page.Locator(selector);
        await trigger.ClickAsync(new() { Timeout = timeout });

        var optionLoc = _page.GetByRole(AriaRole.Option, new() { Name = text, Exact = false });
        var fallbackLoc = _page.Locator("[role='option'], li").Filter(new() { HasText = text });

        var target = await optionLoc.CountAsync() > 0 ? optionLoc.First : fallbackLoc.First;

        await Assertions.Expect(target).ToBeVisibleAsync(new() { Timeout = timeout });
        await target.ClickAsync(new() { Timeout = timeout });

        await _page.Keyboard.PressAsync("Escape");
    }

    /// <summary>
    /// Clica repetidamente em um elemento gatilho até que o elemento alvo torne-se visível.
    /// Tolera períodos de re-renderização do gatilho e lança exceção descritiva informando qual seletor não foi encontrado.
    /// </summary>
    /// <param name="clickSelector">Seletor do elemento a ser clicado repetidamente.</param>
    /// <param name="targetSelector">Seletor do elemento alvo esperado.</param>
    /// <param name="timeout">Tempo limite total em milissegundos.</param>
    /// <param name="intervalMs">Intervalo entre tentativas em milissegundos.</param>
    public async Task ClickUntilVisibleAsync(string clickSelector, string targetSelector, float timeout = 30000, int intervalMs = 500)
    {
        var sw = Stopwatch.StartNew();
        var clickLoc = _page.Locator(clickSelector).First;
        var targetLoc = _page.Locator(targetSelector).First;
        bool targetFound = false;

        while (sw.ElapsedMilliseconds < timeout)
        {
            if (await targetLoc.IsVisibleAsync())
            {
                targetFound = true;
                break;
            }

            if (await clickLoc.IsVisibleAsync())
            {
                try
                {
                    await clickLoc.ClickAsync(new() { Timeout = intervalMs });
                }
                catch
                {
                    // Ignora falhas momentâneas durante re-renderização
                }
            }

            await _page.WaitForTimeoutAsync(intervalMs);
        }

        if (!targetFound && await targetLoc.IsVisibleAsync())
        {
            targetFound = true;
        }

        if (!targetFound)
        {
            bool clickVisible = await clickLoc.IsVisibleAsync();
            string missing = !clickVisible
                ? $"o elemento de clique '{clickSelector}' e o elemento alvo '{targetSelector}'"
                : $"o elemento alvo '{targetSelector}'";

            throw new TimeoutException($"ClickUntilVisibleAsync expirou após {timeout}ms. Não foi possível visualizar {missing}.");
        }
    }

    /// <summary>
    /// Valida que um elemento contém todos os textos informados utilizando asserções nativas do Playwright.
    /// </summary>
    /// <param name="selector">Seletor do elemento contêiner.</param>
    /// <param name="texts">Coleção de textos esperados.</param>
    /// <param name="timeout">Tempo limite em milissegundos para cada validação.</param>
    public async Task ValidateTextInElementAsync(string selector, IEnumerable<string> texts, float timeout = 30000)
    {
        var locator = _page.Locator(selector);
        await ValidateTextInElementAsync(locator, texts, timeout);
    }

    /// <summary>
    /// Valida que um ILocator contém todos os textos informados utilizando asserções nativas do Playwright.
    /// </summary>
    /// <param name="locator">Instância do ILocator.</param>
    /// <param name="texts">Coleção de textos esperados.</param>
    /// <param name="timeout">Tempo limite em milissegundos para cada validação.</param>
    public async Task ValidateTextInElementAsync(ILocator locator, IEnumerable<string> texts, float timeout = 30000)
    {
        foreach (var text in texts)
        {
            await Assertions.Expect(locator).ToContainTextAsync(text, new() { Timeout = timeout });
        }
    }

    /// <summary>
    /// Valida que um elemento contém o texto informado.
    /// </summary>
    public Task ValidateTextInElementAsync(string selector, string text, float timeout = 30000) =>
        ValidateTextInElementAsync(selector, [text], timeout);

    /// <summary>
    /// Valida que um ILocator contém o texto informado.
    /// </summary>
    public Task ValidateTextInElementAsync(ILocator locator, string text, float timeout = 30000) =>
        ValidateTextInElementAsync(locator, [text], timeout);

    /// <summary>
    /// Preenche sequencialmente os campos de um formulário de busca e opcionalmente aciona o botão de submissão.
    /// </summary>
    /// <param name="fields">Lista de definições dos campos do formulário.</param>
    /// <param name="submitButtonSelector">Seletor opcional do botão de submissão ou pesquisa.</param>
    /// <param name="timeout">Tempo limite em milissegundos para cada ação.</param>
    public async Task SearchFormAsync(IEnumerable<SearchField> fields, string? submitButtonSelector = null, float timeout = 30000)
    {
        foreach (var field in fields)
        {
            var locator = _page.Locator(field.Selector);

            if (field.UseOptionIndex || field.OptionIndex > 0)
            {
                await locator.ClickAsync(new() { Timeout = timeout });
                var options = _page.Locator("[role='option'], li, option");
                await options.Nth(field.OptionIndex).ClickAsync(new() { Timeout = timeout });
            }
            else
            {
                if (field.Sequentially)
                {
                    await locator.PressSequentiallyAsync(field.Value, new() { Timeout = timeout });
                }
                else
                {
                    await locator.FillAsync(field.Value, new() { Timeout = timeout });
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(submitButtonSelector))
        {
            await ClickWhenVisibleAndEnabledAsync(submitButtonSelector, new() { Timeout = timeout });
        }
    }

    /// <summary>
    /// Recarrega a página repetidamente até que o texto esperado seja localizado em algum dos elementos correspondentes ao seletor.
    /// </summary>
    /// <param name="selector">Seletor dos elementos a serem inspecionados.</param>
    /// <param name="expectedText">Texto esperado.</param>
    /// <param name="options">Opções de tentativas e intervalos.</param>
    public async Task ReloadUntilVisibleAsync(string selector, string expectedText, ReloadOptions? options = null)
    {
        options ??= new ReloadOptions();
        string normalizedExpected = DataGenerator.NormalizeText(expectedText);

        for (int attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            await _page.ReloadAsync();
            await WaitForPageLoadAsync();

            var sw = Stopwatch.StartNew();
            try
            {
                var locator = _page.Locator(selector);
                await Assertions.Expect(locator).ToContainTextAsync(expectedText, new() { Timeout = options.TimeoutPerAttemptMs });

                var contents = await locator.AllTextContentsAsync();
                bool found = contents.Any(c => DataGenerator.NormalizeText(c).Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase));
                if (found)
                {
                    return;
                }
            }
            catch
            {
                // Continua para a próxima tentativa até atingir MaxAttempts
            }

            int elapsed = (int)sw.ElapsedMilliseconds;
            int remainingWait = options.IntervalMs - elapsed;
            if (remainingWait > 0 && attempt < options.MaxAttempts)
            {
                await _page.WaitForTimeoutAsync(remainingWait);
            }
        }

        throw new TimeoutException($"ReloadUntilVisibleAsync falhou após {options.MaxAttempts} tentativas. O seletor '{selector}' não exibiu o texto '{expectedText}'.");
    }

    /// <summary>
    /// Intercepta e valida um download disparado por uma ação assíncrona, retornando o nome sugerido do arquivo (retrocompatível).
    /// </summary>
    /// <param name="trigger">Ação que dispara o download (ex: clique em um link ou botão).</param>
    /// <returns>Nome sugerido do arquivo baixado.</returns>
    public async Task<string> ValidateDownloadAsync(Func<Task> trigger)
    {
        var result = await ValidateDownloadDetailedAsync(trigger);
        return result.FileName;
    }

    /// <summary>
    /// Intercepta um download disparado por uma ação assíncrona, validando a persistência no disco e opcionalmente salvando no caminho especificado.
    /// </summary>
    /// <param name="trigger">Ação que dispara o download.</param>
    /// <param name="saveTo">Caminho relativo ou absoluto onde o arquivo deve ser salvo.</param>
    /// <returns>DownloadResult contendo o nome sugerido e o caminho físico do arquivo.</returns>
    public async Task<DownloadResult> ValidateDownloadDetailedAsync(Func<Task> trigger, string? saveTo = null)
    {
        var download = await _page.RunAndWaitForDownloadAsync(trigger);
        string suggestedName = download.SuggestedFilename;
        string? tempPath = await download.PathAsync();

        if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath))
        {
            throw new InvalidOperationException("O download foi acionado pelo navegador, mas o arquivo temporário não foi localizado em disco.");
        }

        string finalPath = tempPath;

        if (!string.IsNullOrWhiteSpace(saveTo))
        {
            string destination = Path.IsPathRooted(saveTo)
                ? saveTo
                : Path.Combine(GetDownloadsDirectory(), saveTo);

            string? dir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await download.SaveAsAsync(destination);
            finalPath = destination;
        }

        return new DownloadResult(suggestedName, finalPath);
    }

    private static string GetDownloadsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PlaywrightMasterToolkit.sln")) ||
                Directory.Exists(Path.Combine(dir.FullName, "downloads")))
            {
                return Path.Combine(dir.FullName, "downloads");
            }
            dir = dir.Parent;
        }
        return Path.Combine(Directory.GetCurrentDirectory(), "downloads");
    }

    /// <summary>
    /// Gera um nome dinâmico único com prefixo e preenche o elemento de entrada, retornando o valor preenchido.
    /// </summary>
    public async Task<string> FillGeneratedNameAsync(string selector, string prefix = "Cliente", UiOptions? options = null)
    {
        string name = DataGenerator.Name(prefix);
        await InteractWithElementAsync(selector, UiAction.Type, (options ?? new UiOptions()) with { Text = name });
        return name;
    }

    /// <summary>
    /// Gera um CPF válido com ou sem máscara e preenche o campo, retornando o valor preenchido.
    /// </summary>
    public async Task<string> FillGeneratedCpfAsync(string selector, bool mask = false, UiOptions? options = null)
    {
        string cpf = DataGenerator.Cpf(mask);
        await InteractWithElementAsync(selector, UiAction.Type, (options ?? new UiOptions()) with { Text = cpf });
        return cpf;
    }

    /// <summary>
    /// Gera um CNPJ válido com ou sem máscara e preenche o campo, retornando o valor preenchido.
    /// </summary>
    public async Task<string> FillGeneratedCnpjAsync(string selector, bool mask = false, UiOptions? options = null)
    {
        string cnpj = DataGenerator.Cnpj(mask);
        await InteractWithElementAsync(selector, UiAction.Type, (options ?? new UiOptions()) with { Text = cnpj });
        return cnpj;
    }

    /// <summary>
    /// Gera um e-mail único e preenche o campo, retornando o valor preenchido.
    /// </summary>
    public async Task<string> FillGeneratedEmailAsync(string selector, string domain = "empresa.com", UiOptions? options = null)
    {
        string email = DataGenerator.Email(null, domain);
        await InteractWithElementAsync(selector, UiAction.Type, (options ?? new UiOptions()) with { Text = email });
        return email;
    }

    /// <summary>
    /// Gera um telefone celular brasileiro com ou sem máscara e preenche o campo, retornando o valor preenchido.
    /// </summary>
    public async Task<string> FillGeneratedPhoneAsync(string selector, bool mask = false, UiOptions? options = null)
    {
        string phone = DataGenerator.Cellphone(mask);
        await InteractWithElementAsync(selector, UiAction.Type, (options ?? new UiOptions()) with { Text = phone });
        return phone;
    }

    private ILocator ResolveLocator(string selector, UiAction action, UiOptions options)
    {
        var locator = _page.Locator(selector);

        if (action == UiAction.ContainsClick && !string.IsNullOrEmpty(options.Text))
        {
            locator = locator.Filter(new() { HasText = options.Text });
        }
        else if (action == UiAction.ContainsSelectorClick && !string.IsNullOrEmpty(options.Text))
        {
            locator = locator.Locator(options.Text);
        }

        if (options.Eq != 0)
        {
            locator = locator.Nth(options.Eq);
        }

        return locator;
    }

    private static async Task ExecuteActionAsync(ILocator locator, UiAction action, UiOptions options)
    {
        switch (action)
        {
            case UiAction.Click:
            case UiAction.ContainsClick:
            case UiAction.ContainsSelectorClick:
                await locator.ClickAsync(new() { Force = options.Force, Timeout = options.Timeout });
                break;
            case UiAction.Type:
                if (options.Sequentially)
                {
                    await locator.PressSequentiallyAsync(options.Text ?? string.Empty, new()
                    {
                        Delay = options.Delay,
                        Timeout = options.Timeout
                    });
                }
                else
                {
                    await locator.FillAsync(options.Text ?? string.Empty, new()
                    {
                        Force = options.Force,
                        Timeout = options.Timeout
                    });
                }
                break;
            case UiAction.Clear:
                await locator.ClearAsync(new() { Force = options.Force, Timeout = options.Timeout });
                break;
            case UiAction.MouseOver:
                await locator.HoverAsync(new() { Force = options.Force, Timeout = options.Timeout });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }
}
