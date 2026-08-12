namespace Tooark.Mediator.Abstractions;

/// <summary>
/// Define um comando, que é um tipo de requisição que espera uma resposta do tipo TResponse.
/// </summary>
/// <remarks>
/// Os comandos são usados para operações que modificam o estado do sistema ou realizam uma ação específica,
/// e geralmente são manipulados por um único manipulador.
/// </remarks>
/// <typeparam name="TResponse">O tipo de resposta que o comando retorna.</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>
{ }

/// <summary>
/// Define um comando que não retorna uma resposta.
/// </summary>
/// <remarks>
/// Este é um atalho para comandos que não precisam retornar um valor, usando o tipo de resposta <see cref="Unit"/>.
/// </remarks>
/// <typeparam>O tipo de resposta, que é <see cref="Unit"/> para indicar que não há resposta.</typeparam>
public interface ICommand : ICommand<Unit>
{ }
