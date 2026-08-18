namespace Tooark.Validations.Documents;

/// <summary>
/// Verificação dos dígitos verificadores de CPF e CNPJ.
/// </summary>
/// <remarks>
/// O CNPJ segue o cálculo alfanumérico publicado pelo Serpro, vigente desde julho de 2026: o valor de cada
/// caractere é o código ASCII menos 48, o que faz os dígitos manterem o próprio valor e as letras assumirem
/// de 17 (A) a 42 (Z). Como os dígitos preservam o valor, o mesmo cálculo atende aos CNPJ numéricos antigos.
/// </remarks>
public static class DocumentDigit
{
  #region Constants

  /// <summary>
  /// Quantidade de dígitos do CPF, incluindo os verificadores.
  /// </summary>
  private const int CpfLength = 11;

  /// <summary>
  /// Quantidade de caracteres do CNPJ que antecedem os dígitos verificadores.
  /// </summary>
  private const int CnpjBaseLength = 12;

  /// <summary>
  /// Deslocamento aplicado ao código ASCII para obter o valor de cálculo do caractere.
  /// </summary>
  private const int AsciiOffset = 48;

  /// <summary>
  /// Menor peso da distribuição, aplicado ao caractere mais à direita.
  /// </summary>
  private const int MinWeight = 2;

  /// <summary>
  /// Quantidade de pesos distintos antes da distribuição recomeçar.
  /// </summary>
  private const int WeightRange = 8;

  /// <summary>
  /// Quantidade de dígitos da CNH, incluindo os verificadores.
  /// </summary>
  private const int CnhLength = 11;

  /// <summary>
  /// Quantidade de caracteres do RG que antecedem o dígito verificador.
  /// </summary>
  private const int RgBaseLength = 8;

  #endregion


  #region Cpf

  /// <summary>
  /// Verifica os dígitos verificadores de um CPF.
  /// </summary>
  /// <param name="value">CPF a ser verificado, com ou sem formatação.</param>
  /// <returns>True quando os dígitos verificadores conferem.</returns>
  public static bool IsCpf(string? value)
  {
    // Mantém apenas os dígitos, descartando a formatação
    var digits = OnlyDigits(value);

    // Verifica a quantidade de dígitos
    if (digits.Length != CpfLength)
    {
      return false;
    }

    // Sequências de um único dígito repetido satisfazem o cálculo, mas não são CPF válido
    if (IsRepeated(digits))
    {
      return false;
    }

    // Calcula os dois dígitos verificadores a partir da base
    var first = CpfDigit(digits, CpfLength - 2);
    var second = CpfDigit(digits, CpfLength - 1);

    // Compara com os dígitos informados
    return digits[CpfLength - 2] - '0' == first && digits[CpfLength - 1] - '0' == second;
  }

  /// <summary>
  /// Calcula um dígito verificador do CPF.
  /// </summary>
  /// <param name="digits">Dígitos do CPF.</param>
  /// <param name="length">Quantidade de dígitos considerados no cálculo.</param>
  /// <returns>Dígito verificador calculado.</returns>
  private static int CpfDigit(string digits, int length)
  {
    var sum = 0;

    // Os pesos decrescem a partir de length + 1, da esquerda para a direita
    for (var index = 0; index < length; index++)
    {
      sum += (digits[index] - '0') * (length + 1 - index);
    }

    return Remainder(sum);
  }

  #endregion


  #region Cnpj

  /// <summary>
  /// Verifica os dígitos verificadores de um CNPJ, numérico ou alfanumérico.
  /// </summary>
  /// <param name="value">CNPJ a ser verificado, com ou sem formatação.</param>
  /// <returns>True quando os dígitos verificadores conferem.</returns>
  public static bool IsCnpj(string? value)
  {
    // Mantém apenas os caracteres alfanuméricos, descartando a formatação
    var characters = OnlyLetterOrDigits(value);

    // Verifica a quantidade de caracteres
    if (characters.Length != CnpjBaseLength + 2)
    {
      return false;
    }

    // Os dígitos verificadores são sempre numéricos
    if (!char.IsAsciiDigit(characters[CnpjBaseLength]) || !char.IsAsciiDigit(characters[CnpjBaseLength + 1]))
    {
      return false;
    }

    // A base admite dígitos e letras maiúsculas
    for (var index = 0; index < CnpjBaseLength; index++)
    {
      if (!char.IsAsciiDigit(characters[index]) && !char.IsAsciiLetterUpper(characters[index]))
      {
        return false;
      }
    }

    // Sequências de um único caractere repetido satisfazem o cálculo, mas não são CNPJ válido
    if (IsRepeated(characters[..CnpjBaseLength]))
    {
      return false;
    }

    // Calcula o primeiro dígito sobre a base e o segundo sobre a base acrescida do primeiro
    var first = CnpjDigit(characters[..CnpjBaseLength]);
    var second = CnpjDigit(characters[..CnpjBaseLength] + first);

    // Compara com os dígitos informados
    return characters[CnpjBaseLength] - '0' == first && characters[CnpjBaseLength + 1] - '0' == second;
  }

  /// <summary>
  /// Calcula um dígito verificador do CNPJ.
  /// </summary>
  /// <remarks>
  /// Os pesos vão de 2 a 9, distribuídos da direita para a esquerda e recomeçando após o oitavo caractere.
  /// </remarks>
  /// <param name="characters">Caracteres considerados no cálculo.</param>
  /// <returns>Dígito verificador calculado.</returns>
  private static int CnpjDigit(string characters)
  {
    var sum = 0;

    // Percorre da esquerda para a direita, derivando o peso pela posição a partir da direita
    for (var index = 0; index < characters.Length; index++)
    {
      var positionFromRight = characters.Length - 1 - index;
      var weight = MinWeight + (positionFromRight % WeightRange);

      sum += (characters[index] - AsciiOffset) * weight;
    }

    return Remainder(sum);
  }

  #endregion


  #region Cnh

  /// <summary>
  /// Verifica os dígitos verificadores de uma CNH.
  /// </summary>
  /// <remarks>
  /// Segue o cálculo do registro nacional: o primeiro dígito usa pesos de 9 a 1 e o segundo, de 1 a 9,
  /// ambos sobre os nove primeiros dígitos, com o resto do módulo 11 reduzido a zero quando chega a 10.
  /// </remarks>
  /// <param name="value">CNH a ser verificada, com ou sem formatação.</param>
  /// <returns>True quando os dígitos verificadores conferem.</returns>
  public static bool IsCnh(string? value)
  {
    // Mantém apenas os dígitos, descartando a formatação
    var digits = OnlyDigits(value);

    // Verifica a quantidade de dígitos
    if (digits.Length != CnhLength)
    {
      return false;
    }

    // Registro composto apenas por zeros não é uma CNH válida
    if (IsRepeated(digits) && digits[0] == '0')
    {
      return false;
    }

    var descending = 0;
    var ascending = 0;

    // Percorre a base aplicando os pesos decrescentes e crescentes na mesma passagem
    for (var index = 0; index < CnhLength - 2; index++)
    {
      var digit = digits[index] - '0';

      descending += digit * (9 - index);
      ascending += digit * (1 + index);
    }

    // O resto do módulo 11 vira zero quando chega a 10
    var first = descending % 11;
    var second = ascending % 11;

    first = first == 10 ? 0 : first;
    second = second == 10 ? 0 : second;

    // Compara com os dígitos informados
    return digits[CnhLength - 2] - '0' == first && digits[CnhLength - 1] - '0' == second;
  }

  #endregion


  #region Rg

  /// <summary>
  /// Verifica o dígito verificador de um RG.
  /// </summary>
  /// <remarks>
  /// O RG não tem padrão nacional; o cálculo aqui é o da Secretaria de Segurança Pública de São Paulo,
  /// que é o formato aceito pelo pacote. O dígito é opcional: um RG informado sem ele passa pela
  /// verificação, e apenas um dígito informado e incorreto reprova o documento.
  /// O dígito pode ser 'X', que representa o valor 10.
  /// </remarks>
  /// <param name="value">RG a ser verificado, com ou sem formatação.</param>
  /// <returns>True quando o dígito verificador confere ou não foi informado.</returns>
  public static bool IsRg(string? value)
  {
    // Mantém apenas os caracteres alfanuméricos, em caixa alta para normalizar o 'X'
    var characters = OnlyLetterOrDigits(value);

    // O RG tem oito dígitos de base, com o verificador opcional
    if (characters.Length != RgBaseLength && characters.Length != RgBaseLength + 1)
    {
      return false;
    }

    // A base é sempre numérica
    for (var index = 0; index < RgBaseLength; index++)
    {
      if (!char.IsAsciiDigit(characters[index]))
      {
        return false;
      }
    }

    // Base composta apenas por zeros não é um RG válido
    if (IsRepeated(characters[..RgBaseLength]) && characters[0] == '0')
    {
      return false;
    }

    // Sem o dígito verificador não há o que conferir
    if (characters.Length == RgBaseLength)
    {
      return true;
    }

    var sum = 0;

    // Os pesos crescem de 2 a 9, da esquerda para a direita
    for (var index = 0; index < RgBaseLength; index++)
    {
      sum += (characters[index] - '0') * (MinWeight + index);
    }

    // O dígito 10 é representado pela letra X
    var digit = (11 - (sum % 11)) % 11;

    return digit == 10 ?
      characters[RgBaseLength] == 'X' :
      characters[RgBaseLength] - '0' == digit;
  }

  #endregion


  #region Helpers

  /// <summary>
  /// Obtém o dígito verificador a partir do somatório, pelo módulo 11.
  /// </summary>
  /// <param name="sum">Somatório dos produtos de valor e peso.</param>
  /// <returns>Dígito verificador. Zero quando o resto é 0 ou 1.</returns>
  private static int Remainder(int sum)
  {
    var remainder = sum % 11;

    return remainder < 2 ? 0 : 11 - remainder;
  }

  /// <summary>
  /// Verifica se todos os caracteres são iguais.
  /// </summary>
  /// <param name="value">Valor a ser verificado.</param>
  /// <returns>True quando o valor é composto por um único caractere repetido.</returns>
  private static bool IsRepeated(string value)
  {
    for (var index = 1; index < value.Length; index++)
    {
      if (value[index] != value[0])
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Mantém apenas os dígitos do valor.
  /// </summary>
  /// <param name="value">Valor a ser tratado.</param>
  /// <returns>Valor apenas com dígitos.</returns>
  private static string OnlyDigits(string? value) =>
    string.IsNullOrEmpty(value) ?
      string.Empty :
      string.Concat(value.Where(char.IsAsciiDigit));

  /// <summary>
  /// Mantém apenas os caracteres alfanuméricos do valor, em caixa alta.
  /// </summary>
  /// <remarks>
  /// O cálculo parte do código ASCII das letras maiúsculas, então a caixa é normalizada antes da conta.
  /// </remarks>
  /// <param name="value">Valor a ser tratado.</param>
  /// <returns>Valor apenas com caracteres alfanuméricos maiúsculos.</returns>
  private static string OnlyLetterOrDigits(string? value) =>
    string.IsNullOrEmpty(value) ?
      string.Empty :
      string.Concat(value.Where(char.IsAsciiLetterOrDigit).Select(char.ToUpperInvariant));

  #endregion
}
