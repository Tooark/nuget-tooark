using Microsoft.Extensions.Localization;
using Tooark.Extensions;

namespace Tooark.Dtos;

/// <summary>
/// Classe base para DTOs.
/// </summary>
/// <remarks>
/// Fornece o localizador usado para traduzir as chaves de erro das respostas. O localizador resolve o idioma
/// pelo fluxo de execução e lê as traduções de arquivos, então não depende do container de injeção de
/// dependência: a tradução funciona com ou sem <c>AddTooarkDtos</c>.
/// </remarks>
public abstract class Dto
{
  #region Private Static Fields

  /// <summary>
  /// Localizador de strings compartilhado pelos DTOs.
  /// </summary>
  private static readonly IStringLocalizer Localizer = new JsonStringLocalizerExtension();

  #endregion

  #region Properties

  /// <summary>
  /// Obtém o localizador de strings.
  /// </summary>
  internal static IStringLocalizer LocalizerString => Localizer;

  #endregion
}
