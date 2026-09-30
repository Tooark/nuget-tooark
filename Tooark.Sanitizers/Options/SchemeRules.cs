using System.Text.RegularExpressions;
using Tooark.Exceptions;

namespace Tooark.Sanitizers.Options;

/// <summary>
/// Regras comuns aos esquemas de URL liberados nas opções.
/// </summary>
internal static class SchemeRules
{
  #region Constants

  /// <summary>
  /// Esquemas que executam código ou embutem conteúdo, e que nenhuma configuração pode liberar.
  /// </summary>
  internal static readonly IReadOnlySet<string> Blocked =
    new HashSet<string>(["javascript", "vbscript", "data"], StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Sintaxe de um esquema de URL: letra seguida de letras, dígitos, <c>+</c>, <c>-</c> ou <c>.</c>.
  /// </summary>
  private static readonly Regex Syntax =
    new("^[a-z][a-z0-9+.-]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

  #endregion

  #region Methods

  /// <summary>
  /// Valida os esquemas informados na configuração.
  /// </summary>
  /// <param name="schemes">Os esquemas informados. Nulo é aceito e significa o padrão do sanitizador.</param>
  /// <exception cref="InternalServerErrorException">Quando um esquema é inválido ou está bloqueado.</exception>
  internal static void Validate(IEnumerable<string>? schemes)
  {
    foreach (var scheme in schemes ?? [])
    {
      // Aceita o esquema com ou sem os dois-pontos, como aparece em uma URL
      var name = scheme?.Trim().TrimEnd(':') ?? string.Empty;

      if (!Syntax.IsMatch(name))
      {
        throw new InternalServerErrorException($"Options.Sanitizers.SchemeInvalid;{scheme}");
      }

      if (Blocked.Contains(name))
      {
        throw new InternalServerErrorException($"Options.Sanitizers.SchemeNotAllowed;{name}");
      }
    }
  }

  /// <summary>
  /// Normaliza os esquemas informados: sem espaços, sem os dois-pontos e em minúsculas.
  /// </summary>
  /// <param name="schemes">Os esquemas já validados.</param>
  /// <returns>Os esquemas normalizados.</returns>
  internal static IEnumerable<string> Normalize(IEnumerable<string> schemes) =>
    schemes.Select(scheme => scheme.Trim().TrimEnd(':').ToLowerInvariant());

  #endregion
}
