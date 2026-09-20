using Tooark.Validations.Messages;

namespace Tooark.Validations;

/// <summary>
/// Classe de validação de List.
/// </summary>
public partial class Validation
{
  #region Validates
  /// <summary>
  /// Função para validar condição.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Valor a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="condition">Condição a ser validada.</param>
  /// <returns>Validação.</returns>
  private Validation Validate<T>(T[] list, int comparer, string property, string message, Func<T[], int, bool> condition) where T : IConvertible
  {
    // Lista nula é tratada como vazia: a validação de tamanho reprova a lista em vez de lançar.
    // Se a condição for verdadeira, adicione a notificação.
    if (condition(list ?? [], comparer))
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.LST1");
    }

    // Retorna uma validação.
    return this;
  }

  /// <summary>
  /// Função para validar lista de condição.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Lista a ser comparada.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <param name="condition">Condição a ser validada.</param>
  /// <returns>Validação.</returns>
  private Validation ValidateList<T>(T[] list, T[] comparer, string property, string message, Func<T[], T[], bool> condition) where T : IConvertible
  {
    // Listas nulas são tratadas como vazias, mantendo a comparação sem lançar.
    // Se a condição for verdadeira, adicione a notificação.
    if (condition(list ?? [], comparer ?? []))
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.LST2");
    }

    // Retorna uma validação.
    return this;
  }
  #endregion


  #region IsGreater
  /// <summary>
  /// Verifica se o tamanho da lista é maior que o valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Tamanho a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsGreater<T>(T[] list, int comparer, string property) where T : IConvertible =>
    IsGreater(list, comparer, property, ValidationErrorMessages.IsGreater(property, IntFormat(comparer)));

  /// <summary>
  /// Verifica se o tamanho da lista é maior que o valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Tamanho a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsGreater<T>(T[] list, int comparer, string property, string message) where T : IConvertible =>
    Validate(list, comparer, property, message, (v, c) => v.Length <= c);
  #endregion

  #region IsGreaterOrEquals
  /// <summary>
  /// Verifica se o tamanho da lista é maior ou igual que o valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Tamanho a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsGreaterOrEquals<T>(T[] list, int comparer, string property) where T : IConvertible =>
    IsGreaterOrEquals(list, comparer, property, ValidationErrorMessages.IsGreaterOrEquals(property, IntFormat(comparer)));

  /// <summary>
  /// Verifica se o tamanho da lista é maior ou igual que o valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Tamanho a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsGreaterOrEquals<T>(T[] list, int comparer, string property, string message) where T : IConvertible =>
    Validate(list, comparer, property, message, (v, c) => v.Length < c);
  #endregion

  #region IsLower
  /// <summary>
  /// Verifica se o tamanho da lista é menor que o valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Tamanho a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsLower<T>(T[] list, int comparer, string property) where T : IConvertible =>
    IsLower(list, comparer, property, ValidationErrorMessages.IsLower(property, IntFormat(comparer)));

  /// <summary>
  /// Verifica se o tamanho da lista é menor que o valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Tamanho a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsLower<T>(T[] list, int comparer, string property, string message) where T : IConvertible =>
    Validate(list, comparer, property, message, (v, c) => v.Length >= c);
  #endregion

  #region IsLowerOrEquals
  /// <summary>
  /// Verifica se o tamanho da lista é menor ou igual que o valor comparado. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Tamanho a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation IsLowerOrEquals<T>(T[] list, int comparer, string property) where T : IConvertible =>
    IsLowerOrEquals(list, comparer, property, ValidationErrorMessages.IsLowerOrEquals(property, IntFormat(comparer)));

  /// <summary>
  /// Verifica se o tamanho da lista é menor ou igual que o valor comparado.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Tamanho a ser comparado.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsLowerOrEquals<T>(T[] list, int comparer, string property, string message) where T : IConvertible =>
    Validate(list, comparer, property, message, (v, c) => v.Length > c);
  #endregion

  #region AreEquals
  /// <summary>
  /// Verifica se a lista é igual a lista comparada. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Lista a ser comparada.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation AreEquals<T>(T[] list, T[] comparer, string property) where T : IConvertible =>
    AreEquals(list, comparer, property, ValidationErrorMessages.AreEquals(property, "List"));

  /// <summary>
  /// Verifica se a lista é igual a lista comparada. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Lista a ser comparada.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation AreEquals<T>(T[] list, T[] comparer, string property, string message) where T : IConvertible =>
    ValidateList(list, comparer, property, message, (v, c) => !v.SequenceEqual(c));
  #endregion

  #region AreNotEquals
  /// <summary>
  /// Verifica se a lista não é igual a lista comparada. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Lista a ser comparada.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <returns>Validação.</returns>
  public Validation AreNotEquals<T>(T[] list, T[] comparer, string property) where T : IConvertible =>
    AreNotEquals(list, comparer, property, ValidationErrorMessages.AreNotEquals(property, "List"));

  /// <summary>
  /// Verifica se a lista não é igual a lista comparada. Com mensagem padrão.
  /// </summary>
  /// <typeparam name="T">Tipo a ser convertido.</typeparam>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="comparer">Lista a ser comparada.</param>
  /// <param name="property">Propriedade a ser validada.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation AreNotEquals<T>(T[] list, T[] comparer, string property, string message) where T : IConvertible =>
    ValidateList(list, comparer, property, message, (v, c) => v.SequenceEqual(c));
  #endregion

  #region IsNull
  /// <summary>
  /// Valida se a lista é nula. Com a mensagem padrão.
  /// </summary>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <returns>Validação.</returns>
  public Validation IsNull<T>(T[]? list, string property) =>
    IsNull(list, property, ValidationErrorMessages.IsNull(property));

  /// <summary>
  /// Valida se a lista é nula.
  /// </summary>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsNull<T>(T[]? list, string property, string message)
  {
    // Se a condição for verdadeira, adicione a notificação.
    if (list != null)
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.LST3");
    }

    // Retorna uma validação.
    return this;
  }
  #endregion

  #region IsNotNull
  /// <summary>
  /// Valida se a lista não é nula. Com a mensagem padrão.
  /// </summary>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotNull<T>(T[]? list, string property) =>
    IsNotNull(list, property, ValidationErrorMessages.IsNotNull(property));

  /// <summary>
  /// Valida se a lista não é nula.
  /// </summary>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotNull<T>(T[]? list, string property, string message)
  {
    // Se a condição for verdadeira, adicione a notificação.
    if (list == null)
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.LST4");
    }

    // Retorna uma validação.
    return this;
  }
  #endregion

  #region IsEmpty
  /// <summary>
  /// Valida se a lista é vazia. Com a mensagem padrão.
  /// </summary>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmpty<T>(T[]? list, string property) =>
    IsEmpty(list, property, ValidationErrorMessages.IsEmpty(property));

  /// <summary>
  /// Valida se a lista é vazia.
  /// </summary>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsEmpty<T>(T[]? list, string property, string message)
  {
    // Se a condição for verdadeira, adicione a notificação.
    if (list?.Length > 0)
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.LST5");
    }

    // Retorna uma validação.
    return this;
  }
  #endregion

  #region IsNotEmpty
  /// <summary>
  /// Valida se a lista não é vazia. Com a mensagem padrão.
  /// </summary>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotEmpty<T>(T[]? list, string property) =>
    IsNotEmpty(list, property, ValidationErrorMessages.IsNotEmpty(property));

  /// <summary>
  /// Valida se a lista não é vazia.
  /// </summary>
  /// <param name="list">Lista a ser validada.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <param name="message">Mensagem de erro.</param>
  /// <returns>Validação.</returns>
  public Validation IsNotEmpty<T>(T[]? list, string property, string message)
  {
    // Se a condição for verdadeira, adicione a notificação.
    if (list?.Length == 0)
    {
      // Adiciona a notificação.
      AddNotification(message, property, "T.VLD.LST6");
    }

    // Retorna uma validação.
    return this;
  }
  #endregion
}
