using System.Text.RegularExpressions;

namespace PlaywrightMasterToolkit.Tests.Smoke;

[TestFixture]
public class DataGeneratorTests
{
    [Test]
    public void Cpf_ShouldGenerateValidCpfs_WithAndWithoutMask()
    {
        for (int i = 0; i < 100; i++)
        {
            string maskedCpf = DataGenerator.Cpf(mask: true);
            Assert.That(maskedCpf, Does.Match(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$"), $"CPF com máscara inválido: {maskedCpf}");
            Assert.That(DataGenerator.IsValidCpf(maskedCpf), Is.True, $"CPF com máscara não validado: {maskedCpf}");

            string unmaskedCpf = DataGenerator.Cpf(mask: false);
            Assert.That(unmaskedCpf, Does.Match(@"^\d{11}$"), $"CPF sem máscara inválido: {unmaskedCpf}");
            Assert.That(DataGenerator.IsValidCpf(unmaskedCpf), Is.True, $"CPF sem máscara não validado: {unmaskedCpf}");
        }
    }

    [Test]
    public void Cnpj_ShouldGenerateValidCnpjs_WithAndWithoutMask()
    {
        for (int i = 0; i < 100; i++)
        {
            string maskedCnpj = DataGenerator.Cnpj(mask: true);
            Assert.That(maskedCnpj, Does.Match(@"^\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}$"), $"CNPJ com máscara inválido: {maskedCnpj}");
            Assert.That(DataGenerator.IsValidCnpj(maskedCnpj), Is.True, $"CNPJ com máscara não validado: {maskedCnpj}");

            string unmaskedCnpj = DataGenerator.Cnpj(mask: false);
            Assert.That(unmaskedCnpj, Does.Match(@"^\d{14}$"), $"CNPJ sem máscara inválido: {unmaskedCnpj}");
            Assert.That(DataGenerator.IsValidCnpj(unmaskedCnpj), Is.True, $"CNPJ sem máscara não validado: {unmaskedCnpj}");
        }
    }

    [Test]
    public void Email_ShouldGenerateUniqueEmails()
    {
        var emails = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            string email = DataGenerator.Email("usuario", "teste.com");
            Assert.That(emails.Add(email), Is.True, $"E-mail duplicado gerado: {email}");
            Assert.That(email, Does.EndWith("@teste.com"));
        }
    }

    [Test]
    public void Cellphone_ShouldMatchExpectedFormat()
    {
        for (int i = 0; i < 50; i++)
        {
            string masked = DataGenerator.Cellphone(mask: true);
            Assert.That(masked, Does.Match(@"^\(\d{2}\) 9\d{4}-\d{4}$"), $"Celular mascarado no formato incorreto: {masked}");

            string unmasked = DataGenerator.Cellphone(mask: false);
            Assert.That(unmasked, Does.Match(@"^\d{2}9\d{8}$"), $"Celular sem máscara no formato incorreto: {unmasked}");
        }
    }

    [Test]
    public void NormalizeText_ShouldRemoveAccentsAndCollapseWhitespaces()
    {
        string input = "  Ação   Éxito ";
        string result = DataGenerator.NormalizeText(input);
        Assert.That(result, Is.EqualTo("acao exito"));
    }

    [TestCase("111.111.111-11", ExpectedResult = false)]
    [TestCase("000.000.000-00", ExpectedResult = false)]
    [TestCase("222.222.222-22", ExpectedResult = false)]
    [TestCase("123.456.789-00", ExpectedResult = false)]
    [TestCase("1234567890", ExpectedResult = false)]
    [TestCase("", ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool IsValidCpf_ShouldDetectKnownInvalidValues(string? cpf)
    {
        return DataGenerator.IsValidCpf(cpf);
    }

    [TestCase("11.111.111/1111-11", ExpectedResult = false)]
    [TestCase("00.000.000/0000-00", ExpectedResult = false)]
    [TestCase("12.345.678/0001-99", ExpectedResult = false)]
    [TestCase("12345678000199", ExpectedResult = false)]
    [TestCase("12345", ExpectedResult = false)]
    [TestCase("", ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool IsValidCnpj_ShouldDetectKnownInvalidValues(string? cnpj)
    {
        return DataGenerator.IsValidCnpj(cnpj);
    }
}
