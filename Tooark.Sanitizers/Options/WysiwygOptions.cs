namespace Tooark.Sanitizers.Options;

/// <summary>
/// Classe que representa as opções do sanitizador do conteúdo do <c>@tooark/wysiwyg</c>.
/// </summary>
/// <remarks>
/// As regras de URL, cor e estrutura são as do componente e não são configuráveis: o conteúdo aceito aqui precisa ser
/// o mesmo que o editor e o viewer aceitam.
/// </remarks>
public class WysiwygOptions
{
  #region Properties

  /// <summary>
  /// Indica se nós de imagem são mantidos. Padrão: verdadeiro.
  /// </summary>
  /// <remarks>
  /// Desligue quando a aplicação não habilita o grupo <c>media</c> do editor: sem isso, um JSON montado fora do
  /// editor ainda pode trazer uma imagem apontando para um servidor de rastreamento.
  /// </remarks>
  public bool AllowImages { get; set; } = true;

  /// <summary>
  /// Indica se nós de vídeo são mantidos. Padrão: verdadeiro.
  /// </summary>
  public bool AllowVideos { get; set; } = true;

  #endregion
}
