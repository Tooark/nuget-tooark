using System.Globalization;
using System.Text.RegularExpressions;
using Tooark.Utils.Interfaces;
using Tooark.Validations.Patterns;

namespace Tooark.Utils;

/// <summary>
/// Classe estática Language que contém constantes e propriedades para gerenciamento de idiomas.
/// </summary>
/// <remarks>
/// O idioma atual acompanha a cultura do fluxo de execução (<see cref="CultureInfo.CurrentCulture"/>), e não um
/// estado global do processo. Em uma aplicação web, cada requisição enxerga apenas a própria cultura.
/// </remarks>
public static class Language
{
  #region Private Static Fields

  /// <summary>
  /// Campo privado que armazena a instância da interface ILanguage.
  /// </summary>
  private static ILanguage _instance = new LanguageImplementation();

  #endregion

  #region Properties

  /// <summary>
  /// A instância da interface ILanguage.
  /// </summary>
  /// <exception cref="ArgumentNullException">Se a instância informada for nula.</exception>
  public static ILanguage Instance
  {
    get => _instance;
    set => _instance = value ?? throw new ArgumentNullException(nameof(value));
  }

  /// <summary>
  /// O código de idioma padrão usado na aplicação. Padrão "en-US".
  /// </summary>
  public static string Default { get => Instance.DefaultLanguage; }

  /// <summary>
  /// O código de idioma atual do ambiente de execução.
  /// </summary>
  public static string Current { get => Instance.CurrentLanguage; }

  /// <summary>
  /// A cultura atual do ambiente de execução.
  /// </summary>
  public static CultureInfo CurrentCulture { get => Instance.CurrentCultureInfo; }

  #endregion

  #region Methods

  /// <summary>
  /// Função para definir a cultura atual para a aplicação.
  /// </summary>
  /// <remarks>
  /// A cultura vale apenas para o fluxo de execução atual. Se o nome for nulo, vazio ou não corresponder ao
  /// formato <c>xx-XX</c>, a cultura padrão da aplicação é aplicada.
  /// </remarks>
  /// <param name="culture">O nome da cultura a ser definida. Exemplo: "en-US" ou "pt-BR".</param>
  public static void SetCulture(string? culture)
  {
    Instance.SetCultureInfo(culture);
  }

  /// <summary>
  /// Função para definir a cultura atual para a aplicação.
  /// </summary>
  /// <remarks>
  /// A cultura vale apenas para o fluxo de execução atual. Se a cultura for nula, a cultura padrão da aplicação
  /// é aplicada.
  /// </remarks>
  /// <param name="culture">A cultura a ser definida. Exemplo: "en-US" ou "pt-BR".</param>
  public static void SetCulture(CultureInfo? culture)
  {
    Instance.SetCultureInfo(culture);
  }

  #endregion

  #region Internal Classes

  /// <summary>
  /// Implementação da interface ILanguage.
  /// </summary>
  internal class LanguageImplementation : ILanguage
  {
    #region Properties

    /// <summary>
    /// O código de idioma padrão usado na aplicação. Padrão "en-US".
    /// </summary>
    public string DefaultLanguage => InternalLanguage.Default;

    /// <summary>
    /// O código de idioma atual do ambiente de execução.
    /// </summary>
    public string CurrentLanguage => InternalLanguage.Current;

    /// <summary>
    /// A cultura atual do ambiente de execução.
    /// </summary>
    public CultureInfo CurrentCultureInfo => InternalLanguage.CurrentCulture;

    #endregion

    #region Methods

    /// <summary>
    /// Função para definir a cultura atual para a aplicação.
    /// </summary>
    /// <param name="culture">O nome da cultura a ser definida. Exemplo: "en-US" ou "pt-BR".</param>
    public void SetCultureInfo(string? culture)
    {
      InternalLanguage.SetCulture(culture);
    }

    /// <summary>
    /// Função para definir a cultura atual para a aplicação.
    /// </summary>
    /// <param name="culture">A cultura a ser definida. Exemplo: "en-US" ou "pt-BR".</param>
    public void SetCultureInfo(CultureInfo? culture)
    {
      InternalLanguage.SetCulture(culture);
    }

    #endregion
  }

  #endregion
}

/// <summary>
/// Classe estática interna Language que contém constantes e propriedades para gerenciamento de idiomas.
/// </summary>
internal static class InternalLanguage
{
  #region Private Static Fields

  /// <summary>
  /// Tempo máximo de execução da expressão regular que valida o nome da cultura.
  /// </summary>
  private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

  /// <summary>
  /// A cultura padrão usada na aplicação, aplicada quando a cultura solicitada é inválida.
  /// </summary>
  private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("en-US");

  #endregion

  #region Internal Static Fields

  /// <summary>
  /// O código de idioma padrão usado na aplicação. Padrão "en-US".
  /// </summary>
  internal static readonly string Default = DefaultCulture.Name;

  #endregion

  #region Properties

  /// <summary>
  /// O código de idioma atual do ambiente de execução.
  /// </summary>
  internal static string Current { get => CurrentCulture.Name; }

  /// <summary>
  /// A cultura atual do ambiente de execução.
  /// </summary>
  /// <remarks>
  /// Lê diretamente a cultura do fluxo de execução, que o .NET propaga por requisição e por contexto assíncrono.
  /// Guardar esse valor em um campo estático faria o idioma de uma requisição vazar para as demais.
  /// </remarks>
  internal static CultureInfo CurrentCulture { get => CultureInfo.CurrentCulture; }

  #endregion

  #region Methods

  /// <summary>
  /// Função para definir a cultura atual para a aplicação.
  /// </summary>
  /// <param name="culture">O nome da cultura a ser definida. Exemplo: "en-US" ou "pt-BR".</param>
  internal static void SetCulture(string? culture)
  {
    // Define a cultura atual a partir do nome informado
    SetCulture(CreateCulture(culture));
  }

  /// <summary>
  /// Função para definir a cultura atual para a aplicação.
  /// </summary>
  /// <param name="culture">A cultura a ser definida. Exemplo: "en-US" ou "pt-BR".</param>
  internal static void SetCulture(CultureInfo? culture)
  {
    // Se a cultura for nula, aplica a cultura padrão
    culture ??= DefaultCulture;

    // Define a cultura atual e a cultura de interface do usuário do fluxo de execução
    CultureInfo.CurrentCulture = culture;
    CultureInfo.CurrentUICulture = culture;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Cria a cultura a partir do nome informado, retornando a cultura padrão quando o nome é inválido.
  /// </summary>
  /// <param name="culture">O nome da cultura. Exemplo: "en-US" ou "pt-BR".</param>
  /// <returns>A cultura correspondente ao nome informado, ou a cultura padrão.</returns>
  private static CultureInfo CreateCulture(string? culture)
  {
    // Verifica se o nome da cultura foi informado
    if (string.IsNullOrWhiteSpace(culture))
    {
      // Retorna a cultura padrão caso o nome não tenha sido informado
      return DefaultCulture;
    }

    try
    {
      // Verifica se o nome da cultura segue o formato esperado, sem diferenciar maiúsculas de minúsculas
      if (!Regex.IsMatch(culture, RegexPattern.CultureIgnoreCase, RegexOptions.None, RegexTimeout))
      {
        // Retorna a cultura padrão caso o nome não siga o formato esperado
        return DefaultCulture;
      }

      // Cria a cultura com o nome informado, que o .NET normaliza para o formato "xx-XX"
      return CultureInfo.GetCultureInfo(culture);
    }
    catch (CultureNotFoundException)
    {
      // Retorna a cultura padrão caso a cultura não exista
      return DefaultCulture;
    }
    catch (RegexMatchTimeoutException)
    {
      // Retorna a cultura padrão caso a validação do nome exceda o tempo limite
      return DefaultCulture;
    }
  }

  #endregion
}
