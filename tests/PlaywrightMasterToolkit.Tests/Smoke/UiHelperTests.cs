namespace PlaywrightMasterToolkit.Tests.Smoke;

[TestFixture]
public class UiHelperTests : BaseTest
{
    [Test]
    public async Task TodoMvc_FullFlow_ValidatesUiHelperMethods()
    {
        // 1. Navega para a aplicação de exemplo e aguarda o carregamento
        await Page.GotoAsync(TestConfig.BaseUrl);
        await Ui.WaitForPageLoadAsync();

        // 2. Preenchimento de nome gerado dinamicamente
        string todo1 = await Ui.FillGeneratedNameAsync(".new-todo", "Tarefa");
        Assert.That(todo1, Does.StartWith("Tarefa "));
        await Page.Locator(".new-todo").PressAsync("Enter");

        // 3. Validação do texto no elemento da lista
        await Ui.ValidateTextInElementAsync(".todo-list", todo1);

        // 4. Adiciona segundo item via InteractWithElementAsync (Type)
        string todo2 = $"Segunda {DataGenerator.UniqueHash(4)}";
        await Ui.InteractWithElementAsync(".new-todo", UiAction.Type, new UiOptions { Text = todo2 });
        await Page.Locator(".new-todo").PressAsync("Enter");
        await Ui.ValidateTextInElementAsync(".todo-list", [todo1, todo2]);

        // 5. Interação com ClickUntilVisibleAsync: marca a tarefa até o botão 'Clear completed' ficar visível
        await Ui.ClickUntilVisibleAsync(".todo-list li .toggle", ".clear-completed");
        await Assertions.Expect(Page.Locator(".clear-completed")).ToBeVisibleAsync();

        // 6. Clique seguro quando visível e habilitado no botão de limpar concluídos
        await Ui.ClickWhenVisibleAndEnabledAsync(".clear-completed");
        await Assertions.Expect(Page.Locator(".clear-completed")).ToBeHiddenAsync();

        // 7. Recarregamento resiliente para validar persistência
        await Ui.ReloadUntilVisibleAsync(".todo-list", todo2);
    }

    [Test]
    public async Task InteractWithElement_SupportsClearAndContainsClick()
    {
        await Page.SetContentAsync(@"
            <input id='target-input' value='Texto Inicial' />
            <div id='items'>
                <button class='item-btn'>Primeiro</button>
                <button class='item-btn'>Segundo Botao</button>
            </div>
            <div id='result'>Aguardando</div>
            <script>
                document.querySelectorAll('.item-btn').forEach(btn => {
                    btn.addEventListener('click', () => {
                        document.getElementById('result').innerText = 'Clicado: ' + btn.innerText;
                    });
                });
            </script>
        ");

        // Clear
        await Ui.InteractWithElementAsync("#target-input", UiAction.Clear);
        await Assertions.Expect(Page.Locator("#target-input")).ToHaveValueAsync(string.Empty);

        // ContainsClick com texto específico
        await Ui.InteractWithElementAsync(".item-btn", UiAction.ContainsClick, new UiOptions { Text = "Segundo Botao" });
        await Assertions.Expect(Page.Locator("#result")).ToHaveTextAsync("Clicado: Segundo Botao");
    }

    [Test]
    public async Task SelectAutocomplete_And_SearchForm_ShouldWorkCorrectly()
    {
        await Page.SetContentAsync(@"
            <div>
                <input id='city-autocomplete' autocomplete='off' />
                <ul id='options' style='display:none;'>
                    <li role='option'>Belo Horizonte</li>
                    <li role='option'>São Paulo</li>
                    <li role='option'>Curitiba</li>
                </ul>
                <div id='selected-result'>Nenhum</div>

                <form id='search-form'>
                    <input id='client-name' name='name' />
                    <input id='client-city' name='city' />
                    <button id='btn-search' type='button'>Pesquisar</button>
                </form>
                <div id='search-status'>Pronto</div>
            </div>
            <script>
                const input = document.getElementById('city-autocomplete');
                const options = document.getElementById('options');
                input.addEventListener('focus', () => options.style.display = 'block');
                input.addEventListener('input', () => options.style.display = 'block');
                document.querySelectorAll('#options li').forEach(li => {
                    li.addEventListener('click', () => {
                        input.value = li.innerText;
                        document.getElementById('selected-result').innerText = li.innerText;
                        options.style.display = 'none';
                    });
                });

                document.getElementById('btn-search').addEventListener('click', () => {
                    document.getElementById('search-status').innerText = 'Buscado: ' + document.getElementById('client-name').value;
                });
            </script>
        ");

        // Autocomplete
        await Ui.SelectAutocompleteAsync("#city-autocomplete", "São Paulo", type: true);
        await Assertions.Expect(Page.Locator("#selected-result")).ToHaveTextAsync("São Paulo");

        // Formulário de busca com SearchField
        var fields = new List<SearchField>
        {
            new() { Selector = "#client-name", Value = "Matheus", Sequentially = true },
            new() { Selector = "#client-city", Value = "São Paulo" }
        };

        await Ui.SearchFormAsync(fields, submitButtonSelector: "#btn-search");
        await Assertions.Expect(Page.Locator("#search-status")).ToHaveTextAsync("Buscado: Matheus");
    }

    [Test]
    public async Task ValidateDownload_ShouldInterceptAndSaveDownload()
    {
        // Cria elemento de download via EvaluateAsync com Blob e atributo download
        await Page.EvaluateAsync(@"() => {
            const blob = new Blob(['Conteudo do relatorio'], { type: 'application/pdf' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.id = 'download-anchor';
            a.href = url;
            a.download = 'relatorio.pdf';
            a.innerText = 'Baixar';
            document.body.appendChild(a);
        }");

        // Validação retrocompatível (retorna nome)
        string fileName = await Ui.ValidateDownloadAsync(async () =>
        {
            await Page.Locator("#download-anchor").ClickAsync();
        });
        Assert.That(fileName, Is.EqualTo("relatorio.pdf"));

        // Validação detalhada com salvamento no diretório downloads/
        var detailed = await Ui.ValidateDownloadDetailedAsync(async () =>
        {
            await Page.Locator("#download-anchor").ClickAsync();
        }, saveTo: "relatorio_salvo.pdf");

        Assert.That(detailed.FileName, Is.EqualTo("relatorio.pdf"));
        Assert.That(detailed.Path, Is.Not.Null);
        Assert.That(File.Exists(detailed.Path), Is.True);
    }
}
