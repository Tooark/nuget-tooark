using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Tooark.Exceptions;

namespace Tooark.Securities;

/// <summary>
/// Senha de uso único baseada em tempo (TOTP, RFC 6238), o segundo fator dos aplicativos autenticadores.
/// </summary>
/// <remarks>
/// Os padrões são os que o Google Authenticator, o Microsoft Authenticator e similares aceitam: HMAC-SHA1,
/// 6 dígitos e passos de 30 segundos. O segredo é gerado em Base32 e deve ser guardado cifrado
/// (<see cref="Interfaces.ICryptographyService"/>), porque quem o lê gera códigos válidos.
/// A verificação aceita o passo atual e os vizinhos dentro da janela, para tolerar relógio defasado, e recusa
/// qualquer passo já usado: grave o <c>matchedStep</c> devolvido por <see cref="Verify"/> e informe-o na próxima
/// verificação, para que o mesmo código não valha duas vezes.
/// </remarks>
public static class Totp
{
  #region Constants

  /// <summary>
  /// Quantidade padrão de dígitos do código.
  /// </summary>
  public const int DefaultDigits = 6;

  /// <summary>
  /// Duração padrão do passo em segundos.
  /// </summary>
  public const int DefaultPeriodSeconds = 30;

  /// <summary>
  /// Janela padrão de tolerância: um passo para cada lado do atual.
  /// </summary>
  public const int DefaultWindow = 1;

  /// <summary>
  /// Tamanho padrão do segredo em bytes (160 bits, o tamanho do HMAC-SHA1).
  /// </summary>
  public const int DefaultSecretSize = 20;

  /// <summary>
  /// Valor de <c>matchedStep</c> quando nenhum passo confere.
  /// </summary>
  public const long NoStep = -1;

  #endregion

  #region Methods

  /// <summary>
  /// Gera um segredo aleatório em Base32, a chave compartilhada com o aplicativo autenticador.
  /// </summary>
  /// <param name="sizeInBytes">Quantidade de bytes aleatórios, entre 16 e 64. Padrão: 20.</param>
  /// <returns>Segredo em Base32 sem preenchimento (32 caracteres para 20 bytes).</returns>
  /// <exception cref="ArgumentOutOfRangeException">Quando o tamanho está fora da faixa aceita.</exception>
  public static string GenerateSecret(int sizeInBytes = DefaultSecretSize)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(sizeInBytes, 16);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(sizeInBytes, 64);

    return Base32.Encode(RandomNumberGenerator.GetBytes(sizeInBytes));
  }

  /// <summary>
  /// Monta a URI <c>otpauth://totp/...</c> que vira o QR code lido pelo aplicativo autenticador.
  /// </summary>
  /// <param name="secret">Segredo em Base32, de <see cref="GenerateSecret"/>.</param>
  /// <param name="issuer">Nome da aplicação, exibido no aplicativo.</param>
  /// <param name="account">Identificação da conta, como o e-mail do usuário.</param>
  /// <param name="digits">Dígitos do código: 6, 7 ou 8. Padrão: 6.</param>
  /// <param name="periodSeconds">Duração do passo em segundos. Padrão: 30.</param>
  /// <returns>A URI, com emissor e conta escapados.</returns>
  /// <exception cref="BadRequestException">Quando o emissor ou a conta estão vazios.</exception>
  /// <exception cref="InternalServerErrorException">Quando o segredo não é um Base32 válido.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Quando os dígitos ou o período estão fora da faixa.</exception>
  /// <remarks>
  /// Nem todo aplicativo honra <c>digits</c> e <c>period</c> diferentes do padrão; alguns ignoram e usam 6 e 30.
  /// </remarks>
  public static string BuildUri(
    string secret,
    string issuer,
    string account,
    int digits = DefaultDigits,
    int periodSeconds = DefaultPeriodSeconds
  )
  {
    // Valida o emissor, exibido no aplicativo autenticador
    if (string.IsNullOrWhiteSpace(issuer))
    {
      throw new BadRequestException("Totp.IssuerNotProvided");
    }

    // Valida a conta, que identifica o usuário no aplicativo autenticador
    if (string.IsNullOrWhiteSpace(account))
    {
      throw new BadRequestException("Totp.AccountNotProvided");
    }

    ValidateDigits(digits);
    ValidatePeriod(periodSeconds);

    // O segredo entra normalizado: maiúsculas, sem separadores nem preenchimento
    var normalizedSecret = Base32.Encode(DecodeSecret(secret));
    var label = $"{Uri.EscapeDataString(issuer.Trim())}:{Uri.EscapeDataString(account.Trim())}";

    return $"otpauth://totp/{label}?secret={normalizedSecret}&issuer={Uri.EscapeDataString(issuer.Trim())}" +
      $"&algorithm=SHA1&digits={digits}&period={periodSeconds}";
  }

  /// <summary>
  /// Calcula o passo de tempo de um instante: segundos desde a época Unix divididos pelo período.
  /// </summary>
  /// <param name="at">Instante de referência.</param>
  /// <param name="periodSeconds">Duração do passo em segundos. Padrão: 30.</param>
  /// <returns>O número do passo.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Quando o período está fora da faixa.</exception>
  public static long GetTimeStep(DateTimeOffset at, int periodSeconds = DefaultPeriodSeconds)
  {
    ValidatePeriod(periodSeconds);

    return at.ToUnixTimeSeconds() / periodSeconds;
  }

  /// <summary>
  /// Calcula o código de um passo de tempo (RFC 4226 sobre o contador do RFC 6238).
  /// </summary>
  /// <param name="secret">Segredo em Base32.</param>
  /// <param name="timeStep">Número do passo, de <see cref="GetTimeStep"/>.</param>
  /// <param name="digits">Dígitos do código: 6, 7 ou 8. Padrão: 6.</param>
  /// <returns>O código com zeros à esquerda.</returns>
  /// <exception cref="InternalServerErrorException">Quando o segredo não é um Base32 válido.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Quando os dígitos estão fora da faixa ou o passo é negativo.</exception>
  public static string Compute(string secret, long timeStep, int digits = DefaultDigits)
  {
    ValidateDigits(digits);

    ArgumentOutOfRangeException.ThrowIfNegative(timeStep);

    return Compute(DecodeSecret(secret), timeStep, digits);
  }

  /// <summary>
  /// Confere um código digitado contra o passo atual e os vizinhos da janela, recusando passos já usados.
  /// </summary>
  /// <param name="secret">Segredo em Base32 guardado para o usuário.</param>
  /// <param name="code">Código digitado. Espaços são ignorados.</param>
  /// <param name="lastUsedStep">Último passo aceito para o usuário, ou um valor negativo quando nenhum código foi aceito ainda.</param>
  /// <param name="matchedStep">O passo que conferiu, a ser gravado como último usado; <see cref="NoStep"/> quando nenhum conferiu.</param>
  /// <param name="window">Passos aceitos para cada lado do atual, entre 0 e 10. Padrão: 1 (30 segundos de tolerância).</param>
  /// <param name="digits">Dígitos do código: 6, 7 ou 8. Padrão: 6.</param>
  /// <param name="periodSeconds">Duração do passo em segundos. Padrão: 30.</param>
  /// <param name="timeProvider">Relógio usado para obter o passo atual. Padrão: o relógio do sistema.</param>
  /// <returns>Verdadeiro quando o código confere em um passo ainda não usado.</returns>
  /// <exception cref="InternalServerErrorException">Quando o segredo não é um Base32 válido.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Quando a janela, os dígitos ou o período estão fora da faixa.</exception>
  /// <remarks>
  /// Um código ausente, com tamanho diferente ou com caracteres que não são dígitos devolve falso sem lançar.
  /// Todos os passos da janela são calculados e comparados em tempo constante, mesmo depois de um acerto.
  /// </remarks>
  public static bool Verify(
    string secret,
    string? code,
    long lastUsedStep,
    out long matchedStep,
    int window = DefaultWindow,
    int digits = DefaultDigits,
    int periodSeconds = DefaultPeriodSeconds,
    TimeProvider? timeProvider = null
  )
  {
    ArgumentOutOfRangeException.ThrowIfNegative(window);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(window, 10);

    ValidateDigits(digits);
    ValidatePeriod(periodSeconds);

    var key = DecodeSecret(secret);
    matchedStep = NoStep;

    // O código precisa ter exatamente a quantidade de dígitos; qualquer outra coisa não confere
    var normalizedCode = NormalizeCode(code);

    if (normalizedCode.Length != digits)
    {
      return false;
    }

    var currentStep = GetTimeStep((timeProvider ?? TimeProvider.System).GetUtcNow(), periodSeconds);
    var expectedBytes = Encoding.ASCII.GetBytes(normalizedCode);
    var matched = false;

    // Percorre toda a janela sem sair no primeiro acerto, para não revelar o passo pelo tempo de resposta
    for (var step = currentStep - window; step <= currentStep + window; step++)
    {
      // Passos já usados e anteriores à época não contam
      if (step < 0 || step <= lastUsedStep)
      {
        continue;
      }

      var candidate = Encoding.ASCII.GetBytes(Compute(key, step, digits));

      if (CryptographicOperations.FixedTimeEquals(candidate, expectedBytes) && !matched)
      {
        matched = true;
        matchedStep = step;
      }
    }

    return matched;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Calcula o código HOTP de um contador com a chave já decodificada.
  /// </summary>
  /// <param name="key">Chave do HMAC.</param>
  /// <param name="counter">Contador (passo de tempo).</param>
  /// <param name="digits">Dígitos do código.</param>
  /// <returns>O código com zeros à esquerda.</returns>
  private static string Compute(byte[] key, long counter, int digits)
  {
    // O contador entra como 8 bytes em big-endian
    Span<byte> message = stackalloc byte[8];
    BinaryPrimitives.WriteInt64BigEndian(message, counter);

    Span<byte> hash = stackalloc byte[20];
    HMACSHA1.HashData(key, message, hash);

    // Truncagem dinâmica (RFC 4226, seção 5.3): o nibble baixo do último byte escolhe 4 bytes do hash
    var offset = hash[^1] & 0x0F;
    var binary = ((hash[offset] & 0x7F) << 24)
      | ((hash[offset + 1] & 0xFF) << 16)
      | ((hash[offset + 2] & 0xFF) << 8)
      | (hash[offset + 3] & 0xFF);

    var modulo = (int)Math.Pow(10, digits);

    return (binary % modulo).ToString().PadLeft(digits, '0');
  }

  /// <summary>
  /// Decodifica o segredo em Base32 ou falha com a chave de mensagem <c>Totp.InvalidSecret</c>.
  /// </summary>
  /// <param name="secret">Segredo em Base32.</param>
  /// <returns>Os bytes da chave.</returns>
  /// <exception cref="InternalServerErrorException">Quando o segredo não é um Base32 válido.</exception>
  private static byte[] DecodeSecret(string secret)
  {
    // Um segredo corrompido impede qualquer verificação: falha alto em vez de recusar todo código em silêncio.
    // É erro interno (500), não de requisição: o segredo vem do banco ou do serviço, nunca do usuário
    if (!Base32.TryDecode(secret, out var key))
    {
      throw new InternalServerErrorException("Totp.InvalidSecret");
    }

    return key;
  }

  /// <summary>
  /// Remove os espaços do código digitado e garante que só restem dígitos.
  /// </summary>
  /// <param name="code">Código digitado.</param>
  /// <returns>Os dígitos do código, ou vazio quando há outro caractere.</returns>
  private static string NormalizeCode(string? code)
  {
    // Sem código não há o que conferir
    if (string.IsNullOrWhiteSpace(code))
    {
      return string.Empty;
    }

    var builder = new StringBuilder(code.Length);

    foreach (var character in code)
    {
      // Espaços de digitação saem; qualquer coisa que não seja dígito invalida o código
      if (character == ' ')
      {
        continue;
      }

      if (!char.IsAsciiDigit(character))
      {
        return string.Empty;
      }

      builder.Append(character);
    }

    return builder.ToString();
  }

  /// <summary>
  /// Confere a faixa de dígitos aceita pelo RFC 6238.
  /// </summary>
  /// <param name="digits">Dígitos do código.</param>
  private static void ValidateDigits(int digits)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(digits, 6);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(digits, 8);
  }

  /// <summary>
  /// Confere a faixa de período aceita.
  /// </summary>
  /// <param name="periodSeconds">Duração do passo em segundos.</param>
  private static void ValidatePeriod(int periodSeconds)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(periodSeconds, 1);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(periodSeconds, 300);
  }

  #endregion
}
