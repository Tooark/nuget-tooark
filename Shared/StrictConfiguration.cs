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
/// O binder do .NET ignora em silêncio uma chave que não corresponde a nenhuma propriedade: um erro de digitação
/// como <c>"SampligRatio"</c> deixa a opção no valor padrão, sem aviso. Por isso toda seção lida por um pacote
/// Tooark falha no startup quando traz uma chave desconhecida, com <c>Options.Configuration.UnknownKey</c>.
/// <c>"Tooark": { "StrictConfiguration": false }</c> desliga a conferência; na linha 4.x ela era opt-in.
/// </para>
/// <para>
/// Este arquivo é compilado dentro de cada pacote que lê configuração (item <c>Compile</c> com <c>Link</c> no
/// .csproj), como tipo interno: assim a regra e o padrão ficam em um lugar só, sem API pública nova e sem levar
/// o binder de configuração para o <c>Tooark.Exceptions</c>.
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
  internal const bool DefaultEnabled = true;

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
  /// <exception cref="TooarkException">Quando um valor é recusado pelo tipo da própria opção, com a configuração estrita ligada ou não.</exception>
  internal static void Bind(IConfiguration configuration, string section, object options)
  {
    var source = configuration.GetSection(section);
    var strict = IsEnabled(configuration);

    try
    {
      // Desligada, a leitura é a do binder do .NET, que ignora o que não reconhece
      source.Bind(options, binder => binder.ErrorOnUnknownConfiguration = strict);
    }
    catch (InvalidOperationException exception) when (exception.InnerException is TooarkException inner)
    {
      // O valor foi recusado pelo próprio tipo da opção (um protocolo que não existe, por exemplo), e o binder
      // embrulha a falha da conversão: devolve a exceção original, com a chave de mensagem
      ExceptionDispatchInfo.Capture(inner).Throw();
    }
    catch (InvalidOperationException exception) when (strict && exception.Message.Contains(BinderMarker, StringComparison.Ordinal))
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
