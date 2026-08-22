using Microsoft.Extensions.Localization;

namespace Tooark.Extensions.Factories;

/// <summary>
/// Fábrica de instâncias de <see cref="JsonStringLocalizerExtension"/>.
/// </summary>
/// <remarks>
/// A fábrica é responsável por criar instâncias de <see cref="JsonStringLocalizerExtension"/>.
/// Os dois métodos ignoram os argumentos: o localizador resolve as traduções pelo idioma do fluxo de
/// execução, e não pelo tipo ou pelo caminho do recurso.
/// </remarks>
/// <seealso cref="IStringLocalizerFactory"/>
public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
  #region Methods

  /// <summary>
  /// Cria uma instância de <see cref="JsonStringLocalizerExtension"/>.
  /// </summary>
  /// <param name="resourceSource">Parâmetro não utilizado.</param>
  /// <returns>Instância de <see cref="IStringLocalizer"/>.</returns>
  public IStringLocalizer Create(Type resourceSource) => new JsonStringLocalizerExtension();

  /// <summary>
  /// Cria uma instância de <see cref="JsonStringLocalizerExtension"/>.
  /// </summary>
  /// <param name="baseName">Parâmetro não utilizado.</param>
  /// <param name="location">Parâmetro não utilizado.</param>
  /// <returns>Instância de <see cref="IStringLocalizer"/>.</returns>
  public IStringLocalizer Create(string baseName, string location) => new JsonStringLocalizerExtension();

  #endregion
}
