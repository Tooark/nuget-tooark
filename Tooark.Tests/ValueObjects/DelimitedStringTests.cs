using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

public class DelimitedStringTests
{
  private static readonly string[] arrayValues = ["value1", "value2", "value3"];
  private static readonly List<string> listValues = ["value1", "value2", "value3"];
  private static readonly string stringValues = "value1;value2;value3";

  // Teste se o valor é uma string delimitada válida quando passado como string
  [Fact]
  public void DelimitedString_WithValidString_ShouldInitializeCorrectly()
  {
    // Arrange
    var input = stringValues;

    // Act
    var delimitedString = new DelimitedString(input);

    // Assert
    Assert.True(delimitedString.IsValid);
    Assert.Equal(stringValues, delimitedString.Value);
    Assert.Equal(arrayValues, delimitedString.Values);
    Assert.Equal(listValues, delimitedString.Values);
  }

  // Teste se o valor é uma string delimitada válida quando passado como uma array de string
  [Fact]
  public void DelimitedString_WithValidArrayString_ShouldInitializeCorrectly()
  {
    // Arrange
    var input = arrayValues;

    // Act
    var delimitedString = new DelimitedString(input);

    // Assert
    Assert.True(delimitedString.IsValid);
    Assert.Equal(stringValues, delimitedString.Value);
    Assert.Equal(arrayValues, delimitedString.Values);
    Assert.Equal(listValues, delimitedString.Values);
  }

  // Teste se o valor é uma string delimitada inválida quando passado como string vazia
  [Fact]
  public void DelimitedString_WithEmptyString_ShouldBeInvalid()
  {
    // Arrange
    var input = "";

    // Act
    var delimitedString = new DelimitedString(input);

    // Assert
    Assert.False(delimitedString.IsValid);
    Assert.Equal("", delimitedString.Value);
    Assert.Equal([], delimitedString.Values);
  }

  // Teste se o valor é uma string delimitada inválida quando passado como uma lista de string vazia
  [Fact]
  public void DelimitedString_WithEmptyListString_ShouldBeInvalid()
  {
    // Arrange
    var input = Array.Empty<string>();

    // Act
    var delimitedString = new DelimitedString(input);

    // Assert
    Assert.False(delimitedString.IsValid);
    Assert.Equal("", delimitedString.Value);
    Assert.Equal([], delimitedString.Values);
  }

  // Teste se o método ToString retorna uma string delimitada
  [Fact]
  public void DelimitedString_ShouldReturnCorrectStringRepresentation()
  {
    // Arrange
    var input = arrayValues;
    var delimitedString = new DelimitedString(input);

    // Act
    var result = delimitedString.ToString();

    // Assert
    Assert.Equal(stringValues, result);
  }

  // Teste se o método ToArray retorna uma array de string
  [Fact]
  public void DelimitedString_ShouldReturnCorrectArrayRepresentation()
  {
    // Arrange
    var input = stringValues;
    var delimitedString = new DelimitedString(input);

    // Act
    var result = delimitedString.ToArray();

    // Assert
    Assert.Equal(arrayValues, result);
  }

  // Teste se o método ToList retorna uma lista de string
  [Fact]
  public void DelimitedString_ShouldReturnCorrectListRepresentation()
  {
    // Arrange
    var input = stringValues;
    var delimitedString = new DelimitedString(input);

    // Act
    var result = delimitedString.ToList();

    // Assert
    Assert.Equal(listValues, result);
  }

  // Teste se a string delimitada está sendo convertida para string implicitamente
  [Fact]
  public void DelimitedString_ShouldConvertToStringImplicitly()
  {
    // Arrange
    var input = arrayValues;
    var delimitedString = new DelimitedString(input);

    // Act
    string result = delimitedString;

    // Assert
    Assert.Equal(stringValues, result);
  }

  // Teste se a string delimitada está sendo convertida para array de string implicitamente
  [Fact]
  public void DelimitedString_ShouldConvertToArrayImplicitly()
  {
    // Arrange
    var input = stringValues;
    var delimitedString = new DelimitedString(input);

    // Act
    string[] result = delimitedString;

    // Assert
    Assert.Equal(arrayValues, result);
  }

  // Teste se a string delimitada está sendo convertida para lista de string implicitamente
  [Fact]
  public void DelimitedString_ShouldConvertToListImplicitly()
  {
    // Arrange
    var input = stringValues;
    var delimitedString = new DelimitedString(input);

    // Act
    List<string> result = delimitedString;

    // Assert
    Assert.Equal(listValues, result);
  }

  // Teste se a string delimitada está sendo convertida de string implicitamente
  [Fact]
  public void DelimitedString_ShouldConvertFromStringImplicitly()
  {
    // Arrange
    var input = stringValues;

    // Act
    DelimitedString result = input;

    // Assert
    Assert.Equal(stringValues, result.Value);
    Assert.Equal(arrayValues, result.Values);
    Assert.Equal(listValues, result.Values);
  }

  // Teste se a string delimitada está sendo convertida de array de string implicitamente
  [Fact]
  public void DelimitedString_ShouldConvertFromArrayImplicitly()
  {
    // Arrange
    var input = arrayValues;

    // Act
    DelimitedString result = input;

    // Assert
    Assert.Equal(stringValues, result.Value);
    Assert.Equal(arrayValues, result.Values);
    Assert.Equal(listValues, result.Values);
  }

  // Teste se a string delimitada está sendo convertida de lista de string implicitamente
  [Fact]
  public void DelimitedString_ShouldConvertFromListImplicitly()
  {
    // Arrange
    var input = listValues;

    // Act
    DelimitedString result = input;

    // Assert
    Assert.Equal(stringValues, result.Value);
    Assert.Equal(arrayValues, result.Values);
    Assert.Equal(listValues, result.Values);
  }

  // Teste se a constante pública do delimitador padrão está disponível para consulta externa com o valor esperado
  [Fact]
  public void DelimitedString_DefaultDelimiter_ShouldBePubliclyConsultable()
  {
    // Assert
    Assert.Equal(';', DelimitedString.DefaultDelimiter);
  }
}
