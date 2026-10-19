using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PlaywrightMasterToolkit.Utils;

/// <summary>
/// Utilitário estático para geração e validação de massas de dados dinâmicas para automação de testes.
/// Suporta geração de CPFs, CNPJs válidos, e-mails únicos, telefones e normalização de textos.
/// </summary>
public static class DataGenerator
{
    private static readonly int[] CpfWeights1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] CpfWeights2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

    private static readonly int[] CnpjWeights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] CnpjWeights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>
    /// Gera um hash alfanumérico pseudoaleatório de tamanho especificado.
    /// </summary>
    /// <param name="length">Quantidade de caracteres desejada. Padrão é 8.</param>
    /// <returns>Cadeia alfanumérica única.</returns>
    public static string UniqueHash(int length = 8)
    {
        if (length <= 0) return string.Empty;

        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var span = new char[length];
        for (int i = 0; i < length; i++)
        {
            span[i] = chars[Random.Shared.Next(chars.Length)];
        }
        return new string(span);
    }

    /// <summary>
    /// Gera um nome com prefixo e hash único para evitar colisões em cadastros.
    /// </summary>
    /// <param name="prefix">Prefixo do nome. Padrão é "Cliente".</param>
    /// <returns>Nome formatado com hash único.</returns>
    public static string Name(string prefix = "Cliente")
    {
        return $"{prefix} {UniqueHash(6)}";
    }

    /// <summary>
    /// Gera um endereço de e-mail pseudoaleatório e único.
    /// </summary>
    /// <param name="prefix">Prefixo opcional do e-mail. Se nulo, utiliza "user".</param>
    /// <param name="domain">Domínio do e-mail. Padrão é "empresa.com".</param>
    /// <returns>E-mail único gerado.</returns>
    public static string Email(string? prefix = null, string domain = "empresa.com")
    {
        string p = string.IsNullOrWhiteSpace(prefix)
            ? "user"
            : NormalizeText(prefix).Replace(" ", ".");

        return $"{p}.{UniqueHash(8)}@{domain}";
    }

    /// <summary>
    /// Gera um número de telefone celular brasileiro (DDD + 9 dígitos), com ou sem máscara.
    /// </summary>
    /// <param name="mask">Se verdadeiro, aplica a máscara (XX) 9XXXX-XXXX. Padrão é false.</param>
    /// <returns>Número de telefone gerado.</returns>
    public static string Cellphone(bool mask = false)
    {
        int ddd = Random.Shared.Next(11, 99);
        int part1 = Random.Shared.Next(1000, 9999);
        int part2 = Random.Shared.Next(1000, 9999);

        return mask
            ? $"({ddd}) 9{part1:D4}-{part2:D4}"
            : $"{ddd}9{part1:D4}{part2:D4}";
    }

    /// <summary>
    /// Gera um CPF brasileiro matematicamente válido, com dígitos verificadores calculados.
    /// Garante distribuição uniforme e evita sequências inválidas de dígitos repetidos.
    /// </summary>
    /// <param name="mask">Se verdadeiro, aplica a máscara XXX.XXX.XXX-XX. Padrão é false.</param>
    /// <returns>Número de CPF gerado.</returns>
    public static string Cpf(bool mask = false)
    {
        int[] digits = new int[9];
        bool allEqual;

        do
        {
            for (int i = 0; i < 9; i++)
            {
                digits[i] = Random.Shared.Next(10);
            }

            allEqual = true;
            for (int i = 1; i < 9; i++)
            {
                if (digits[i] != digits[0])
                {
                    allEqual = false;
                    break;
                }
            }
        } while (allEqual);

        int sum1 = 0;
        for (int i = 0; i < 9; i++)
        {
            sum1 += digits[i] * CpfWeights1[i];
        }

        int rem1 = sum1 % 11;
        int d1 = rem1 < 2 ? 0 : 11 - rem1;

        int sum2 = 0;
        for (int i = 0; i < 9; i++)
        {
            sum2 += digits[i] * CpfWeights2[i];
        }
        sum2 += d1 * CpfWeights2[9];

        int rem2 = sum2 % 11;
        int d2 = rem2 < 2 ? 0 : 11 - rem2;

        return mask
            ? $"{digits[0]}{digits[1]}{digits[2]}.{digits[3]}{digits[4]}{digits[5]}.{digits[6]}{digits[7]}{digits[8]}-{d1}{d2}"
            : $"{digits[0]}{digits[1]}{digits[2]}{digits[3]}{digits[4]}{digits[5]}{digits[6]}{digits[7]}{digits[8]}{d1}{d2}";
    }

    /// <summary>
    /// Valida se um número de CPF é matematicamente autêntico. Aceita valores com ou sem pontuação.
    /// </summary>
    /// <param name="cpf">Cadeia de caracteres contendo o CPF a ser validado.</param>
    /// <returns>Verdadeiro se o CPF for válido; caso contrário, falso.</returns>
    public static bool IsValidCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return false;
        }

        string digits = Regex.Replace(cpf, @"\D", "");
        if (digits.Length != 11)
        {
            return false;
        }

        // Rejeita sequências com todos os dígitos iguais (ex: 111.111.111-11)
        bool allEqual = true;
        for (int i = 1; i < 11; i++)
        {
            if (digits[i] != digits[0])
            {
                allEqual = false;
                break;
            }
        }
        if (allEqual)
        {
            return false;
        }

        int sum1 = 0;
        for (int i = 0; i < 9; i++)
        {
            sum1 += (digits[i] - '0') * CpfWeights1[i];
        }

        int rem1 = sum1 % 11;
        int expD1 = rem1 < 2 ? 0 : 11 - rem1;
        if (digits[9] - '0' != expD1)
        {
            return false;
        }

        int sum2 = 0;
        for (int i = 0; i < 9; i++)
        {
            sum2 += (digits[i] - '0') * CpfWeights2[i];
        }
        sum2 += expD1 * CpfWeights2[9];

        int rem2 = sum2 % 11;
        int expD2 = rem2 < 2 ? 0 : 11 - rem2;

        return digits[10] - '0' == expD2;
    }

    /// <summary>
    /// Gera um número de CNPJ brasileiro matematicamente válido, com dígitos verificadores calculados.
    /// </summary>
    /// <param name="mask">Se verdadeiro, aplica a máscara XX.XXX.XXX/XXXX-XX. Padrão é false.</param>
    /// <returns>Número de CNPJ gerado.</returns>
    public static string Cnpj(bool mask = false)
    {
        int[] digits = new int[12];
        for (int i = 0; i < 8; i++)
        {
            digits[i] = Random.Shared.Next(10);
        }
        // Padrão matriz 0001
        digits[8] = 0;
        digits[9] = 0;
        digits[10] = 0;
        digits[11] = 1;

        int sum1 = 0;
        for (int i = 0; i < 12; i++)
        {
            sum1 += digits[i] * CnpjWeights1[i];
        }

        int rem1 = sum1 % 11;
        int d1 = rem1 < 2 ? 0 : 11 - rem1;

        int sum2 = 0;
        for (int i = 0; i < 12; i++)
        {
            sum2 += digits[i] * CnpjWeights2[i];
        }
        sum2 += d1 * CnpjWeights2[12];

        int rem2 = sum2 % 11;
        int d2 = rem2 < 2 ? 0 : 11 - rem2;

        return mask
            ? $"{digits[0]}{digits[1]}.{digits[2]}{digits[3]}{digits[4]}.{digits[5]}{digits[6]}{digits[7]}/{digits[8]}{digits[9]}{digits[10]}{digits[11]}-{d1}{d2}"
            : $"{digits[0]}{digits[1]}{digits[2]}{digits[3]}{digits[4]}{digits[5]}{digits[6]}{digits[7]}{digits[8]}{digits[9]}{digits[10]}{digits[11]}{d1}{d2}";
    }

    /// <summary>
    /// Valida se um número de CNPJ é matematicamente autêntico. Aceita valores com ou sem pontuação.
    /// </summary>
    /// <param name="cnpj">Cadeia de caracteres contendo o CNPJ a ser validado.</param>
    /// <returns>Verdadeiro se o CNPJ for válido; caso contrário, falso.</returns>
    public static bool IsValidCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
        {
            return false;
        }

        string digits = Regex.Replace(cnpj, @"\D", "");
        if (digits.Length != 14)
        {
            return false;
        }

        bool allEqual = true;
        for (int i = 1; i < 14; i++)
        {
            if (digits[i] != digits[0])
            {
                allEqual = false;
                break;
            }
        }
        if (allEqual)
        {
            return false;
        }

        int sum1 = 0;
        for (int i = 0; i < 12; i++)
        {
            sum1 += (digits[i] - '0') * CnpjWeights1[i];
        }

        int rem1 = sum1 % 11;
        int expD1 = rem1 < 2 ? 0 : 11 - rem1;
        if (digits[12] - '0' != expD1)
        {
            return false;
        }

        int sum2 = 0;
        for (int i = 0; i < 12; i++)
        {
            sum2 += (digits[i] - '0') * CnpjWeights2[i];
        }
        sum2 += expD1 * CnpjWeights2[12];

        int rem2 = sum2 % 11;
        int expD2 = rem2 < 2 ? 0 : 11 - rem2;

        return digits[13] - '0' == expD2;
    }

    /// <summary>
    /// Normaliza uma cadeia de texto removendo acentos, convertendo para minúsculas e colapsando múltiplos espaços em branco.
    /// </summary>
    /// <param name="input">Texto a ser normalizado.</param>
    /// <returns>Texto sem acentos e com espaçamento normalizado.</returns>
    public static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string normalizedFormD = input.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in normalizedFormD)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        string cleanString = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        return Regex.Replace(cleanString, @"\s+", " ").Trim();
    }
}
