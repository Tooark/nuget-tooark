namespace Tooark.Sanitizers.Policies;

/// <summary>
/// Regra de aceitação de URL compartilhada pelos sanitizadores.
/// </summary>
/// <remarks>
/// Segue a regra do <c>isSafeUrl</c> do <c>@tooark/wysiwyg</c>, para que uma URL tenha o mesmo destino no navegador e
/// no servidor:
/// <list type="number">
/// <item>caracteres de controle e espaços saem de toda a URL, porque o navegador os ignora e <c>java&#9;script:</c>
/// vira <c>javascript:</c>;</item>
/// <item>relativa é a que começa com <c>/</c>, <c>#</c>, <c>?</c>, <c>./</c> ou <c>../</c>;</item>
/// <item>absoluta precisa de um esquema da lista de permissão.</item>
/// </list>
/// A URL devolvida é a limpa, sem normalização: o que o navegador vai interpretar é exatamente o que foi validado.
/// </remarks>
/// <param name="schemes">Esquemas permitidos em URL absoluta, já normalizados.</param>
/// <param name="allowRelative">Indica se URLs relativas são aceitas.</param>
/// <param name="allowCredentials">Indica se URLs com usuário e senha são aceitas.</param>
internal sealed class UrlPolicy(IEnumerable<string> schemes, bool allowRelative, bool allowCredentials)
{
  #region Private Fields

  /// <summary>
  /// Prefixos que caracterizam uma URL relativa.
  /// </summary>
  private static readonly string[] RelativePrefixes = ["/", "#", "?", "./", "../"];

  /// <summary>
  /// Esquemas permitidos em URL absoluta.
  /// </summary>
  private readonly HashSet<string> _schemes = new(schemes, StringComparer.OrdinalIgnoreCase);

  #endregion

  #region Methods

  /// <summary>
  /// Limpa a URL e confere se ela é aceita.
  /// </summary>
  /// <param name="url">A URL a ser conferida.</param>
  /// <returns>A URL sem caracteres de controle e espaços, ou nulo quando ela não é aceita.</returns>
  public string? Clean(string? url)
  {
    if (url is null)
    {
      return null;
    }

    // Remove o que o navegador ignoraria ao interpretar a URL
    var cleaned = Strip(url);

    if (cleaned.Length == 0)
    {
      return null;
    }

    // Relativa não tem esquema para conferir: vale a configuração
    if (RelativePrefixes.Any(prefix => cleaned.StartsWith(prefix, StringComparison.Ordinal)))
    {
      return allowRelative ? cleaned : null;
    }

    // Absoluta: o esquema precisa estar na lista, e o que não é URL absoluta é recusado
    if (!Uri.TryCreate(cleaned, UriKind.Absolute, out var uri) || !_schemes.Contains(uri.Scheme))
    {
      return null;
    }

    // Usuário e senha na URL disfarçam o host de destino. No mailto o que vem antes do @ é o endereço, e não
    // credencial, embora o Uri o leia como usuário.
    if (!allowCredentials && uri.UserInfo.Length > 0 && !uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return cleaned;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Remove caracteres de controle (C0, DEL e C1) e espaços de toda a URL.
  /// </summary>
  /// <param name="url">A URL original.</param>
  /// <returns>A URL sem esses caracteres.</returns>
  private static string Strip(string url) =>
    string.Concat(url.Where(c => !(c <= '\u001F' || (c >= '\u007F' && c <= '\u009F') || char.IsWhiteSpace(c) || c == '﻿')));

  #endregion
}
