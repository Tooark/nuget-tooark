using System.ComponentModel.DataAnnotations;
using Tooark.Attributes.Messages;
using Tooark.Exceptions;
using Tooark.Validations.Patterns;

namespace Tooark.Attributes;

/// <summary>
/// Atributo de validação de link de vídeo.
/// </summary>
/// <remarks>
/// O link é aceito quando corresponde a algum dos provedores habilitados.
/// Valor ausente é reportado como campo obrigatório.
/// Desabilitar os três provedores é erro de configuração: nenhum link poderia ser aceito, então o
/// atributo lança em vez de reprovar todo valor em silêncio.
/// </remarks>
/// <param name="propertyName">Nome do campo usado na mensagem de erro. Padrão: "Link".</param>
/// <param name="youtube">Permite link do YouTube. Padrão: true.</param>
/// <param name="vimeo">Permite link do Vimeo. Padrão: true.</param>
/// <param name="dailymotion">Permite link do Dailymotion. Padrão: true.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class LinkVideoValidationAttribute(
  string propertyName = "Link",
  bool youtube = true,
  bool vimeo = true,
  bool dailymotion = true
) : TooarkValidationAttribute(propertyName)
{
  #region Private Fields

  /// <summary>
  /// Indica se o link do YouTube é aceito.
  /// </summary>
  private readonly bool _youtube = youtube;

  /// <summary>
  /// Indica se o link do Vimeo é aceito.
  /// </summary>
  private readonly bool _vimeo = vimeo;

  /// <summary>
  /// Indica se o link do Dailymotion é aceito.
  /// </summary>
  private readonly bool _dailymotion = dailymotion;

  #endregion

  #region Methods

  /// <summary>
  /// Verifica se o valor é um link de vídeo válido em algum dos provedores habilitados.
  /// </summary>
  /// <param name="value">O valor a ser verificado.</param>
  /// <returns>Verdadeiro quando o valor é um link de vídeo válido.</returns>
  /// <exception cref="InternalServerErrorException">Se nenhum provedor estiver habilitado.</exception>
  protected override bool IsSatisfied(string value)
  {
    // Sem provedor habilitado nenhum link poderia ser aceito, o que torna o atributo inútil em silêncio
    if (!_youtube && !_vimeo && !_dailymotion)
    {
      throw new InternalServerErrorException($"{AttributeErrorMessages.LinkVideoNoProvider};{PropertyName}");
    }

    // Verifica o link contra os provedores habilitados
    return
      (_youtube && Matches(value, RegexPattern.YouTube)) ||
      (_vimeo && Matches(value, RegexPattern.Vimeo)) ||
      (_dailymotion && Matches(value, RegexPattern.Dailymotion));
  }

  #endregion
}
