using Tooark.Validations.Messages;

namespace Tooark.Validations;

/// <summary>
/// Classe de validação de double.
/// </summary>
public partial class Validation
{
  #region Formatters
  /// <summary>
  /// Valor de comparação para double com precisão de 10^-10 (0.0000000001).
  /// </summary>
  private static readonly double doubleEpsilon = 1e-10;

  /// <summary>
  /// Função para formatar para tipo double.
  /// </summary>
  /// <param name="value">Valor a ser formatado.</param>
  /// <returns>Valor formatado.</returns>
  private static string DoubleFormat(double value) => $"{value}";
  #endregion

  #region Converters
  /// <summary>
  /// Função para converter para double.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser convertido.</param>
  /// <returns>Valor convertido.</returns>
  private static double ConvertToDouble<T>(T value) where T : IConvertible => Convert.ToDouble(value);
  #endregion

  #region Validates
  /// <summary>
  /// Função para validar condição.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="condition">Condição a ser validada.</param>
  /// <returns>Validação.</returns>
  private Validation Validate(double value, double comparer, string property, string message, Func<double, double, bool> condition)
  {
    // Se a condição for verdadeira, adicione a notificação.
    if (condition(value, comparer))
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.DBL1");
    }

    // Retorna uma validação.
    return this;
  }

  /// <summary>
  /// Função para validar condição.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="condition">Condição a ser validada.</param>
  /// <returns>Validação.</returns>
  private Validation Validate<T>(double value, T comparer, string property, string message, Func<double, double, bool> condition) where T : IConvertible =>
    Validate(value, ConvertToDouble(comparer), property, message, condition);

  /// <summary>
  /// Função para validar lista de condição.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista a ser comparada.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="condition">Condição a ser validada.</param>
  /// <returns>Validação.</returns>
  private Validation ValidateList(double value, double[] list, string property, string message, Func<double, double[], bool> condition)
  {
    // Se a condição for verdadeira, adicione a notificação.
    if (condition(value, list))
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.DBL2");
    }

    // Retorna uma validação.
    return this;
  }

  /// <summary>
  /// Função para validar lista de condição.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista a ser comparada.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="condition">Condição a ser validada.</param>
  /// <returns>Validação.</returns>
  private Validation ValidateList<T>(double value, T[] list, string property, string message, Func<double, double[], bool> condition) where T : IConvertible =>
    ValidateList(value, [.. list.Select(item => ConvertToDouble(item))], property, message, condition);
  #endregion


  #region IsGreater
  /// <summary>
  /// Verifica se o valor é maior que o valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsGreater<T>(double value, T comparer, string property) where T : IConvertible =>
    IsGreater(value, comparer, property, ValidationErrorMessages.IsGreater(property, DoubleFormat(ConvertToDouble(comparer))));

  /// <summary>
  /// Verifica se o valor é maior que o valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsGreater<T>(double value, T comparer, string property, string message) where T : IConvertible =>
    Validate(value, comparer, property, message, (v, c) => v <= c);
  #endregion

  #region IsGreaterOrEquals
  /// <summary>
  /// Verifica se o valor é maior ou igual ao valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsGreaterOrEquals<T>(double value, T comparer, string property) where T : IConvertible =>
    IsGreaterOrEquals(value, comparer, property, ValidationErrorMessages.IsGreaterOrEquals(property, DoubleFormat(ConvertToDouble(comparer))));

  /// <summary>
  /// Verifica se o valor é maior ou igual ao valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsGreaterOrEquals<T>(double value, T comparer, string property, string message) where T : IConvertible =>
    Validate(value, comparer, property, message, (v, c) => v < c);
  #endregion

  #region IsLower
  /// <summary>
  /// Verifica se o valor é menor que o valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsLower<T>(double value, T comparer, string property) where T : IConvertible =>
    IsLower(value, comparer, property, ValidationErrorMessages.IsLower(property, DoubleFormat(ConvertToDouble(comparer))));

  /// <summary>
  /// Verifica se o valor é menor que o valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsLower<T>(double value, T comparer, string property, string message) where T : IConvertible =>
    Validate(value, comparer, property, message, (v, c) => v >= c);
  #endregion

  #region IsLowerOrEquals
  /// <summary>
  /// Verifica se o valor é menor ou igual ao valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsLowerOrEquals<T>(double value, T comparer, string property) where T : IConvertible =>
    IsLowerOrEquals(value, comparer, property, ValidationErrorMessages.IsLowerOrEquals(property, DoubleFormat(ConvertToDouble(comparer))));

  /// <summary>
  /// Verifica se o valor é menor ou igual ao valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsLowerOrEquals<T>(double value, T comparer, string property, string message) where T : IConvertible =>
    Validate(value, comparer, property, message, (v, c) => v > c);
  #endregion

  #region IsBetween
  /// <summary>
  /// Verifica se o valor é entre os valores comparados. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="start">Valor inicial.</param>
  /// <param name="end">Valor final.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsBetween<T>(double value, T start, T end, string property) where T : IConvertible =>
    IsBetween(value, start, end, property, ValidationErrorMessages.IsBetween(property, DoubleFormat(ConvertToDouble(start)), DoubleFormat(ConvertToDouble(end))));

  /// <summary>
  /// Verifica se o valor é entre os valores comparados
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="start">Valor inicial.</param>
  /// <param name="end">Valor final.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsBetween<T>(double value, T start, T end, string property, string message) where T : IConvertible =>
    Validate(value, start, property, message, (v, s) => v < s || v > ConvertToDouble(end));
  #endregion

  #region IsNotBetween
  /// <summary>
  /// Verifica se o valor não é entre os valores comparados. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="start">Valor inicial.</param>
  /// <param name="end">Valor final.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotBetween<T>(double value, T start, T end, string property) where T : IConvertible =>
    IsNotBetween(value, start, end, property, ValidationErrorMessages.IsNotBetween(property, DoubleFormat(ConvertToDouble(start)), DoubleFormat(ConvertToDouble(end))));

  /// <summary>
  /// Verifica se o valor não é entre os valores comparados
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="start">Valor inicial.</param>
  /// <param name="end">Valor final.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotBetween<T>(double value, T start, T end, string property, string message) where T : IConvertible =>
    Validate(value, start, property, message, (v, s) => v >= s && v <= ConvertToDouble(end));
  #endregion

  #region IsMin
  /// <summary>
  /// Verifica se o valor é igual ao valor mínimo do tipo. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsMin(double value, string property) =>
    IsMin(value, property, ValidationErrorMessages.IsMin(property, DoubleFormat(double.MinValue)));

  /// <summary>
  /// Verifica se o valor é igual ao valor mínimo do tipo.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsMin(double value, string property, string message) =>
    Validate(value, double.MinValue, property, message, (v, c) => Math.Abs(v - c) >= doubleEpsilon);
  #endregion

  #region IsNotMin
  /// <summary>
  /// Verifica se o valor é não igual ao valor mínimo do tipo. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotMin(double value, string property) =>
    IsNotMin(value, property, ValidationErrorMessages.IsNotMin(property, DoubleFormat(double.MinValue)));

  /// <summary>
  /// Verifica se o valor é não igual ao valor mínimo do tipo.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotMin(double value, string property, string message) =>
    Validate(value, double.MinValue, property, message, (v, c) => Math.Abs(v - c) < doubleEpsilon);
  #endregion

  #region IsMax
  /// <summary>
  /// Verifica se o valor é igual ao valor máximo do tipo. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsMax(double value, string property) =>
    IsMax(value, property, ValidationErrorMessages.IsMax(property, DoubleFormat(double.MaxValue)));

  /// <summary>
  /// Verifica se o valor é igual ao valor máximo do tipo.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsMax(double value, string property, string message) =>
    Validate(value, double.MaxValue, property, message, (v, c) => Math.Abs(v - c) >= doubleEpsilon);
  #endregion

  #region IsNotMax
  /// <summary>
  /// Verifica se o valor é não igual ao valor máximo do tipo. Com mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotMax(double value, string property) =>
    IsNotMax(value, property, ValidationErrorMessages.IsNotMax(property, DoubleFormat(double.MaxValue)));

  /// <summary>
  /// Verifica se o valor é não igual ao valor máximo do tipo.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotMax(double value, string property, string message) =>
    Validate(value, double.MaxValue, property, message, (v, c) => Math.Abs(v - c) < doubleEpsilon);
  #endregion

  #region AreEquals
  /// <summary>
  /// Verifica se o valor é igual ao valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation AreEquals<T>(double value, T comparer, string property) where T : IConvertible =>
    AreEquals(value, comparer, property, ValidationErrorMessages.AreEquals(property, DoubleFormat(ConvertToDouble(comparer))));

  /// <summary>
  /// Verifica se o valor é igual ao valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation AreEquals<T>(double value, T comparer, string property, string message) where T : IConvertible =>
    Validate(value, comparer, property, message, (v, c) => Math.Abs(v - c) >= doubleEpsilon);
  #endregion

  #region AreNotEquals
  /// <summary>
  /// Verifica se o valor não é igual ao valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation AreNotEquals<T>(double value, T comparer, string property) where T : IConvertible =>
    AreNotEquals(value, comparer, property, ValidationErrorMessages.AreNotEquals(property, DoubleFormat(ConvertToDouble(comparer))));

  /// <summary>
  /// Verifica se o valor não é igual ao valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation AreNotEquals<T>(double value, T comparer, string property, string message) where T : IConvertible =>
    Validate(value, comparer, property, message, (v, c) => Math.Abs(v - c) < doubleEpsilon);
  #endregion

  #region Contains
  /// <summary>
  /// Verifica se contém o valor na lista. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista de valores a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation Contains<T>(double value, T[] list, string property) where T : IConvertible =>
    Contains(value, list, property, ValidationErrorMessages.Contains(property, DoubleFormat(value)));

  /// <summary>
  /// Verifica se contém o valor na lista.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista de valores a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation Contains<T>(double value, T[] list, string property, string message) where T : IConvertible =>
    ValidateList(value, list, property, message, (v, l) => !l.Any(x => Math.Abs(v - x) < doubleEpsilon));
  #endregion

  #region NotContains
  /// <summary>
  /// Verifica se não contém o valor na lista. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista de valores a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation NotContains<T>(double value, T[] list, string property) where T : IConvertible =>
    NotContains(value, list, property, ValidationErrorMessages.NotContains(property, DoubleFormat(value)));

  /// <summary>
  /// Verifica se não contém o valor na lista.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista de valores a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation NotContains<T>(double value, T[] list, string property, string message) where T : IConvertible =>
    ValidateList(value, list, property, message, (v, l) => l.Any(x => Math.Abs(v - x) < doubleEpsilon));
  #endregion

  #region All
  /// <summary>
  /// Verifica se todos os valores são iguais ao valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista de valores a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation All<T>(double value, T[] list, string property) where T : IConvertible =>
    All(value, list, property, ValidationErrorMessages.All(property, DoubleFormat(value)));

  /// <summary>
  /// Verifica se todos os valores são iguais ao valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista de valores a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation All<T>(double value, T[] list, string property, string message) where T : IConvertible =>
    ValidateList(value, list, property, message, (v, l) => !l.All(x => Math.Abs(v - x) < doubleEpsilon));
  #endregion

  #region NotAll
  /// <summary>
  /// Verifica se todos os valores são diferentes ao valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista de valores a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation NotAll<T>(double value, T[] list, string property) where T : IConvertible =>
    NotAll(value, list, property, ValidationErrorMessages.NotAll(property, DoubleFormat(value)));

  /// <summary>
  /// Verifica se todos os valores são diferentes ao valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="list">Lista de valores a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation NotAll<T>(double value, T[] list, string property, string message) where T : IConvertible =>
    ValidateList(value, list, property, message, (v, l) => l.All(x => Math.Abs(v - x) < doubleEpsilon));
  #endregion

  #region IsNull
  /// <summary>
  /// Valida se o valor é nulo. Com a mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <returns>Validação.</returns>
  public Validation IsNull(double? value, string property) =>
    IsNull(value, property, ValidationErrorMessages.IsNull(property));

  /// <summary>
  /// Valida se o valor é nulo.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsNull(double? value, string property, string message) =>
    ValidateNull(value, false, property, message);
  #endregion

  #region IsNotNull
  /// <summary>
  /// Valida se o valor não é nulo. Com a mensagem padrão.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotNull(double? value, string property) =>
    IsNotNull(value, property, ValidationErrorMessages.IsNotNull(property));

  /// <summary>
  /// Valida se o valor é nulo.
  /// </summary>
  /// <param name="value">Valor a ser validado.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotNull(double? value, string property, string message) =>
    ValidateNull(value, true, property, message);
  #endregion
}
