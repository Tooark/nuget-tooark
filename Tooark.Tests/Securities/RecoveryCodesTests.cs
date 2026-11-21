using Tooark.Securities;

namespace Tooark.Tests.Securities;

public class RecoveryCodesTests
{
  // Testa se os códigos padrão saem em quatro grupos de cinco, no alfabeto sem caracteres ambíguos, e distintos
  [Fact]
  public void Generate_ShouldReturnTenDistinctFormattedCodes_ByDefault()
  {
    // Act
    var codes = RecoveryCodes.Generate();

    // Assert
    Assert.Equal(10, codes.Count);
    Assert.Equal(10, codes.Distinct().Count());
    Assert.All(codes, code => Assert.Matches("^[A-HJ-NP-Z2-9]{5}(-[A-HJ-NP-Z2-9]{5}){3}$", code));
  }

  // Testa se o tamanho padrão é o de 100 bits: 20 caracteres de 5 bits
  [Fact]
  public void DefaultLength_ShouldBeTwentyCharacters()
  {
    // Act
    var code = Assert.Single(RecoveryCodes.Generate(count: 1));

    // Assert
    Assert.Equal(20, RecoveryCodes.DefaultLength);
    Assert.Equal(RecoveryCodes.DefaultLength, RecoveryCodes.Normalize(code).Length);
  }

  // Testa se um código de 10 caracteres, o padrão até a v4.10.0, continua conferindo depois da troca do padrão
  [Fact]
  public void Verify_ShouldStillAcceptACodeWithTheFormerDefaultLength()
  {
    // Arrange
    var code = Assert.Single(RecoveryCodes.Generate(count: 1, length: 10));
    var hash = SecureToken.Hash(RecoveryCodes.Normalize(code));

    // Act
    var valid = SecureToken.Verify(RecoveryCodes.Normalize(code.ToLowerInvariant()), hash);

    // Assert
    Assert.Matches("^[A-HJ-NP-Z2-9]{5}-[A-HJ-NP-Z2-9]{5}$", code);
    Assert.True(valid);
  }

  // Testa se a quantidade e o tamanho informados são respeitados, com o último grupo menor quando não divide por cinco
  [Fact]
  public void Generate_ShouldRespectCountAndLength()
  {
    // Act
    var codes = RecoveryCodes.Generate(count: 3, length: 8);

    // Assert
    Assert.Equal(3, codes.Count);
    Assert.All(codes, code => Assert.Matches("^[A-HJ-NP-Z2-9]{5}-[A-HJ-NP-Z2-9]{3}$", code));
  }

  // Testa se quantidade ou tamanho fora da faixa são recusados
  [Theory]
  [InlineData(0, 10)]
  [InlineData(51, 10)]
  [InlineData(10, 7)]
  [InlineData(10, 33)]
  public void Generate_ShouldThrowArgumentOutOfRange_WhenCountOrLengthIsOutOfRange(int count, int length)
  {
    // Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryCodes.Generate(count, length));
  }

  // Testa se a normalização tira hífens e espaços e sobe para maiúsculas, para o hash e a comparação
  [Theory]
  [InlineData("abcde-fghjk", "ABCDEFGHJK")]
  [InlineData(" ab cde fgh jk ", "ABCDEFGHJK")]
  [InlineData("ABCDE-FGHJK", "ABCDEFGHJK")]
  [InlineData(null, "")]
  [InlineData("  ", "")]
  public void Normalize_ShouldRemoveSeparatorsAndUppercase(string? code, string expected)
  {
    // Act
    var normalized = RecoveryCodes.Normalize(code);

    // Assert
    Assert.Equal(expected, normalized);
  }

  // Testa o fluxo documentado: guardar o hash do código normalizado e conferir o que o usuário digitou
  [Fact]
  public void Generate_ShouldVerifyThroughSecureToken_AfterNormalization()
  {
    // Arrange
    var code = RecoveryCodes.Generate(count: 1)[0];
    var stored = SecureToken.Hash(RecoveryCodes.Normalize(code));
    var typed = code.ToLowerInvariant().Replace('-', ' ');

    // Act & Assert
    Assert.True(SecureToken.Verify(RecoveryCodes.Normalize(typed), stored));
    Assert.False(SecureToken.Verify(RecoveryCodes.Normalize(RecoveryCodes.Generate(count: 1)[0]), stored));
  }
}
