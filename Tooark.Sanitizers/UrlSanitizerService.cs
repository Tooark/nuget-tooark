using Microsoft.Extensions.Options;
using Tooark.Sanitizers.Interfaces;
using Tooark.Sanitizers.Options;
using Tooark.Sanitizers.Policies;

namespace Tooark.Sanitizers;

/// <summary>
/// Serviço que sanitiza URLs com lista de permissão de esquemas.
/// </summary>
/// <remarks>
/// Caracteres de controle e espaços saem de toda a URL antes da conferência. Uma URL absoluta precisa de um esquema
/// permitido; uma relativa, de <see cref="UrlOptions.AllowRelative"/>. <c>javascript:</c>, <c>vbscript:</c> e
/// <c>data:</c> são sempre recusados.
/// </remarks>
public sealed class UrlSanitizerService : IUrlSanitizerService
{
  #region Private Fields

  /// <summary>
  /// Regra de aceitação montada a partir das opções.
  /// </summary>
  private readonly UrlPolicy _policy;

  #endregion

  #region Constructor

  /// <summary>
  /// Cria o serviço a partir das opções dos sanitizadores.
  /// </summary>
  /// <param name="options">As opções dos sanitizadores.</param>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções de URL liberam um esquema inseguro.</exception>
  public UrlSanitizerService(IOptions<SanitizerOptions> options)
  {
    var url = options.Value.Url;
    url.Validate();

    // Sem esquemas na configuração, valem http e https
    var schemes = url.AllowedSchemes is { Length: > 0 } ? url.AllowedSchemes : UrlOptions.DefaultSchemes;

    _policy = new UrlPolicy(SchemeRules.Normalize(schemes), url.AllowRelative, url.AllowCredentials);
  }

  #endregion

  #region Methods

  /// <inheritdoc/>
  public string Sanitize(string? url) => _policy.Clean(url) ?? string.Empty;

  /// <inheritdoc/>
  public bool IsSafe(string? url) => _policy.Clean(url) is not null;

  #endregion
}
