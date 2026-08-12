namespace Tooark.Mediator.Abstractions;

/// <summary>
/// Define uma consulta, que é um tipo de requisição que espera uma resposta do tipo TResponse.
/// </summary>
/// <remarks>
/// As consultas são usadas para operações de leitura ou recuperação de dados, onde o objetivo é obter informações
/// do sistema sem modificar seu estado. Elas geralmente são manipuladas por um único manipulador, que é responsável
/// por processar a consulta e retornar os dados solicitados.
/// </remarks>
/// <typeparam name="TResponse">O tipo de resposta que a consulta retorna.</typeparam>
public interface IQuery<TResponse> : IRequest<TResponse>
{ }
