# Exemplos Práticos de Uso do Playwright .NET Master Toolkit

Este documento apresenta os cenários de teste do código de referência original portados para C# idiomático utilizando NUnit e a classe base `BaseTest`.

---

## Cenário 1: Login Resiliente e Cadastro com Dados Únicos

Demonstra o login com tratamento de sessão concorrente ativa, preenchimento de formulário com dados dinâmicos não colidentes, seleção em autocomplete e clique seguro.

```csharp
using NUnit.Framework;
using PlaywrightMasterToolkit.Base;
using PlaywrightMasterToolkit.Models;

namespace MeusTestesE2E;

[TestFixture]
public class CadastroClienteTests : BaseTest
{
    [Test]
    public async Task DeveAutenticarECadastrarClienteComDadosUnicos()
    {
        // 1. Login com tratamento de sessão concorrente ativa
        await Auth.LoginPlatformAsync("usuario.qa@empresa.com", "SenhaForte@123");

        // 2. Navegação para o formulário de cadastro
        await Page.GotoAsync("/clientes/novo");
        await Ui.WaitForPageLoadAsync();

        // 3. Preenchimento de campos com dados randômicos únicos e válidos
        string nomeCliente = await Ui.FillGeneratedNameAsync("#txt-nome", "Cliente");
        string cnpjCliente = await Ui.FillGeneratedCnpjAsync("#txt-cnpj", mask: true);
        string emailCliente = await Ui.FillGeneratedEmailAsync("#txt-email");
        string telefone = await Ui.FillGeneratedPhoneAsync("#txt-telefone", mask: true);

        // 4. Dropdown dinâmico com autocomplete e digitação sequencial
        await Ui.SelectAutocompleteAsync("#filtro-cidade", "São Paulo", type: true);

        // 5. Clique seguro no botão salvar quando visível e habilitado
        await Ui.ClickWhenVisibleAndEnabledAsync("#btn-salvar");

        // 6. Validação de mensagem de sucesso na tela
        await Assertions.Expect(Page.GetByText("Cadastrado com sucesso")).ToBeVisibleAsync();
    }
}
```

---

## Cenário 2: Teste Híbrido (API Seeding) + Download Nativo

Demonstra a injeção rápida de massa via API utilizando `ApiHelper`, navegação direta na tela do registro injetado, validação de múltiplos textos e interceptação nativa de download com persistência em disco.

```csharp
using NUnit.Framework;
using PlaywrightMasterToolkit.Base;
using PlaywrightMasterToolkit.Utils;

namespace MeusTestesE2E;

public record PedidoCriadoDto(int Id, string Codigo, double Valor);

[TestFixture]
public class PedidoHibridoTests : BaseTest
{
    [Test]
    public async Task DeveCriarPedidoViaApiEBaixarRelatorioPdf()
    {
        // 1. Injeta pedido direto no banco/serviço via API (leva milissegundos!)
        string codigoPedido = $"PED_{DataGenerator.UniqueHash(6)}";
        var pedidoPayload = new
        {
            codigo = codigoPedido,
            valor = 2500.0
        };

        string token = await Api.LoginViaApiAsync("admin", "admin123");
        var pedidoCriado = await Api.SeedRecordViaApiAsync<PedidoCriadoDto>("/pedidos", pedidoPayload, token);

        // 2. Abre a interface já na página de detalhes do pedido criado
        await Page.GotoAsync($"/pedidos/{pedidoCriado.Codigo}");
        await Ui.WaitForPageLoadAsync();

        // 3. Valida múltiplos textos de uma vez no resumo do pedido
        await Ui.ValidateTextInElementAsync(".card-resumo", [
            pedidoCriado.Codigo,
            "R$ 2.500,00"
        ]);

        // 4. Validação nativa de download de arquivo PDF
        var downloadResult = await Ui.ValidateDownloadDetailedAsync(async () =>
        {
            await Page.Locator("#btn-exportar-pdf").ClickAsync();
        }, saveTo: $"{pedidoCriado.Codigo}.pdf");

        Assert.That(downloadResult.FileName, Does.EndWith(".pdf"));
        Assert.That(File.Exists(downloadResult.Path), Is.True);
    }
}
```

---

## Cenário 3: Atualização Resiliente com Reload Until Visible

Demonstra a espera ativa por processamentos assíncronos em segundo plano através de recarregamento resiliente até que o status mude para "Aprovado".

```csharp
using NUnit.Framework;
using PlaywrightMasterToolkit.Base;
using PlaywrightMasterToolkit.Models;

namespace MeusTestesE2E;

[TestFixture]
public class ProcessamentoAssincronoTests : BaseTest
{
    [Test]
    public async Task DeveAguardarProcessamentoDeLoteAteConclusao()
    {
        await Page.GotoAsync("/lotes/processamento");
        await Ui.WaitForPageLoadAsync();

        // Recarrega a página a cada 3 segundos (até 15 tentativas) até encontrar o texto "Processado com Sucesso"
        await Ui.ReloadUntilVisibleAsync("#status-lote", "Processado com Sucesso", new ReloadOptions
        {
            MaxAttempts = 15,
            IntervalMs = 3000,
            TimeoutPerAttemptMs = 2000
        });

        await Assertions.Expect(Page.Locator("#status-lote")).ToContainTextAsync("Processado com Sucesso");
    }
}
```
