using System.Security.Claims;
using Tooark.Exceptions;
using Tooark.Securities.Dtos;

namespace Tooark.Securities.Interfaces;

/// <summary>
/// Interface para o serviço de manipulação de tokens JWT.
/// </summary>
public interface IJwtTokenService
{
  /// <summary>
  /// Cria um token JWT.
  /// </summary>
  /// <param name="data">Dados para incluir no token.</param>
  /// <param name="audience">Destinatário do token. Parâmetro opcional que sobrescreve o destinatário padrão do Options.</param>
  /// <param name="extraClaims">Claims adicionais para incluir no token. Parâmetro opcional para incluir claims extras no token.</param>
  /// <returns>Token JWT.</returns>
  /// <exception cref="InternalServerErrorException">Quando a criação do token não está configurada.</exception>
  string Create(JwtTokenDto data, string? audience = null, IEnumerable<Claim>? extraClaims = null);

  /// <summary>
  /// Valida um token JWT de forma assíncrona.
  /// </summary>
  /// <remarks>
  /// Caminho preferencial: o handler da Microsoft.IdentityModel expõe a validação apenas de forma assíncrona,
  /// então esta sobrecarga chega ao handler sem intermediários. Com chave de assinatura estática — o caso deste
  /// serviço — a validação é CPU-bound e a tarefa devolvida já vem concluída.
  /// A operação não aceita cancelamento porque o handler não oferece esse ponto de extensão nesta chamada.
  /// </remarks>
  /// <param name="token">Token JWT a ser validado.</param>
  /// <param name="audience">Destinatário do token. Parâmetro opcional que sobrescreve o destinatário padrão do Options.</param>
  /// <returns>Resultado da validação do token.</returns>
  /// <exception cref="InternalServerErrorException">Quando a validação do token não está configurada.</exception>
  Task<UserTokenDto> ValidateAsync(string token, string? audience = null);

  /// <summary>
  /// Valida um token JWT.
  /// </summary>
  /// <remarks>
  /// Encapsula <see cref="ValidateAsync(string, string?)"/> para os chamadores síncronos. Em fluxos que já são
  /// assíncronos, prefira <see cref="ValidateAsync(string, string?)"/>: a espera bloqueante ocupa a thread até
  /// a validação concluir.
  /// </remarks>
  /// <param name="token">Token JWT a ser validado.</param>
  /// <param name="audience">Destinatário do token. Parâmetro opcional que sobrescreve o destinatário padrão do Options.</param>
  /// <returns>Resultado da validação do token.</returns>
  /// <exception cref="InternalServerErrorException">Quando a validação do token não está configurada.</exception>
  UserTokenDto Validate(string token, string? audience = null);
}
