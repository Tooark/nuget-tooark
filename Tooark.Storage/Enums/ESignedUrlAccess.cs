namespace Tooark.Storage.Enums;

/// <summary>
/// Enumeração para definir o acesso concedido por uma URL assinada.
/// </summary>
public enum ESignedUrlAccess
{
  /// <summary>
  /// Leitura do objeto (GET), para exibir ou baixar sem expor o bucket.
  /// </summary>
  Read = 0,

  /// <summary>
  /// Escrita do objeto (PUT), para o navegador enviar o arquivo direto ao storage, sem passar pela API.
  /// </summary>
  Write = 1
}
