namespace Tooark.Mediator.Abstractions;

/// <summary>
/// Define uma requisição, que é um tipo de mensagem que espera uma resposta do tipo TResponse.
/// </summary>
/// <remarks>
/// As requisições são usadas para operações que exigem uma resposta, como comandos ou consultas
/// e geralmente são manipuladas por um único manipulador, que é responsável por processar a requisição
/// e retornar a resposta solicitada.
/// A interface é invariante em TResponse: o despacho resolve o manipulador pelo tipo de resposta
/// do ponto de chamada, então enviar uma requisição através de uma referência de tipo-base não é suportado.
/// </remarks>
/// <typeparam name="TResponse">O tipo de resposta que a requisição retorna.</typeparam>
public interface IRequest<TResponse>
{ }

/// <summary>
/// Define uma requisição que não retorna uma resposta.
/// </summary>
/// <remarks>
/// Este é um atalho para requisições que não precisam retornar um valor, usando o tipo de resposta <see cref="Unit"/>,
/// que indica a ausência de resposta.
/// </remarks>
public interface IRequest : IRequest<Unit>
{ }
