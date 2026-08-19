using System.Globalization;

namespace Tooark.Utils.Interfaces;

/// <summary>
/// Interface ILanguage que contém propriedades e métodos para gerenciamento de idiomas.
/// </summary>
public interface ILanguage
{
  #region Properties

  /// <summary>
  /// O código de idioma padrão usado na aplicação.
  /// </summary>
  string DefaultLanguage { get; }

  /// <summary>
  /// O código de idioma atual do ambiente de execução.
  /// </summary>
  string CurrentLanguage { get; }

  /// <summary>
  /// A cultura atual do ambiente de execução.
  /// </summary>
  CultureInfo CurrentCultureInfo { get; }

  #endregion

  #region Methods

  /// <summary>
  /// Função para definir a cultura atual para a aplicação.
  /// </summary>
  /// <param name="culture">O nome da cultura a ser definida. Exemplo: "en-US" ou "pt-BR".</param>
  void SetCultureInfo(string? culture);

  /// <summary>
  /// Função para definir a cultura atual para a aplicação.
  /// </summary>
  /// <param name="culture">A cultura a ser definida. Exemplo: "en-US" ou "pt-BR".</param>
  void SetCultureInfo(CultureInfo? culture);

  #endregion
}
