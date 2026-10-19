using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PlaywrightMasterToolkit.Tests.Smoke;

/// <summary>
/// DTO de exemplo para teste de injeção de massa via API.
/// </summary>
public record OrderDto(int Id, string Codigo, double Valor);

[TestFixture]
public class FixturesTests : BaseTest
{
    [Test]
    public void Helpers_ShouldBeInstantiatedByBaseTest()
    {
        Assert.That(Ui, Is.Not.Null, "UiHelper deve ser instanciado no SetUp do BaseTest.");
        Assert.That(Auth, Is.Not.Null, "AuthHelper deve ser instanciado no SetUp do BaseTest.");
    }

    [Test]
    public void Api_WithoutBaseUrl_ShouldThrowExplicitException()
    {
        string? previous = Environment.GetEnvironmentVariable("API_BASE_URL");
        try
        {
            Environment.SetEnvironmentVariable("API_BASE_URL", null);
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                _ = Api;
            });
            Assert.That(ex!.Message, Does.Contain("API_BASE_URL"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("API_BASE_URL", previous);
        }
    }

    [Test]
    public async Task ApiHelper_WithLocalHttpServerMock_ShouldLoginAndSeed()
    {
        // Decisão de arquitetura: Utilização de HttpListener local com porta dinâmica.
        // O contexto IAPIRequestContext do Playwright atua como um cliente HTTP puro e não é interceptado por BrowserContext.RouteAsync.
        // O HttpListener em processo garante isolamento completo, estabilidade determinística e execução offline ultrarrápida.
        int port = GetFreePort();
        string mockBaseUrl = $"http://127.0.0.1:{port}/";

        using var listener = new HttpListener();
        listener.Prefixes.Add(mockBaseUrl);
        listener.Start();

        var serverTask = Task.Run(async () =>
        {
            for (int i = 0; i < 2; i++)
            {
                var context = await listener.GetContextAsync();
                var req = context.Request;
                var res = context.Response;

                if (req.Url?.AbsolutePath == "/api/v1/auth/login" && req.HttpMethod == "POST")
                {
                    string json = JsonSerializer.Serialize(new { token = "jwt_mock_token_123" });
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    res.StatusCode = 200;
                    res.ContentType = "application/json";
                    await res.OutputStream.WriteAsync(buffer);
                    res.Close();
                }
                else if (req.Url?.AbsolutePath == "/api/v1/orders" && req.HttpMethod == "POST")
                {
                    string authHeader = req.Headers["Authorization"] ?? "";
                    Assert.That(authHeader, Is.EqualTo("Bearer jwt_mock_token_123"));

                    string json = JsonSerializer.Serialize(new OrderDto(1, "PED_12345", 2500.0));
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    res.StatusCode = 200;
                    res.ContentType = "application/json";
                    await res.OutputStream.WriteAsync(buffer);
                    res.Close();
                }
            }
        });

        var requestContext = await Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = mockBaseUrl
        });

        try
        {
            var apiHelper = new ApiHelper(requestContext, mockBaseUrl);

            // 1. Login via API
            string token = await apiHelper.LoginViaApiAsync("admin", "admin123");
            Assert.That(token, Is.EqualTo("jwt_mock_token_123"));

            // 2. Data seeding via API
            var orderPayload = new { codigo = "PED_12345", valor = 2500.0 };
            var order = await apiHelper.SeedRecordViaApiAsync<OrderDto>("/api/v1/orders", orderPayload, token);

            Assert.That(order, Is.Not.Null);
            Assert.That(order.Codigo, Is.EqualTo("PED_12345"));
            Assert.That(order.Valor, Is.EqualTo(2500.0));

            await serverTask;
        }
        finally
        {
            await requestContext.DisposeAsync();
            listener.Stop();
        }
    }

    private static int GetFreePort()
    {
        using var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        int port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();
        return port;
    }
}
