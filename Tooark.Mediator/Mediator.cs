using System.Collections.Concurrent;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.Options;
using Tooark.Mediator.Wrappers;

namespace Tooark.Mediator;

/// <summary>
/// Implementação do mediador.
/// </summary>
/// <remarks>
/// O mediador é responsável por enviar requisições para os manipuladores correspondentes e publicar
/// notificações para os manipuladores de notificações registrados. Ele utiliza o <see cref="IServiceProvider"/>
/// para resolver os manipuladores necessários para processar as mensagens. O despacho usa wrappers
/// genéricos em cache estático: reflection ocorre apenas na primeira chamada de cada tipo de mensagem.
/// </remarks>
/// <param name="serviceProvider">O provedor de serviços para resolver os manipuladores.</param>
/// <param name="options">As opções de configuração do mediador.</param>
public sealed class Mediator(IServiceProvider serviceProvider, MediatorOptions options) : IMediator
{
  #region Private Static Fields

  /// <summary>
  /// Cache de wrappers de requisições por tipo de requisição/resposta.
  /// </summary>
  /// <remarks>
  /// Estático porque o Mediator é registrado como transient: o cache precisa sobreviver às instâncias.
  /// </remarks>
  private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), object> _requestWrappers = new();

  /// <summary>
  /// Cache de wrappers de notificações por tipo de notificação.
  /// </summary>
  /// <remarks>
  /// Estático porque o Mediator é registrado como transient: o cache precisa sobreviver às instâncias.
  /// </remarks>
  private static readonly ConcurrentDictionary<Type, NotifyHandlerWrapper> _notifyWrappers = new();

  #endregion

  #region Private Fields

  /// <summary>
  /// O provedor de serviços para resolver os manipuladores necessários para processar as mensagens.
  /// </summary>
  private readonly IServiceProvider _serviceProvider = serviceProvider;

  /// <summary>
  /// As opções de configuração do mediador.
  /// </summary>
  private readonly MediatorOptions _options = options;

  #endregion

  #region Constructors

  /// <summary>
  /// Construtor do mediador que aceita apenas o provedor de serviços, usando as opções padrão.
  /// </summary>
  public Mediator(IServiceProvider serviceProvider) : this(serviceProvider, new MediatorOptions())
  { }

  #endregion

  #region Methods

  /// <inheritdoc/>
  public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
  {
    // Verifica se a requisição é nula e lança uma exceção personalizada se for o caso.
    if (request is null)
    {
      throw new BadRequestException("Request.Null");
    }

    // Obtém (ou cria, apenas na primeira chamada do tipo) o wrapper tipado da requisição.
    var wrapper = (RequestHandlerWrapper<TResponse>)_requestWrappers.GetOrAdd(
      (request.GetType(), typeof(TResponse)),
      static key => Activator.CreateInstance(typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(key.RequestType, key.ResponseType))
        ?? throw new InternalServerErrorException($"Handler.WrapperCreationFailed;{key.RequestType.FullName}"));

    // Despacha a requisição através do wrapper (chamada direta, sem reflection).
    return await wrapper.HandleAsync(request, _serviceProvider, cancellationToken);
  }

  /// <inheritdoc/>
  public async Task PublishAsync(INotify notify, CancellationToken cancellationToken = default)
  {
    // Verifica se a notificação é nula e lança uma exceção personalizada se for o caso.
    if (notify is null)
    {
      throw new BadRequestException("Notify.Null");
    }

    // Obtém (ou cria, apenas na primeira chamada do tipo) o wrapper tipado da notificação.
    var wrapper = _notifyWrappers.GetOrAdd(
      notify.GetType(),
      static type => (NotifyHandlerWrapper)(Activator.CreateInstance(typeof(NotifyHandlerWrapperImpl<>).MakeGenericType(type))
        ?? throw new InternalServerErrorException($"Handler.WrapperCreationFailed;{type.FullName}")));

    // Publica a notificação através do wrapper conforme a estratégia configurada (chamada direta, sem reflection).
    await wrapper.PublishAsync(notify, _serviceProvider, _options.NotifyPublishStrategy, cancellationToken);
  }

  #endregion
}
