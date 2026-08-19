namespace Tooark.Utils.Messages;

/// <summary>
/// Classe com as mensagens de erro geradas pelo próprio pacote de utilitários.
/// </summary>
/// <remarks>
/// São chaves de tradução, e não textos finais. As traduções acompanham os recursos do
/// <c>Tooark.Extensions</c>, e a chave é montada no formato <c>Chave;parametro</c>.
/// </remarks>
public static class UtilErrorMessages
{
  /// <summary>
  /// Mensagem de erro para quando o conteúdo convertido excede o tamanho máximo permitido.
  /// </summary>
  /// <remarks>
  /// Acompanha o limite em bytes como parâmetro: <c>File.SizeExceeded;5242880</c>.
  /// </remarks>
  public const string FileSizeExceeded = "File.SizeExceeded";
}
