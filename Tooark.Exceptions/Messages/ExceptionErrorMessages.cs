namespace Tooark.Exceptions.Messages;

/// <summary>
/// Classe com as mensagens de erro geradas pelo próprio pacote de exceções.
/// </summary>
/// <remarks>
/// São chaves de tradução, e não textos finais. Ocorrem quando a exceção é construída sem uma
/// mensagem utilizável — situação em que a alternativa seria uma exceção sem erro algum.
/// </remarks>
public static class ExceptionErrorMessages
{
  /// <summary>
  /// Mensagem de erro para quando a mensagem informada é nula, vazia ou composta apenas por espaços em branco.
  /// </summary>
  public const string MessageIsNullOrEmpty = "Exceptions.MessageNullEmpty";

  /// <summary>
  /// Mensagem de erro para quando a lista de mensagens ou a notificação informada é nula ou não contém mensagens.
  /// </summary>
  public const string ErrorsIsNullOrEmpty = "Exceptions.ErrorsNullOrEmpty";
}
