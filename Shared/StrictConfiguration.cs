using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Tooark.Exceptions;

namespace Tooark.Shared;

/// <summary>
/// Leitura das seções de configuração dos pacotes Tooark, com a configuração estrita opcional.
/// </summary>
/// <remarks>
/// <para>
/// Por padrão o binder do .NET ignora em silêncio uma chave que não corresponde a nenhuma propriedade: um erro
/// de digitação como <c>"SampligRatio"</c> deixa a opção no valor padrão, sem aviso. Com
/// <c>"Tooark": { "StrictConfiguration": true }</c> na configuração, toda seção lida por um pacote Tooark passa a
/// falhar no startup quando traz uma chave desconhecida, com <c>Options.Configuration.UnknownKey</c>.
/// </para>
/// <para>
/// Este arquivo é compilado dentro de cada pacote que lê configuração (item <c>Compile</c> com <c>Link</c> no
/// .csproj), como tipo interno: assim a regra e o padrão ficam em um lugar só, sem API pública nova e sem levar
/// o binder de configuração para o <c>Tooark.Exceptions</c>. A v5 liga a opção por padrão trocando só
/// <see cref="DefaultEnabled"/>.
/// </para>
/// </remarks>
internal static partial class StrictConfiguration
{
  #region Constants

  /// <summary>
  /// Seção de configuração das opções gerais do Tooark.
  /// </summary>
  internal const string Section = "Tooark";

  /// <summary>
  /// Chave que liga ou desliga a configuração estrita, dentro de <see cref="Section"/>.
  /// </summary>
  internal const string Key = "StrictConfiguration";

  /// <summary>
  /// Valor da configuração estrita quando a chave não é informada.
  /// </summary>
  internal const bool DefaultEnabled = false;

  /// <summary>
  /// Trecho da mensagem com que o binder do .NET acusa as chaves desconhecidas. As mensagens do runtime não são
  /// traduzidas, e os testes fixam este texto nos dois alvos.
  /// </summary>
  private const string BinderMarker = "ErrorOnUnknownConfiguration";

  #endregion

  #region Methods

  /// <summary>
  /// Indica se a configuração estrita está ligada.
  /// </summary>
  /// <param name="configuration">Configuração da aplicação (raiz), ou nulo quando o pacote foi registrado sem ela.</param>
  /// <returns>O valor de <c>Tooark:StrictConfiguration</c>, ou <see cref="DefaultEnabled"/> quando ausente.</returns>
  /// <exception cref="InternalServerErrorException">Quando o valor informado não é <c>true</c> nem <c>false</c>.</exception>
  internal static bool IsEnabled(IConfiguration? configuration)
  {
    var value = configuration?.GetSection(Section)[Key];

    // Sem a chave vale o padrão da versão
    if (string.IsNullOrWhiteSpace(value))
    {
      return DefaultEnabled;
    }

    // Um valor ilegível não pode virar "desligado" em silêncio: é justamente o erro que a opção existe para pegar
    if (!bool.TryParse(value.Trim(), out var enabled))
    {
      throw new InternalServerErrorException($"Options.Configuration.StrictInvalid;{value}");
    }

    return enabled;
  }

  /// <summary>
  /// Carrega uma seção da configuração nas opções, recusando chaves desconhecidas quando a configuração estrita
  /// está ligada.
  /// </summary>
  /// <param name="configuration">Configuração da aplicação (raiz).</param>
  /// <param name="section">Caminho da seção, como <c>Jwt</c> ou <c>OpenId:Entra</c>.</param>
  /// <param name="options">Instância que recebe os valores.</param>
  /// <exception cref="InternalServerErrorException">Quando a seção traz uma chave desconhecida e a configuração estrita está ligada.</exception>
  internal static void Bind(IConfiguration configuration, string section, object options)
  {
    var source = configuration.GetSection(section);

    // Desligada, a leitura é a de sempre: nenhuma mudança de comportamento para quem não optou
    if (!IsEnabled(configuration))
    {
      source.Bind(options);

      return;
    }

    try
    {
      source.Bind(options, binder => binder.ErrorOnUnknownConfiguration = true);
    }
    catch (InvalidOperationException exception) when (exception.Message.Contains(BinderMarker, StringComparison.Ordinal))
    {
      // O binder já fez a comparação, inclusive nos objetos aninhados: a exceção dele vira a do Tooark, com a
      // chave de mensagem, a seção e os nomes que ele não reconheceu
      throw new InternalServerErrorException(
        $"Options.Configuration.UnknownKey;{section};{UnknownKeys(exception.Message)}",
        exception
      );
    }
  }

  /// <summary>
  /// Confere uma seção contra o tipo das opções quando a configuração estrita está ligada, sem guardar o que leu.
  /// </summary>
  /// <remarks>
  /// Para os registros que só deixam a leitura agendada no sistema de options
  /// (<c>services.Configure&lt;T&gt;(section)</c>): sem esta conferência a chave desconhecida só apareceria no
  /// primeiro uso das opções, e não no startup. Desligada, não faz nada.
  /// </remarks>
  /// <typeparam name="TOptions">Tipo das opções da seção.</typeparam>
  /// <param name="configuration">Configuração da aplicação (raiz).</param>
  /// <param name="section">Caminho da seção.</param>
  /// <exception cref="InternalServerErrorException">Quando a seção traz uma chave desconhecida e a configuração estrita está ligada.</exception>
  internal static void Validate<TOptions>(IConfiguration configuration, string section) where TOptions : new()
  {
    if (!IsEnabled(configuration))
    {
      return;
    }

    try
    {
      Bind(configuration, section, new TOptions());
    }
    catch (TargetInvocationException exception) when (exception.InnerException is TooarkException inner)
    {
      // O binder atribui as propriedades por reflexão e embrulha a falha de um setter que valida o valor:
      // devolve a exceção original, com a chave de mensagem
      ExceptionDispatchInfo.Capture(inner).Throw();
    }
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Extrai da mensagem do binder os nomes das chaves que ele não reconheceu.
  /// </summary>
  /// <param name="message">Mensagem da exceção do binder.</param>
  /// <returns>Os nomes separados por vírgula, ou <c>?</c> quando a mensagem não os traz no formato esperado.</returns>
  private static string UnknownKeys(string message)
  {
    // Os nomes vêm entre aspas simples depois dos dois-pontos; antes deles a mensagem cita a própria opção do binder
    var list = message[(message.LastIndexOf(": ", StringComparison.Ordinal) + 1)..];
    var names = QuotedNameRegex().Matches(list).Select(match => match.Groups[1].Value).ToArray();

    return names.Length == 0 ? "?" : string.Join(", ", names);
  }

  /// <summary>
  /// Expressão de um nome entre aspas simples.
  /// </summary>
  [GeneratedRegex("'([^']+)'", RegexOptions.None, matchTimeoutMilliseconds: 250)]
  private static partial Regex QuotedNameRegex();

  #endregion
}
