using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;
using Tooark.Notifications;

namespace Tooark.Dtos;

/// <summary>
/// Classe de resposta padrão.
/// </summary>
/// <remarks>
/// A classe <see cref="ResponseDto{T}"/> é uma classe de resposta padrão para operações de API.
/// Fornece um objeto de resposta com dados, erros, metadados e informações de paginação.
/// <para>
/// As mensagens de erro passam pelo localizador uma única vez: uma chave conhecida vira o texto traduzido,
/// e um texto que não seja chave chega inalterado.
/// </para>
/// </remarks>
public class ResponseDto<T> : Dto
{
  #region Private Fields

  /// <summary>
  /// Lista interna de erros.
  /// </summary>
  private readonly List<string> _errors = [];

  /// <summary>
  /// Lista interna de metadados.
  /// </summary>
  private readonly List<MetadataDto> _metadata = [];

  /// <summary>
  /// Coleção somente leitura dos erros, reutilizada a cada leitura.
  /// </summary>
  private readonly ReadOnlyCollection<string> _readOnlyErrors;

  /// <summary>
  /// Coleção somente leitura dos metadados, reutilizada a cada leitura.
  /// </summary>
  private readonly ReadOnlyCollection<MetadataDto> _readOnlyMetadata;

  #endregion

  #region Constructor

  /// <summary>
  /// Construtor base, que prepara as coleções somente leitura.
  /// </summary>
  /// <remarks>
  /// Envolver as listas internas é o que impede o consumidor de convertê-las de volta para
  /// <c>IList</c> e alterá-las por fora.
  /// </remarks>
  private ResponseDto()
  {
    _readOnlyErrors = _errors.AsReadOnly();
    _readOnlyMetadata = _metadata.AsReadOnly();
  }

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor com dados de resposta.
  /// </summary>
  /// <remarks>
  /// Quando os dados são uma <see cref="Notification"/> inválida, as mensagens dela viram os erros da
  /// resposta e nada é atribuído a <see cref="Data"/>.
  /// </remarks>
  /// <param name="data">Dados de resposta.</param>
  public ResponseDto(T? data) : this()
  {
    // Verifica se os dados são uma notificação
    if (data is Notification notification && !notification.IsValid)
    {
      // Atribui as mensagens de erro com as strings localizadas correspondentes caso existam
      AddErrors(notification.Messages);
    }
    else
    {
      // Atribui a resposta
      Data = data;
    }
  }

  /// <summary>
  /// Construtor padrão.
  /// </summary>
  /// <param name="data">Dados de resposta.</param>
  /// <param name="errors">Lista de erros.</param>
  public ResponseDto(T data, IList<string>? errors) : this()
  {
    // Atribui os valores base
    Data = data;

    AddErrors(errors);
  }

  /// <summary>
  /// Construtor com dados de resposta.
  /// </summary>
  /// <param name="data">Dados de resposta.</param>
  /// <param name="total">Total de registros.</param>
  /// <param name="request">Requisição.</param>
  /// <exception cref="ArgumentNullException">Se a requisição for nula.</exception>
  public ResponseDto(T? data, long total, HttpRequest request) : this()
  {
    // Atribui a resposta
    Data = data;

    // Atribui os dados de paginação
    Pagination = new PaginationDto(total, request);
  }

  /// <summary>
  /// Construtor com erro.
  /// </summary>
  /// <param name="error">Mensagem de erro.</param>
  public ResponseDto(string? error) : this()
  {
    // Atribui o erro com a string localizada correspondente
    AddErrors([error]);
  }

  /// <summary>
  /// Construtor com lista de erros.
  /// </summary>
  /// <param name="errors">Lista de erros.</param>
  public ResponseDto(IList<string>? errors) : this()
  {
    // Atribui os erros com as strings localizadas correspondentes
    AddErrors(errors);
  }

  /// <summary>
  /// Construtor com exceção.
  /// </summary>
  /// <param name="exception">Exceção.</param>
  public ResponseDto(Exception? exception) : this()
  {
    // Atribui o erro da exceção com a string localizada correspondente
    AddErrors([exception?.Message]);
  }

  /// <summary>
  /// Construtor com mensagem e validador de sucesso.
  /// </summary>
  /// <remarks>
  /// Em caso de sucesso a mensagem é convertida para <typeparamref name="T"/>, o que só faz sentido quando
  /// o tipo aceita a conversão a partir de texto.
  /// </remarks>
  /// <param name="message">Mensagem de resposta.</param>
  /// <param name="isSuccess">Validador de sucesso.</param>
  /// <exception cref="InvalidCastException">Se a mensagem não puder ser convertida para o tipo da resposta.</exception>
  /// <exception cref="FormatException">Se a mensagem não estiver no formato esperado pelo tipo da resposta.</exception>
  public ResponseDto(string? message, bool isSuccess) : this()
  {
    // Verifica se a operação foi bem sucedida
    if (isSuccess)
    {
      // Converte a mensagem para o tipo da resposta; mensagem ausente não tem o que converter
      var converted = message is null ? null : Convert.ChangeType(message, typeof(T));

      // Atribui a mensagem a resposta
      Data = converted is null ? default : (T)converted;
    }
    else
    {
      // Atribui o erro com a string localizada correspondente
      AddErrors([message]);
    }
  }

  /// <summary>
  /// Construtor com lista de itens de notificação.
  /// </summary>
  /// <param name="notifications">Lista de itens de notificação.</param>
  public ResponseDto(IReadOnlyCollection<NotificationItem>? notifications) : this()
  {
    // Atribui as mensagens de erro com as strings localizadas correspondentes
    AddErrors(notifications?.Select(item => item?.Message));
  }

  /// <summary>
  /// Construtor com notificação e opção de uso de código de erro.
  /// </summary>
  /// <param name="notification">Notificação com itens de erro.</param>
  /// <param name="withCode">Indicador se mensagem é com código de erro.</param>
  public ResponseDto(Notification? notification, bool withCode) : this()
  {
    // Sem notificação não há erro a reportar
    if (notification is null)
    {
      return;
    }

    // Traduz a mensagem e, quando pedido, antepõe o código do erro
    foreach (var item in notification.Notifications)
    {
      var message = Localizer(item.Message);

      _errors.Add(withCode ? $"{item.Code}: {message}" : message);
    }
  }

  #endregion

  #region Properties

  /// <summary>
  /// Dados de resposta.
  /// </summary>
  public T? Data { get; private set; }

  /// <summary>
  /// Lista de erros.
  /// </summary>
  /// <remarks>
  /// Coleção somente leitura: os erros são definidos na construção da resposta.
  /// </remarks>
  public IReadOnlyList<string> Errors => _readOnlyErrors;

  /// <summary>
  /// Dados de paginação.
  /// </summary>
  public PaginationDto? Pagination { get; private set; }

  /// <summary>
  /// Metadados.
  /// </summary>
  /// <remarks>
  /// Coleção somente leitura: use <see cref="SetMetadata"/> ou <see cref="AddMetadata"/> para alterá-la.
  /// </remarks>
  public IReadOnlyList<MetadataDto> Metadata => _readOnlyMetadata;

  #endregion

  #region Methods

  /// <summary>
  /// Adiciona dados de paginação.
  /// </summary>
  /// <param name="pagination">Objeto de paginação.</param>
  public void SetPagination(PaginationDto? pagination)
  {
    // Atribui os dados de paginação
    Pagination = pagination;
  }

  /// <summary>
  /// Adiciona metadados.
  /// </summary>
  /// <remarks>
  /// Os itens são copiados: alterar a lista informada depois não altera a resposta.
  /// </remarks>
  /// <param name="metadata">Lista de metadados.</param>
  public void SetMetadata(IList<MetadataDto>? metadata)
  {
    // Substitui o conteúdo, mantendo a lista que a coleção somente leitura envolve
    _metadata.Clear();
    _metadata.AddRange((metadata ?? []).Where(item => item is not null));
  }

  /// <summary>
  /// Adiciona um metadado.
  /// </summary>
  /// <param name="metadata">Metadado.</param>
  public void AddMetadata(MetadataDto? metadata)
  {
    // Metadado ausente não é adicionado
    if (metadata is null)
    {
      return;
    }

    // Adiciona um metadado
    _metadata.Add(metadata);
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Acrescenta as mensagens à lista de erros, já localizadas.
  /// </summary>
  /// <param name="messages">Mensagens de erro.</param>
  private void AddErrors(IEnumerable<string?>? messages)
  {
    // Sem mensagens não há o que acrescentar
    if (messages is null)
    {
      return;
    }

    // Descarta as mensagens ausentes e localiza as demais
    foreach (var message in messages)
    {
      if (!string.IsNullOrWhiteSpace(message))
      {
        _errors.Add(Localizer(message));
      }
    }
  }

  /// <summary>
  /// Localizador de string.
  /// </summary>
  /// <param name="key">Chave da string.</param>
  /// <returns>String localizada, ou a própria chave quando ela não existe.</returns>
  private static string Localizer(string key) => LocalizerString[key];

  #endregion
}
