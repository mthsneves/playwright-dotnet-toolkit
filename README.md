# Playwright .NET Master Toolkit

[![CI](https://github.com/matheusneves/playwright-dotnet-master-toolkit/actions/workflows/ci.yml/badge.svg)](https://github.com/matheusneves/playwright-dotnet-master-toolkit/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Playwright](https://img.shields.io/badge/Playwright-1.63.0-green.svg)
![NUnit](https://img.shields.io/badge/NUnit-3.14-red.svg)

**Playwright .NET Master Toolkit** é um framework e biblioteca de testes reutilizável desenvolvida em **C# (.NET 8)** com **Playwright** e **NUnit**. Ele fornece abstrações ergonômicas e resilientes para automação de testes de ponta a ponta (E2E), testes híbridos com injeção direta de dados via API (API Seeding), gerador dinâmico de massas de dados brasileiras (CPF, CNPJ, Telefone, E-mail com validação matemática) e controle robusto de sessão e falhas com captura automática de traces e screenshots.

---

## 📋 Sumário
- [Visão Geral e Arquitetura](#-visão-geral-e-arquitetura)
- [Pré-requisitos](#-pré-requisitos)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Configuração de Ambientes](#-configuração-de-ambientes)
- [Instalação e Execução](#-instalação-e-execução)
- [Tabela Completa de Métodos](#-tabela-completa-de-métodos)
- [Diferenças em Relação ao TypeScript](#-diferenças-em-relação-ao-typescript)
- [Boas Práticas de Automação](#-boas-práticas-de-automação)
- [Diagnóstico e Visualização de Traces](#-diagnóstico-e-visualização-de-traces)
- [Como Utilizar em Outros Projetos](#-como-utilizar-em-outros-projetos)

---

## 🏛️ Visão Geral e Arquitetura

O toolkit foi arquitetado para resolver dores clássicas de automação E2E:
1. **Flakiness e Actionability**: Remoção de esperas fixas manuais e respeito às checagens nativas de acionabilidade do Playwright.
2. **Multi-ambiente Desacoplado**: Carregamento transparente de arquivos `.env.<ambiente>` via `DotNetEnv` com busca recursiva da pasta `config/`.
3. **Massa de Testes sem Colisão**: Gerador com validação de dígitos reais de documentos brasileiros e hashes únicos.
4. **Testes Híbridos Ultrarrápidos**: Criação e preparação de estado prévio via API direta, navegando na UI apenas para o que importa.
5. **Observabilidade Total**: Em caso de falha de teste, o toolkit salva automaticamente a captura de tela (.png) e o arquivo de tracing completo (.zip) anexados ao relatório do NUnit.

---

## ⚙️ Pré-requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) ou superior (`dotnet --version >= 8.0`).
- [PowerShell 7 (`pwsh`)](https://github.com/PowerShell/PowerShell) (necessário para rodar o script de instalação dos navegadores do Playwright).
- Git (`git --version`).

---

## 📁 Estrutura do Projeto

```text
PlaywrightMasterToolkit/
├── .github/
│   └── workflows/
│       └── ci.yml                 # Pipeline CI no GitHub Actions
├── config/
│   ├── .env.example               # Exemplo versionado de configuração de variáveis
│   └── .env.qa                    # Arquivo local de QA (ignorado pelo git)
├── docs/
│   └── EXEMPLOS.md                # Exemplos práticos detalhados em C#
├── downloads/
│   └── .gitkeep                   # Diretório de persistência de downloads
├── src/PlaywrightMasterToolkit/
│   ├── Base/
│   │   └── BaseTest.cs            # Classe base abstrata derivada de PageTest
│   ├── Config/
│   │   └── TestConfig.cs          # Leitor de variáveis multi-ambiente (.env)
│   ├── Helpers/
│   │   ├── ApiHelper.cs           # Cliente HTTP e Data Seeding
│   │   ├── AuthHelper.cs          # Login resiliente e controle de sessão
│   │   └── UiHelper.cs            # Resiliência e ergonomia de UI
│   ├── Models/
│   │   ├── DownloadResult.cs      # Modelo de retorno de downloads interceptados
│   │   ├── LoginOptions.cs        # Opções de seletores e timeouts de login
│   │   ├── ReloadOptions.cs       # Opções para recarregamento resiliente
│   │   ├── SearchField.cs         # Modelo para formulários de busca
│   │   ├── UiAction.cs            # Enum de ações suportadas
│   │   └── UiOptions.cs           # Opções de interação (Timeout, Force, Sequentially, etc.)
│   └── Utils/
│       └── DataGenerator.cs       # Gerador e validador de massas de dados
├── tests/PlaywrightMasterToolkit.Tests/
│   ├── .runsettings               # Configurações do runner (Playwright Headless/Headed, TEST_ENV)
│   ├── Smoke/
│   │   ├── DataGeneratorTests.cs  # Testes de unidade do gerador de dados
│   │   ├── FixturesTests.cs       # Testes de injeção da base e mock de API
│   │   └── UiHelperTests.cs       # Testes E2E contra TodoMVC e componentes de interface
│   └── Usings.cs                  # Global usings e configuração de paralelismo NUnit
├── CHANGELOG.md                   # Histórico de alterações e mudanças de comportamento
├── LICENSE                        # Licença MIT
└── README.md                      # Documentação completa
```

---

## 🔧 Configuração de Ambientes

O toolkit suporta múltiplos ambientes controlados pela variável de ambiente `TEST_ENV` (padrão: `qa`).

1. Copie o arquivo modelo:
   ```bash
   cp config/.env.example config/.env.qa
   ```
2. Configure os parâmetros desejados em `config/.env.qa`:
   ```properties
   BASE_URL=https://demo.playwright.dev/todomvc
   API_BASE_URL=https://api.exemplo.com
   USER_EMAIL=usuario.qa@empresa.com
   USER_PASSWORD=SenhaForte@123
   ```
> [!NOTE]
> O arquivo `config/.env.example` é versionado. Todos os arquivos `config/.env.*` reais contendo credenciais são estritamente ignorados pelo `.gitignore`.

---

## 🚀 Instalação e Execução

### 1. Restaurar dependências e compilar a solução
```bash
dotnet build
```

### 2. Instalar os navegadores do Playwright
Execute o script gerado na compilação:
```powershell
pwsh tests/PlaywrightMasterToolkit.Tests/bin/Debug/net8.0/playwright.ps1 install chromium
```
*(No Linux, para instalar dependências do sistema operacional: `install --with-deps chromium`)*

### 3. Execução dos Testes
- **Execução padrão (modo headless com ambiente padrão qa)**:
  ```bash
  dotnet test
  ```
- **Execução especificando outro ambiente**:
  - No Linux/macOS:
    ```bash
    TEST_ENV=staging dotnet test
    ```
  - No PowerShell:
    ```powershell
    $env:TEST_ENV="staging"; dotnet test
    ```
- **Execução em modo visual (Headed)**:
  ```powershell
  $env:HEADED="1"; dotnet test
  ```
- **Executando apenas a suíte de Smoke**:
  ```bash
  dotnet test --filter "FullyQualifiedName~Smoke"
  ```

---

## 📖 Tabela Completa de Métodos

### `DataGenerator` (Classe Estática)
| Assinatura | Descrição | Exemplo de Uso |
|---|---|---|
| `string UniqueHash(int length = 8)` | Gera cadeia alfanumérica pseudoaleatória única. | `DataGenerator.UniqueHash(6)` |
| `string Name(string prefix = "Cliente")` | Gera um nome com prefixo e hash único. | `DataGenerator.Name("Fornecedor")` |
| `string Email(string? prefix = null, string domain = "empresa.com")` | Gera e-mail único normalizado. | `DataGenerator.Email("qa", "teste.com")` |
| `string Cellphone(bool mask = false)` | Gera número de celular brasileiro válido (com ou sem máscara). | `DataGenerator.Cellphone(mask: true)` |
| `string Cpf(bool mask = false)` | Gera CPF matematicamente válido com dígitos verificadores. | `DataGenerator.Cpf(mask: true)` |
| `bool IsValidCpf(string? cpf)` | Valida autenticidade matemática de um CPF (com ou sem máscara). | `DataGenerator.IsValidCpf("123.456.789-09")` |
| `string Cnpj(bool mask = false)` | Gera CNPJ matematicamente válido (padrão matriz 0001). | `DataGenerator.Cnpj(mask: true)` |
| `bool IsValidCnpj(string? cnpj)` | Valida autenticidade matemática de um CNPJ (com ou sem máscara). | `DataGenerator.IsValidCnpj(cnpj)` |
| `string NormalizeText(string? input)` | Remove acentuação, converte para minúsculas e colapsa espaços. | `DataGenerator.NormalizeText(" Ação ")` |

### `UiHelper`
| Assinatura | Descrição | Exemplo de Uso |
|---|---|---|
| `Task WaitForPageLoadAsync(bool waitForNetworkIdle = false)` | Aguarda carregamento completo da página (DOMContentLoaded e Load). | `await Ui.WaitForPageLoadAsync()` |
| `Task InteractWithElementAsync(string selector, UiAction action, UiOptions? options = null)` | Executa ação em elemento por seletor (Click, Type, Clear, MouseOver, ContainsClick, etc). | `await Ui.InteractWithElementAsync("#btn", UiAction.Click)` |
| `Task InteractWithElementAsync(ILocator locator, UiAction action, UiOptions? options = null)` | Executa ação em instância de `ILocator`. | `await Ui.InteractWithElementAsync(loc, UiAction.Clear)` |
| `Task ClickWhenVisibleAndEnabledAsync(string selector, UiOptions? options = null)` | Aguarda que o elemento esteja visível e habilitado antes do clique. | `await Ui.ClickWhenVisibleAndEnabledAsync("#salvar")` |
| `Task ClickWhenVisibleAndEnabledAsync(ILocator locator, UiOptions? options = null)` | Sobrecarga de clique resiliente para `ILocator`. | `await Ui.ClickWhenVisibleAndEnabledAsync(myLoc)` |
| `Task SelectAutocompleteAsync(string selector, string value, UiOptions? options = null)` | Seleciona opção em autocomplete, opcionalmente digitando sequencialmente. | `await Ui.SelectAutocompleteAsync("#cidade", "SP", new() { Type = true })` |
| `Task SelectAutocompleteAsync(string selector, string value, bool type, UiOptions? options = null)` | Sobrecarga ergonômica com parâmetro direto `type`. | `await Ui.SelectAutocompleteAsync("#cidade", "SP", type: true)` |
| `Task SelectOptionWithTextAsync(string selector, string text, float timeout = 30000)` | Abre dropdown, clica na opção com texto e fecha com Escape. | `await Ui.SelectOptionWithTextAsync("#combo", "Ativo")` |
| `Task ClickUntilVisibleAsync(string clickSelector, string targetSelector, float timeout = 30000, int intervalMs = 500)` | Clica repetidamente no gatilho até o elemento alvo ficar visível. | `await Ui.ClickUntilVisibleAsync(".chk", "#btn-prosseguir")` |
| `Task ValidateTextInElementAsync(string selector, IEnumerable<string> texts, float timeout = 30000)` | Valida que o seletor contém todos os textos da coleção com asserções nativas. | `await Ui.ValidateTextInElementAsync(".card", [id, "R$ 10"])` |
| `Task ValidateTextInElementAsync(ILocator locator, IEnumerable<string> texts, float timeout = 30000)` | Valida coleção de textos em um `ILocator`. | `await Ui.ValidateTextInElementAsync(cardLoc, textos)` |
| `Task SearchFormAsync(IEnumerable<SearchField> fields, string? submitButtonSelector = null, float timeout = 30000)` | Preenche sequencialmente campos de busca e aciona botão de envio. | `await Ui.SearchFormAsync(campos, "#btn-buscar")` |
| `Task ReloadUntilVisibleAsync(string selector, string expectedText, ReloadOptions? options = null)` | Recarrega a página em loop até que o texto normalizado seja encontrado. | `await Ui.ReloadUntilVisibleAsync("#status", "Concluído")` |
| `Task<string> ValidateDownloadAsync(Func<Task> trigger)` | Intercepta download nativo e retorna o nome sugerido do arquivo. | `string name = await Ui.ValidateDownloadAsync(btn.ClickAsync)` |
| `Task<DownloadResult> ValidateDownloadDetailedAsync(Func<Task> trigger, string? saveTo = null)` | Intercepta download e persiste opcionalmente na pasta `downloads/`. | `var dl = await Ui.ValidateDownloadDetailedAsync(action, "doc.pdf")` |
| `Task<string> FillGeneratedNameAsync(string selector, string prefix = "Cliente", UiOptions? options = null)` | Gera nome único, preenche o campo e retorna o valor preenchido. | `string nome = await Ui.FillGeneratedNameAsync("#nome")` |
| `Task<string> FillGeneratedCpfAsync(string selector, bool mask = false, UiOptions? options = null)` | Gera CPF válido, preenche o campo e retorna o valor. | `string cpf = await Ui.FillGeneratedCpfAsync("#cpf", mask: true)` |
| `Task<string> FillGeneratedCnpjAsync(string selector, bool mask = false, UiOptions? options = null)` | Gera CNPJ válido, preenche o campo e retorna o valor. | `string cnpj = await Ui.FillGeneratedCnpjAsync("#cnpj", true)` |
| `Task<string> FillGeneratedEmailAsync(string selector, string domain = "empresa.com", UiOptions? options = null)` | Gera e-mail único, preenche o campo e retorna o valor. | `string email = await Ui.FillGeneratedEmailAsync("#email")` |
| `Task<string> FillGeneratedPhoneAsync(string selector, bool mask = false, UiOptions? options = null)` | Gera telefone celular brasileiro, preenche o campo e retorna o valor. | `string tel = await Ui.FillGeneratedPhoneAsync("#tel", true)` |

### `AuthHelper`
| Assinatura | Descrição | Exemplo de Uso |
|---|---|---|
| `Task LoginPlatformAsync(string? email = null, string? password = null, LoginOptions? options = null)` | Fluxo completo de login resiliente (limpa cookies/storage, trata hash routing e sessão concorrente). | `await Auth.LoginPlatformAsync()` |
| `Task LogoutAsync(string userMenuSelector = "#user-menu", string logoutButtonSelector = "#btn-logout")` | Aciona o menu do usuário, aguarda botão de saída e limpa armazenamento e cookies. | `await Auth.LogoutAsync()` |

### `ApiHelper`
| Assinatura | Descrição | Exemplo de Uso |
|---|---|---|
| `Task<string> LoginViaApiAsync(string user, string pass, string loginEndpoint = "/api/v1/auth/login")` | Efetua login HTTP direto e extrai o token JWT da resposta. | `string token = await Api.LoginViaApiAsync("admin", "123")` |
| `Task<T> SeedRecordViaApiAsync<T>(string endpoint, object payload, string? token = null)` | Cria registro via API (POST) com desserialização genérica do retorno. | `var user = await Api.SeedRecordViaApiAsync<UserDto>("/users", payload)` |
| `Task<string> SeedRecordViaApiAsync(string endpoint, object payload, string? token = null)` | Cria registro via API retornando o corpo textual da resposta. | `string json = await Api.SeedRecordViaApiAsync("/pedidos", payload)` |
| `static string CombineUrl(string baseUrl, string endpoint)` | Concatena URLs com segurança sem duplicar ou omitir barras. | `ApiHelper.CombineUrl("http://api", "/v1")` |

### `TestConfig` (Classe Estática)
| Propriedade / Método | Descrição |
|---|---|
| `string BaseUrl` | URL base da aplicação (fallback: `https://demo.playwright.dev/todomvc`). |
| `string ApiBaseUrl` | URL base da API configurada no ambiente. |
| `string UserEmail` | E-mail do usuário configurado. |
| `string UserPassword` | Senha do usuário configurada. |
| `string TestEnv` | Nome do ambiente atual (padrão: `qa`). |
| `bool IgnoreHttpsErrors` | Indica se deve ignorar erros de certificado HTTPS (padrão: `true`). |
| `string GetRequired(string name)` | Obtém variável obrigatória ou lança exceção detalhada caso ausente. |

### `BaseTest : PageTest`
| Membro | Descrição |
|---|---|
| `UiHelper Ui` | Instância gerenciada do `UiHelper` para a página atual do teste. |
| `AuthHelper Auth` | Instância gerenciada do `AuthHelper` para autenticação. |
| `ApiHelper Api` | Instância lazy do `ApiHelper` (exige `API_BASE_URL` apenas quando acessada). |
| `BrowserNewContextOptions ContextOptions()` | Override que configura Viewport 1366x768, Downloads habilitados e BaseURL. |
| `[SetUp] BaseSetUpAsync()` | Configura timeouts (20s/30s/15s), instancia helpers e inicia o tracing. |
| `[TearDown] BaseTearDownAsync()` | Salva screenshot em `screenshots/` e trace em `traces/<teste>.zip` se falhar. |

---

## 🔄 Diferenças em Relação ao TypeScript

Ao portar o toolkit de TypeScript para C#, algumas adaptações foram necessárias para aderir aos padrões idiomáticos da plataforma .NET:

1. **Herança em vez de `test.extend` (Fixtures)**:
   - *TypeScript*: Utilizava `baseTest.extend<CustomFixtures>({ ui, auth, api })`.
   - *C#*: O Playwright para .NET baseia-se em herança via `PageTest`. O `BaseTest` herda de `PageTest` e expõe propriedades fortemente tipadas (`Ui`, `Auth` e `Api`).
2. **Configuração via `.runsettings` e `ContextOptions`**:
   - *TypeScript*: Centralizava configurações em `playwright.config.ts`.
   - *C#*: Configurações de navegador ficam no arquivo `.runsettings` e opções de contexto são definidas no override de `ContextOptions()` da classe base.
3. **Ausência do runner interativo `--ui`**:
   - *TypeScript*: Possui a flag nativa `npx playwright test --ui`.
   - *C#*: A visualização interativa é feita via Playwright Trace Viewer (`playwright.ps1 show-trace traces/<arquivo>.zip`) ou extensão do Visual Studio / VS Code.
4. **Respeito às checagens de acionabilidade (`Force = false`)**:
   - *TypeScript*: Forçava cliques cegos com `force: true` e esperas redundantes.
   - *C#*: `Force` padrão é `false`, permitindo que o Playwright aguarde o elemento estar estável, visível e clicável.
5. **Tipagem e Modelos Fortemente Tipados**:
   - As opções literais anônimas de TS viraram records C# (`UiOptions`, `LoginOptions`, `SearchField`, `ReloadOptions`, `DownloadResult`).

---

## 🌟 Boas Práticas de Automação

1. **Priorize Localizadores Semânticos**: Dê preferência a `GetByRole()`, `GetByLabel()` e `GetByTestId()` em vez de seletores CSS complexos baseados em estrutura volátil de classes.
2. **Evite `Page.WaitForTimeoutAsync` e `NetworkIdle`**:
   - `WaitForTimeout` introduz lentidão e flakiness desnecessários.
   - `NetworkIdle` é desaconselhado pelo Playwright oficial pois conexões WebSocket ou polling constante impedem que o estado seja atingido.
3. **API Seeding para Preparação de Estado**:
   - Em vez de navegar por 5 telas de cadastro para criar uma entidade antes de testá-la, use `Api.SeedRecordViaApiAsync` para injetar o registro em milissegundos e abra a UI direto no cenário relevante.
4. **Tratamento de Retry**:
   - No NUnit, utilize o atributo `[Retry(2)]` em testes específicos que tenham dependências externas conhecidas, ou configure a repetição no CI.

---

## 🔍 Diagnóstico e Visualização de Traces

Sempre que um teste falhar, o `BaseTest` salva automaticamente um arquivo de trace `.zip` na pasta `traces/` da raiz do projeto.

Para inspecionar passo a passo o teste com a linha do tempo, rede, ações e visualização DOM:
```powershell
pwsh tests/PlaywrightMasterToolkit.Tests/bin/Debug/net8.0/playwright.ps1 show-trace traces/NomeDoTeste.zip
```

---

## 📦 Como Utilizar em Outros Projetos

### Opção 1: Referência Direta de Projeto
Adicione a referência do projeto `PlaywrightMasterToolkit.csproj` ao seu projeto de testes existente:
```bash
dotnet add MeuProjeto.Tests.csproj reference ../PlaywrightMasterToolkit/src/PlaywrightMasterToolkit/PlaywrightMasterToolkit.csproj
```

### Opção 2: Empacotamento NuGet Local
Gere o pacote `.nupkg`:
```bash
dotnet pack src/PlaywrightMasterToolkit/PlaywrightMasterToolkit.csproj -c Release -o ./nupkg
```
E instale no seu projeto:
```bash
dotnet add package PlaywrightMasterToolkit --source ./nupkg
```

Nas suas classes de teste, basta herdar de `BaseTest`:
```csharp
using NUnit.Framework;
using PlaywrightMasterToolkit.Base;

[TestFixture]
public class ExemploTests : BaseTest
{
    [Test]
    public async Task MeuCenario()
    {
        await Page.GotoAsync(TestConfig.BaseUrl);
        await Ui.WaitForPageLoadAsync();
        string nome = await Ui.FillGeneratedNameAsync(".new-todo", "Exemplo");
        await Ui.ValidateTextInElementAsync(".todo-list", nome);
    }
}
```

---

## 📄 Licença
Distribuído sob a licença [MIT](LICENSE). Copyright © 2026 Matheus Neves.
