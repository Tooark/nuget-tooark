using Tooark.Exceptions;
using Tooark.Securities;
using Tooark.Tests.Moq.Util;

namespace Tooark.Tests.Securities;

public class TotpTests
{
  // Segredo dos vetores de teste do RFC 4226 e do RFC 6238 ("12345678901234567890" em ASCII), em Base32
  private const string RfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

  // Testa os vetores do RFC 6238 (apêndice B, SHA1, 8 dígitos): o passo é T / 30
  [Theory]
  [InlineData(59L, "94287082")]
  [InlineData(1111111109L, "07081804")]
  [InlineData(1111111111L, "14050471")]
  [InlineData(1234567890L, "89005924")]
  [InlineData(2000000000L, "69279037")]
  [InlineData(20000000000L, "65353130")]
  public void Compute_ShouldMatchRfc6238Vectors_WithEightDigits(long unixSeconds, string expected)
  {
    // Arrange
    var step = Totp.GetTimeStep(DateTimeOffset.FromUnixTimeSeconds(unixSeconds));

    // Act
    var code = Totp.Compute(RfcSecret, step, digits: 8);

    // Assert
    Assert.Equal(expected, code);
  }

  // Testa os vetores do RFC 4226 (apêndice D, 6 dígitos) usando o passo como contador
  [Theory]
  [InlineData(0L, "755224")]
  [InlineData(1L, "287082")]
  [InlineData(2L, "359152")]
  [InlineData(3L, "969429")]
  [InlineData(4L, "338314")]
  [InlineData(5L, "254676")]
  [InlineData(6L, "287922")]
  [InlineData(7L, "162583")]
  [InlineData(8L, "399871")]
  [InlineData(9L, "520489")]
  public void Compute_ShouldMatchRfc4226Vectors_WithSixDigits(long step, string expected)
  {
    // Act
    var code = Totp.Compute(RfcSecret, step);

    // Assert
    Assert.Equal(expected, code);
  }

  // Testa se o segredo é aceito em minúsculas e com separadores de exibição, como o usuário o vê no aplicativo
  [Fact]
  public void Compute_ShouldAcceptSecret_WithLowercaseAndSeparators()
  {
    // Arrange
    var displayed = "gezd gnbv-gy3t qojq GEZD GNBV GY3T QOJQ";

    // Act
    var code = Totp.Compute(displayed, 1);

    // Assert
    Assert.Equal("287082", code);
  }

  // Testa se um segredo fora do alfabeto Base32 falha alto, em vez de recusar todos os códigos em silêncio
  [Theory]
  [InlineData("1890")]
  [InlineData("")]
  [InlineData("====")]
  public void Compute_ShouldThrowInternalServerError_WhenSecretIsNotBase32(string secret)
  {
    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => Totp.Compute(secret, 1));

    // Assert
    Assert.Equal("Totp.InvalidSecret", exception.Message);
  }

  // Testa se dígitos fora da faixa do RFC (6 a 8) e passo negativo são recusados
  [Theory]
  [InlineData(5, 1L)]
  [InlineData(9, 1L)]
  [InlineData(6, -1L)]
  public void Compute_ShouldThrowArgumentOutOfRange_WhenDigitsOrStepAreInvalid(int digits, long step)
  {
    // Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => Totp.Compute(RfcSecret, step, digits));
  }

  // Testa se o passo é o quociente dos segundos Unix pelo período
  [Theory]
  [InlineData(0L, 30, 0L)]
  [InlineData(29L, 30, 0L)]
  [InlineData(30L, 30, 1L)]
  [InlineData(59L, 30, 1L)]
  [InlineData(1111111111L, 30, 37037037L)]
  [InlineData(120L, 60, 2L)]
  public void GetTimeStep_ShouldDivideUnixSecondsByPeriod(long unixSeconds, int period, long expected)
  {
    // Act
    var step = Totp.GetTimeStep(DateTimeOffset.FromUnixTimeSeconds(unixSeconds), period);

    // Assert
    Assert.Equal(expected, step);
  }

  // Testa se o segredo gerado tem 20 bytes em Base32 (32 caracteres), é decodificável e difere a cada chamada
  [Fact]
  public void GenerateSecret_ShouldReturnBase32Secret_WithDefaultSize()
  {
    // Act
    var first = Totp.GenerateSecret();
    var second = Totp.GenerateSecret();

    // Assert
    Assert.Equal(32, first.Length);
    Assert.Matches("^[A-Z2-7]+$", first);
    Assert.NotEqual(first, second);
    Assert.Equal(6, Totp.Compute(first, 1).Length);
  }

  // Testa se um tamanho de segredo fora da faixa é recusado
  [Theory]
  [InlineData(15)]
  [InlineData(65)]
  public void GenerateSecret_ShouldThrowArgumentOutOfRange_WhenSizeIsOutOfRange(int sizeInBytes)
  {
    // Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => Totp.GenerateSecret(sizeInBytes));
  }

  // Testa se a URI otpauth sai com emissor e conta escapados e os parâmetros que os aplicativos esperam
  [Fact]
  public void BuildUri_ShouldReturnOtpauthUri_WithEscapedLabelAndParameters()
  {
    // Act
    var uri = Totp.BuildUri(RfcSecret, "Minha App", "ana@empresa.com");

    // Assert
    Assert.Equal(
      $"otpauth://totp/Minha%20App:ana%40empresa.com?secret={RfcSecret}&issuer=Minha%20App&algorithm=SHA1&digits=6&period=30",
      uri);
  }

  // Testa se a URI normaliza o segredo exibido com separadores e aceita dígitos e período diferentes
  [Fact]
  public void BuildUri_ShouldNormalizeSecret_AndApplyDigitsAndPeriod()
  {
    // Act
    var uri = Totp.BuildUri("gezd gnbv gy3t qojq gezd gnbv gy3t qojq", "App", "user", digits: 8, periodSeconds: 60);

    // Assert
    Assert.Contains($"secret={RfcSecret}&", uri);
    Assert.EndsWith("&algorithm=SHA1&digits=8&period=60", uri);
  }

  // Testa se emissor ou conta vazios são recusados na URI como erro de requisição
  [Theory]
  [InlineData(RfcSecret, "", "user", "Totp.IssuerNotProvided")]
  [InlineData(RfcSecret, null, "user", "Totp.IssuerNotProvided")]
  [InlineData(RfcSecret, "App", " ", "Totp.AccountNotProvided")]
  [InlineData(RfcSecret, "App", null, "Totp.AccountNotProvided")]
  public void BuildUri_ShouldThrowBadRequest_WhenIssuerOrAccountIsMissing(string secret, string? issuer, string? account, string expectedKey)
  {
    // Act
    var exception = Assert.Throws<BadRequestException>(() => Totp.BuildUri(secret, issuer!, account!));

    // Assert
    Assert.Equal(expectedKey, exception.Message);
  }

  // Testa se um segredo inválido na URI é erro interno: o segredo vem do serviço, não do usuário
  [Fact]
  public void BuildUri_ShouldThrowInternalServerError_WhenSecretIsNotBase32()
  {
    // Act
    var exception = Assert.Throws<InternalServerErrorException>(() => Totp.BuildUri("1890", "App", "user"));

    // Assert
    Assert.Equal("Totp.InvalidSecret", exception.Message);
  }

  // Testa se o código do passo atual é aceito e o passo é devolvido para ser gravado
  [Fact]
  public void Verify_ShouldReturnTrueAndMatchedStep_WhenCodeIsCurrent()
  {
    // Arrange
    var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));

    // Act
    var valid = Totp.Verify(RfcSecret, "287082", lastUsedStep: -1, out var matchedStep, timeProvider: clock);

    // Assert
    Assert.True(valid);
    Assert.Equal(1, matchedStep);
  }

  // Testa se o mesmo código é recusado quando o passo já foi usado (proteção contra reuso)
  [Fact]
  public void Verify_ShouldReturnFalse_WhenStepWasAlreadyUsed()
  {
    // Arrange
    var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));

    // Act
    var valid = Totp.Verify(RfcSecret, "287082", lastUsedStep: 1, out var matchedStep, timeProvider: clock);

    // Assert
    Assert.False(valid);
    Assert.Equal(Totp.NoStep, matchedStep);
  }

  // Testa se a janela padrão aceita o passo anterior e o seguinte, mas não dois passos de distância
  [Theory]
  [InlineData("755224", true, 0L)]
  [InlineData("359152", true, 2L)]
  [InlineData("969429", false, Totp.NoStep)]
  public void Verify_ShouldAcceptNeighbourSteps_WithinWindow(string code, bool expected, long expectedStep)
  {
    // Arrange: passo atual 1
    var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));

    // Act
    var valid = Totp.Verify(RfcSecret, code, lastUsedStep: -1, out var matchedStep, timeProvider: clock);

    // Assert
    Assert.Equal(expected, valid);
    Assert.Equal(expectedStep, matchedStep);
  }

  // Testa se a janela zero aceita só o passo atual
  [Fact]
  public void Verify_ShouldRejectNeighbourSteps_WhenWindowIsZero()
  {
    // Arrange: passo atual 1
    var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));

    // Act & Assert
    Assert.True(Totp.Verify(RfcSecret, "287082", -1, out _, window: 0, timeProvider: clock));
    Assert.False(Totp.Verify(RfcSecret, "755224", -1, out _, window: 0, timeProvider: clock));
    Assert.False(Totp.Verify(RfcSecret, "359152", -1, out _, window: 0, timeProvider: clock));
  }

  // Testa se um passo anterior ao último usado é recusado mesmo dentro da janela, e o seguinte é aceito
  [Fact]
  public void Verify_ShouldOnlyAcceptStepsAfterLastUsed()
  {
    // Arrange: passo atual 1, último usado 0
    var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));

    // Act & Assert
    Assert.False(Totp.Verify(RfcSecret, "755224", lastUsedStep: 0, out _, timeProvider: clock));
    Assert.True(Totp.Verify(RfcSecret, "359152", lastUsedStep: 0, out var matchedStep, timeProvider: clock));
    Assert.Equal(2, matchedStep);
  }

  // Testa se espaços de digitação são ignorados no código
  [Fact]
  public void Verify_ShouldIgnoreSpaces_InCode()
  {
    // Arrange
    var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));

    // Act & Assert
    Assert.True(Totp.Verify(RfcSecret, " 287 082 ", -1, out _, timeProvider: clock));
  }

  // Testa se código ausente, curto, longo ou com letras devolve falso sem lançar
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("28708")]
  [InlineData("2870821")]
  [InlineData("28708a")]
  [InlineData("287-082")]
  public void Verify_ShouldReturnFalse_WhenCodeIsMalformed(string? code)
  {
    // Arrange
    var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));

    // Act
    var valid = Totp.Verify(RfcSecret, code, -1, out var matchedStep, timeProvider: clock);

    // Assert
    Assert.False(valid);
    Assert.Equal(Totp.NoStep, matchedStep);
  }

  // Testa se o código com 8 dígitos é aceito quando os dígitos são configurados assim
  [Fact]
  public void Verify_ShouldAcceptEightDigitCode_WhenConfigured()
  {
    // Arrange
    var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));

    // Act & Assert
    Assert.True(Totp.Verify(RfcSecret, "94287082", -1, out _, digits: 8, timeProvider: clock));
    Assert.False(Totp.Verify(RfcSecret, "287082", -1, out _, digits: 8, timeProvider: clock));
  }

  // Testa se a janela fora da faixa e o segredo inválido são recusados na verificação
  [Fact]
  public void Verify_ShouldThrow_WhenWindowOrSecretAreInvalid()
  {
    // Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => Totp.Verify(RfcSecret, "287082", -1, out _, window: 11));
    Assert.Throws<ArgumentOutOfRangeException>(() => Totp.Verify(RfcSecret, "287082", -1, out _, window: -1));
    Assert.Equal("Totp.InvalidSecret", Assert.Throws<InternalServerErrorException>(() => Totp.Verify("1890", "287082", -1, out _)).Message);
  }

  // Testa o fluxo completo com um segredo gerado e o relógio avançando um passo entre os códigos
  [Fact]
  public void Verify_ShouldFollowClock_WithGeneratedSecret()
  {
    // Arrange
    var secret = Totp.GenerateSecret();
    var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
    var step = Totp.GetTimeStep(clock.GetUtcNow());
    var code = Totp.Compute(secret, step);

    // Act
    var first = Totp.Verify(secret, code, -1, out var firstStep, timeProvider: clock);
    clock.Advance(TimeSpan.FromSeconds(30));
    var replay = Totp.Verify(secret, code, firstStep, out _, timeProvider: clock);
    var next = Totp.Verify(secret, Totp.Compute(secret, step + 1), firstStep, out var nextStep, timeProvider: clock);

    // Assert
    Assert.True(first);
    Assert.Equal(step, firstStep);
    Assert.False(replay);
    Assert.True(next);
    Assert.Equal(step + 1, nextStep);
  }
}
