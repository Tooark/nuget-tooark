using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Exceptions;
using Tooark.Securities.Options;

namespace Tooark.Securities.Injections;

/// <summary>
/// Classe para adicionar o esquema de autenticação dos tokens JWT emitidos pelo próprio serviço.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o esquema de autenticação <c>Bearer</c> que valida os tokens emitidos por
  /// <see cref="JwtTokenService"/>, lendo a mesma seção <c>Jwt</c> e aplicando os mesmos parâmetros de validação.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Evita repetir chave, emissor e destinatário em um <c>AddJwtBearer</c> escrito à mão. A chave pública (ou o
  /// segredo HMAC) é carregada e validada aqui, no startup. <c>MapInboundClaims</c> fica desligado, então as claims
  /// chegam ao <c>ClaimsPrincipal</c> com os nomes do token (<c>id</c>, <c>sub</c>, <c>login</c>), e não com os
  /// URIs do <c>ClaimTypes</c>.
  /// </para>
  /// <para>
  /// O esquema <c>Bearer</c> vira o esquema padrão de autenticação, a menos que a aplicação já tenha definido um.
  /// A aplicação ainda precisa de <c>app.UseAuthentication()</c> e <c>app.UseAuthorization()</c>.
  /// </para>
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para ajustar o handler <c>JwtBearer</c>, executada por último.</param>
  /// <returns>A coleção de serviços com o esquema de autenticação adicionado.</returns>
  /// <exception cref="InternalServerErrorException">Quando as opções do JWT são inválidas ou não há chave de validação.</exception>
  public static IServiceCollection AddTooarkJwtBearer(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<JwtBearerOptions>? configure = null
  )
  {
    var section = configuration.GetSection(JwtOptions.Section);

    // Registra as opções do JWT a partir da configuração, como AddTooarkJwtToken
    services.Configure<JwtOptions>(section);

    // Carrega e valida as chaves no startup, e não na primeira requisição
    var options = new JwtOptions();
    StrictConfiguration.Bind(configuration, JwtOptions.Section, options);

    var keys = JwtSigningKeys.Load(options);

    // Sem chave de validação não há como aceitar token nenhum
    if (!keys.CanValidate)
    {
      throw new InternalServerErrorException("Options.Jwt.KeyNotConfigured;PublicKey");
    }

    var parameters = JwtValidation.BuildParameters(options, keys.ValidationKey!);
    var builder = services.AddAuthentication();

    // Esquema padrão: só quando a aplicação ainda não definiu
    services.Configure<AuthenticationOptions>(authentication =>
    {
      authentication.DefaultScheme ??= JwtBearerDefaults.AuthenticationScheme;
    });

    // Handler com os mesmos parâmetros do JwtTokenService; o consumidor ajusta por último
    builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, bearer =>
    {
      bearer.TokenValidationParameters = parameters;
      bearer.MapInboundClaims = false;

      configure?.Invoke(bearer);
    });

    return services;
  }
}
