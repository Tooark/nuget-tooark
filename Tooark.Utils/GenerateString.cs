using System.Security.Cryptography;
using System.Text;

namespace Tooark.Utils;

/// <summary>
/// Classe estática que fornece métodos para gerar strings.
/// </summary>
public static class GenerateString
{
  #region Methods

  /// <summary>
  /// Converte um número inteiro em uma representação equivalente alfabético do número.
  /// </summary>
  /// <param name="number">O número inteiro maior que zero a ser convertido.</param>
  /// <returns>Uma string em maiúsculas representando o equivalente alfabético do número. Vazia se o número não for positivo.</returns>
  /// <example>
  /// <code>
  /// string result = Sequential(1); // result: "A"
  /// string result = Sequential(27); // result: "AA"
  /// </code>
  /// </example>
  public static string Sequential(int number)
  {
    return InternalGenerateString.Sequential(number);
  }

  /// <summary>
  /// Gera uma string com critérios específicos.
  /// </summary>
  /// <remarks>
  /// Se todos os tipos de caractere estiverem desativados, todos são reativados. Comprimentos menores que 8 são
  /// elevados para 8. A string gerada contém ao menos um caractere de cada tipo ativado.
  /// </remarks>
  /// <param name="len">Comprimento da string a ser gerada. Valor padrão é 12. Deve ser maior ou igual a 8.</param>
  /// <param name="upper">Indica se deve incluir caracteres maiúsculos. Valor padrão é true.</param>
  /// <param name="lower">Indica se deve incluir caracteres minúsculos. Valor padrão é true.</param>
  /// <param name="number">Indica se deve incluir números. Valor padrão é true.</param>
  /// <param name="special">Indica se deve incluir caracteres especiais. Valor padrão é true.</param>
  /// <param name="similarity">Indica se deve utilizar caracteres semelhantes. Valor padrão é false.</param>
  /// <returns>Uma string gerada de acordo com os critérios especificados.</returns>
  public static string Password(int len = 12, bool upper = true, bool lower = true, bool number = true, bool special = true, bool similarity = false)
  {
    return InternalGenerateString.Password(len, upper, lower, number, special, similarity);
  }

  /// <summary>
  ///  Gera uma string hexadecimal aleatória.
  /// </summary>
  /// <param name="sizeToken">O tamanho da string hexadecimal a ser gerada. Valores menores que 2 são elevados para 2.</param>
  /// <returns>Uma string hexadecimal aleatória com exatamente o tamanho solicitado.</returns>
  public static string Hexadecimal(int sizeToken = 128)
  {
    return InternalGenerateString.Hexadecimal(sizeToken);
  }

  /// <summary>
  ///  Gera uma string Guid sem hífens.
  /// </summary>
  /// <returns>Uma string Guid sem hífens, com 32 caracteres.</returns>
  public static string GuidCode()
  {
    return InternalGenerateString.GuidCode();
  }

  /// <summary>
  /// Gera uma string de token.
  /// </summary>
  /// <param name="length">O comprimento da string de token a ser gerada. Valor padrão é 256. Deve ser maior ou igual a 256.</param>
  /// <returns>Uma string de token de no mínimo 256 caracteres.</returns>
  public static string Token(int length = 256)
  {
    return InternalGenerateString.Token(length);
  }

  #endregion
}

/// <summary>
/// Classe estática interna que fornece métodos para gerar strings.
/// </summary>
internal static class InternalGenerateString
{
  #region Private Static Fields

  /// <summary>
  /// Conjuntos de caracteres maiúsculos: sem caracteres semelhantes e completo.
  /// </summary>
  private static readonly string[] CharUpper = [
    "ABCDEFGHJKLMNPQRSTUVWXYZ",
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
  ];

  /// <summary>
  /// Conjuntos de caracteres minúsculos: sem caracteres semelhantes e completo.
  /// </summary>
  private static readonly string[] CharLower = [
    "abcdefghijkmnopqrstuvwxyz",
    "abcdefghijklmnopqrstuvwxyz",
  ];

  /// <summary>
  /// Conjuntos de números: sem caracteres semelhantes e completo.
  /// </summary>
  private static readonly string[] CharNumber = [
    "123456789",
    "1234567890",
  ];

  /// <summary>
  /// Conjuntos de caracteres especiais: reduzido e completo.
  /// </summary>
  private static readonly string[] CharSpecial = [
    "@#$%&*_",
    "@#$%&*!()[]{},.;<>:_-|",
  ];

  #endregion

  #region Internal Methods

  /// <summary>
  /// Converte um número inteiro em uma representação equivalente alfabético do número.
  /// </summary>
  /// <param name="number">O número inteiro maior que zero a ser convertido.</param>
  /// <returns>Uma string em maiúsculas representando o equivalente alfabético do número. Vazia se o número não for positivo.</returns>
  /// <example>
  /// <code>
  /// string result = Sequential(1); // result: "A"
  /// string result = Sequential(27); // result: "AA"
  /// </code>
  /// </example>
  internal static string Sequential(int number)
  {
    // Cria um objeto StringBuilder para armazenar a string resultante.
    var result = new StringBuilder();

    // Converte o número inteiro para sua representação alfabética.
    while (number > 0)
    {
      // Decrementa o número em 1 para que a divisão funcione corretamente.
      number--;

      // Calcula o caractere correspondente ao número.
      char letter = (char)('A' + (number % 26));

      // Adiciona o caractere ao início da string.
      result.Insert(0, letter);

      // Divide o número por 26.
      number /= 26;
    }

    // Retorna a string resultante.
    return result.ToString();
  }

  /// <summary>
  /// Gera uma string com critérios específicos.
  /// </summary>
  /// <param name="length">Comprimento da string a ser gerada. Valor padrão é 12. Deve ser maior ou igual a 8.</param>
  /// <param name="upperChar">Indica se deve incluir caracteres maiúsculos. Valor padrão é true.</param>
  /// <param name="lowerChar">Indica se deve incluir caracteres minúsculos. Valor padrão é true.</param>
  /// <param name="numberChar">Indica se deve incluir números. Valor padrão é true.</param>
  /// <param name="specialChar">Indica se deve incluir caracteres especiais. Valor padrão é true.</param>
  /// <param name="similarChar">Indica se deve utilizar caracteres semelhantes. Valor padrão é false.</param>
  /// <returns>Uma string gerada de acordo com os critérios especificados.</returns>
  internal static string Password(
    int length = 12,
    bool upperChar = true,
    bool lowerChar = true,
    bool numberChar = true,
    bool specialChar = true,
    bool similarChar = false)
  {
    // Verifica se pelo menos um tipo de caractere está ativado.
    if (!upperChar && !lowerChar && !numberChar && !specialChar)
    {
      // Aplica critérios padrão.
      upperChar = true;
      lowerChar = true;
      numberChar = true;
      specialChar = true;
    }

    // Define o comprimento da string aleatória.
    int lenRandom = length >= 8 ? length : 8;

    // Define utiliza caracteres similares ou apenas distintos.
    int distinctOrComplete = similarChar ? 1 : 0;

    // Define a lista de conjuntos de caracteres que estarão disponíveis para uso.
    List<string> chars = [];

    // Adiciona os conjuntos de caracteres maiúsculos com similares ou distintos.
    if (upperChar)
    {
      chars.Add(CharUpper[distinctOrComplete]);
    }

    // Adiciona os conjuntos de caracteres minúsculos com similares ou distintos.
    if (lowerChar)
    {
      chars.Add(CharLower[distinctOrComplete]);
    }

    // Adiciona os conjuntos de caracteres números com similares ou distintos.
    if (numberChar)
    {
      chars.Add(CharNumber[distinctOrComplete]);
    }

    // Adiciona os conjuntos de caracteres especiais com similares ou distintos.
    if (specialChar)
    {
      chars.Add(CharSpecial[distinctOrComplete]);
    }

    // Define o tipo de caractere para cada posição da string, garantindo pelo menos um de cada tipo ativado.
    int[] typeChar = new int[lenRandom];

    // Distribui os tipos de forma cíclica antes do embaralhamento.
    for (int i = 0; i < lenRandom; i++)
    {
      // Define o tipo de caractere para cada posição da string.
      typeChar[i] = i % chars.Count;
    }

    // Embaralha a ordem dos tipos de caractere.
    Shuffle(typeChar);

    // Define o array de caracteres gerados.
    var arrayChar = new char[lenRandom];

    // Gera a string aleatória.
    for (int i = 0; i < arrayChar.Length; i++)
    {
      // Seleciona o conjunto de caracteres do tipo sorteado para a posição.
      var charSet = chars[typeChar[i]];

      // Seleciona um caractere aleatório do conjunto de caracteres correspondente.
      arrayChar[i] = charSet[RandomNumberGenerator.GetInt32(charSet.Length)];
    }

    // Retorna a string gerada.
    return new string(arrayChar);
  }

  /// <summary>
  ///  Gera uma string hexadecimal aleatória.
  /// </summary>
  /// <param name="sizeToken">O tamanho da string hexadecimal a ser gerada. Valores menores que 2 são elevados para 2.</param>
  /// <returns>Uma string hexadecimal aleatória com exatamente o tamanho solicitado.</returns>
  internal static string Hexadecimal(int sizeToken = 128)
  {
    // Define o tamanho da string hexadecimal.
    int size = sizeToken > 2 ? sizeToken : 2;

    // Gera os bytes necessários, arredondando para cima porque cada byte vira dois caracteres.
    var hexadecimal = Convert.ToHexString(RandomNumberGenerator.GetBytes((size + 1) / 2));

    // Descarta o caractere excedente quando o tamanho solicitado é ímpar.
    return hexadecimal.Length > size ? hexadecimal[..size] : hexadecimal;
  }

  /// <summary>
  ///  Gera uma string Guid sem hífens.
  /// </summary>
  /// <returns>Uma string Guid sem hífens, com 32 caracteres.</returns>
  internal static string GuidCode() => Guid.NewGuid().ToString("N").ToUpperInvariant();

  /// <summary>
  /// Gera uma string de token.
  /// </summary>
  /// <param name="length">O comprimento da string de token a ser gerada. Valor padrão é 256. Deve ser maior ou igual a 256.</param>
  /// <returns>Uma string de token de no mínimo 256 caracteres.</returns>
  internal static string Token(int length = 256) => $"{GuidCode()}{Hexadecimal(length > 256 ? length - 32 : 224)}";

  #endregion

  #region Private Methods

  /// <summary>
  /// Embaralha o vetor no lugar com o algoritmo de Fisher-Yates, usando um gerador criptográfico.
  /// </summary>
  /// <remarks>
  /// Ordenar por uma chave aleatória não produz uma permutação uniforme: chaves repetidas mantêm a ordem
  /// original dos elementos empatados, o que preservaria a distribuição cíclica dos tipos de caractere.
  /// </remarks>
  /// <param name="values">O vetor a ser embaralhado.</param>
  private static void Shuffle(int[] values)
  {
    // Percorre o vetor do fim para o início.
    for (int i = values.Length - 1; i > 0; i--)
    {
      // Sorteia uma posição entre o início e a posição atual, inclusive.
      int position = RandomNumberGenerator.GetInt32(i + 1);

      // Troca os elementos de posição.
      (values[i], values[position]) = (values[position], values[i]);
    }
  }

  #endregion
}
