using Tooark.Utils;

namespace Tooark.Tests.Utils;

public class GenerateStringTests
{
  // Testa se String retorna a string esperada
  [Theory]
  [InlineData(0, "")]
  [InlineData(1, "A")]
  [InlineData(2, "B")]
  [InlineData(26, "Z")]
  [InlineData(27, "AA")]
  [InlineData(28, "AB")]
  [InlineData(52, "AZ")]
  [InlineData(53, "BA")]
  [InlineData(54, "BB")]
  [InlineData(78, "BZ")]
  [InlineData(703, "AAA")]
  [InlineData(704, "AAB")]
  [InlineData(728, "AAZ")]
  [InlineData(729, "ABA")]
  [InlineData(730, "ABB")]
  [InlineData(754, "ABZ")]
  public void String_ShouldReturnExpectedString(int number, string expected)
  {
    // Arrange & Act
    var result = GenerateString.Sequential(number);

    // Assert
    Assert.Equal(expected, result);
  }

  // Testa se Password gera uma string com o comprimento correto
  [Theory]
  [InlineData(8)]
  [InlineData(12)]
  [InlineData(16)]
  [InlineData(20)]
  public void Password_ShouldReturnStringWithCorrectLength(int expected)
  {
    // Arrange & Act
    var result = GenerateString.Password(expected);

    // Assert
    Assert.Equal(expected, result.Length);
  }

  // Testa se Password gera uma string com o comprimento mínimo
  [Theory]
  [InlineData(0)]
  [InlineData(2)]
  [InlineData(4)]
  [InlineData(6)]
  public void Password_ShouldReturnStringWithMinLength(int expected)
  {
    // Arrange & Act
    var result = GenerateString.Password(expected);

    // Assert
    Assert.Equal(8, result.Length);
  }

  // Testa se Password aplica os critérios padrão quando todos os tipos de caracteres estão desativados
  [Fact]
  public void Password_ShouldReturnString_WhenAllCharacterTypesDisabled()
  {
    // Arrange & Act
    var result = GenerateString.Password(12, false, false, false, false);

    // Assert
    Assert.Equal(12, result.Length);
  }

  // Testa se Hexadecimal gera uma string hexadecimal com o comprimento correto
  [Theory]
  [InlineData(16)]
  [InlineData(32)]
  [InlineData(64)]
  public void Hexadecimal_ShouldReturnStringWithCorrectLength(int expected)
  {
    // Arrange & Act
    var result = GenerateString.Hexadecimal(expected);

    // Assert
    Assert.Equal(expected, result.Length);
  }

  // Testa se Hexadecimal gera uma string hexadecimal com o comprimento mínimo
  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(2)]
  public void Hexadecimal_ShouldReturnStringWithMinLength(int expected)
  {
    // Arrange & Act
    var result = GenerateString.Hexadecimal(expected);

    // Assert
    Assert.Equal(2, result.Length);
  }

  // Testa se GuidCode gera uma string com o comprimento correto
  [Fact]
  public void GuidCode_ShouldReturnStringWithCorrectLength()
  {
    // Arrange & Act
    var result = GenerateString.GuidCode();

    // Assert
    Assert.Equal(32, result.Length);
  }

  // Testa se Token gera uma string com o comprimento correto
  [Theory]
  [InlineData(260)]
  [InlineData(280)]
  [InlineData(300)]
  public void Token_ShouldReturnStringWithCorrectLength(int expected)
  {
    // Arrange & Act
    var result = GenerateString.Token(expected);

    // Assert
    Assert.Equal(expected, result.Length);
  }

  // Testa se Token gera uma string com o comprimento mínimo
  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(2)]
  public void Token_ShouldReturnStringWithMinLength(int expected)
  {
    // Arrange & Act
    var result = GenerateString.Token(expected);

    // Assert
    Assert.Equal(256, result.Length);
  }

  // Testa se Hexadecimal gera uma string com o comprimento exato quando o tamanho é ímpar
  [Theory]
  [InlineData(3)]
  [InlineData(7)]
  [InlineData(15)]
  [InlineData(127)]
  public void Hexadecimal_ShouldReturnStringWithExactLength_WhenSizeIsOdd(int expected)
  {
    // Arrange & Act
    var result = GenerateString.Hexadecimal(expected);

    // Assert
    Assert.Equal(expected, result.Length);
  }

  // Testa se Hexadecimal gera apenas caracteres hexadecimais
  [Fact]
  public void Hexadecimal_ShouldReturnOnlyHexadecimalCharacters()
  {
    // Arrange & Act
    var result = GenerateString.Hexadecimal(64);

    // Assert
    Assert.All(result, character => Assert.Contains(character, "0123456789ABCDEF"));
  }

  // Testa se Password contém ao menos um caractere de cada tipo ativado
  [Fact]
  public void Password_ShouldContainEveryEnabledCharacterType()
  {
    // Arrange & Act
    var result = GenerateString.Password(16);

    // Assert
    Assert.Contains(result, char.IsUpper);
    Assert.Contains(result, char.IsLower);
    Assert.Contains(result, char.IsDigit);
    Assert.Contains(result, character => "@#$%&*_".Contains(character));
  }

  // Testa se Password respeita os tipos de caractere desativados
  [Fact]
  public void Password_ShouldNotContainDisabledCharacterTypes()
  {
    // Arrange & Act
    var result = GenerateString.Password(16, upper: true, lower: false, number: false, special: false);

    // Assert
    Assert.All(result, character => Assert.True(char.IsUpper(character)));
  }

  // Testa se Password não repete a mesma sequência de tipos de caractere
  [Fact]
  public void Password_ShouldNotRepeatTypeSequence()
  {
    // Arrange
    var sequences = new HashSet<string>();

    // Act
    for (int i = 0; i < 200; i++)
    {
      // Mapeia cada posição para o tipo de caractere gerado
      sequences.Add(new string([.. GenerateString.Password(12).Select(TypeOf)]));
    }

    // Assert
    Assert.True(sequences.Count > 1, "O embaralhamento dos tipos de caractere não variou entre as gerações.");
  }

  // Retorna o tipo de caractere de uma posição da senha.
  private static char TypeOf(char character) =>
    char.IsUpper(character) ? 'U' :
    char.IsLower(character) ? 'L' :
    char.IsDigit(character) ? 'N' : 'S';
}
