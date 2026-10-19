# Changelog

Todas as mudanças notáveis deste projeto serão documentadas neste arquivo.

O formato é baseado no [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/) e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [1.0.0] - 2026-10-19

### Adicionado
- Scaffold da solução C# (.NET 8) com projetos `PlaywrightMasterToolkit` (biblioteca de classes) e `PlaywrightMasterToolkit.Tests` (testes NUnit).
- `TestConfig`: Gerenciamento multi-ambiente via `DotNetEnv` com busca recursiva da pasta `config/` e propriedades tipadas (`BaseUrl`, `ApiBaseUrl`, `UserEmail`, `UserPassword`, `GetRequired`).
- `DataGenerator`: Geração dinâmica de massas de dados com `UniqueHash`, `Email`, `Cellphone`, `Cpf`, `Cnpj`, `Name` e `NormalizeText`.
- `DataGenerator`: Novos validadores matemáticos de documentos brasileiros `IsValidCpf` e `IsValidCnpj` com suporte a entradas com ou sem máscara.
- `UiHelper`: Ações de interface com resiliência (`InteractWithElementAsync`, `ClickWhenVisibleAndEnabledAsync`, `SelectAutocompleteAsync`, `SelectOptionWithTextAsync`, `ClickUntilVisibleAsync`, `ValidateTextInElementAsync`, `SearchFormAsync`, `ReloadUntilVisibleAsync`).
- `UiHelper`: Novos métodos auxiliares para preenchimento de massas geradas retornando o valor preenchido (`FillGeneratedNameAsync`, `FillGeneratedCpfAsync`, `FillGeneratedCnpjAsync`, `FillGeneratedEmailAsync`, `FillGeneratedPhoneAsync`).
- `UiHelper`: Novo método `ValidateDownloadDetailedAsync` retornando `DownloadResult(FileName, Path)` com persistência garantida na pasta `downloads/`.
- `AuthHelper`: Fluxo de autenticação resiliente `LoginPlatformAsync` com limpeza de armazenamento/cookies, tratamento de sessão concorrente ativa e `LogoutAsync`.
- `ApiHelper`: Cliente HTTP com `LoginViaApiAsync` e injeção genérica de dados `SeedRecordViaApiAsync<T>` utilizando `System.Text.Json`.
- `BaseTest`: Classe base de testes herdando de `PageTest` do Playwright com injeção de `Ui`, `Auth`, lazy `Api`, captura automática de screenshots e traces em falhas anexados ao relatório do NUnit.
- Suíte de testes de fumaça (Smoke Tests) cobrindo geradores de dados, interações de UI com TodoMVC e mocks determinísticos de API via `HttpListener`.
- Pipeline de integração contínua no GitHub Actions (`.github/workflows/ci.yml`).

### Mudanças de Comportamento em Relação ao Código Original em TypeScript
- **`InteractWithElementAsync` (Force padrão `false`)**: O código original forçava `force: true` em todas as interações e incluía `waitFor` redundante. No toolkit C#, `Force` padrão foi alterado para `false`, permitindo que o Playwright execute suas verificações completas de acionabilidade (visibilidade, estabilidade, recebedor de eventos e habilitação). A opção `Force = true` permanece disponível para casos excepcionais via `UiOptions`.
- **`InteractWithElementAsync` (`UiAction` enum)**: Strings literais como `"click"` e `"type"` foram substituídas pelo enum fortemente tipado `UiAction`.
- **`WaitForPageLoadAsync` (`waitForNetworkIdle`)**: Adicionado parâmetro opcional `waitForNetworkIdle = false`. Por padrão, apenas `DOMContentLoaded` e `Load` são aguardados, evitando travamentos em páginas com WebSockets ou requisições contínuas de polling.
- **`SelectAutocompleteAsync` (`PressSequentially`)**: No modo digitação (`type: true`), utiliza `PressSequentiallyAsync` com delay entre teclas em vez de `FillAsync`, garantindo o disparo de eventos de teclado requeridos por componentes como MUI e AntDesign.
- **`SelectOptionWithTextAsync` (Tecla `Escape`)**: Envia a tecla `Escape` após a seleção para garantir o fechamento seguro de dropdowns suspensos.
- **`ClickUntilVisibleAsync` (Stopwatch e diagnóstico)**: Substituição de `DateTime` por `Stopwatch`, tolerância a re-renderizações e lançamento de `TimeoutException` detalhando com precisão qual seletor faltou.
- **`ValidateTextInElementAsync` (Asserções nativas)**: Remoção de esperas manuais redundantes; uso direto de `Expect(locator).ToContainTextAsync(...)`.
- **`SearchFormAsync` (Resolução de `OptionIndex`)**: O índice especificado em `OptionIndex` é de fato respeitado através de `.Nth(OptionIndex)` nos elementos de opções.
- **`ReloadUntilVisibleAsync` (Normalização e múltiplos elementos)**: Inspeciona todos os elementos correspondentes via `AllTextContentsAsync()` com normalização de texto sem acentos, sem depender apenas do primeiro elemento.
- **`AuthHelper.LoginPlatformAsync` (Limpeza e rotas com hash)**: Limpa cookies e `localStorage`/`sessionStorage` em bloco seguro, valida preenchimento com `ToHaveValueAsync`, trata rotas baseadas em hash (`cleanPath`) e resolve modais de sessão concorrente ativa sem `Thread.Sleep`.
- **`DataGenerator` (Distribuição uniforme e sem repetidos)**: Substituição de `Math.round` por `Random.Shared.Next(10)`, eliminando distorção estatística nos dígitos 0 e 9, e regeneração automática de CPFs com dígitos idênticos.
