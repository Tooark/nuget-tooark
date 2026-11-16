namespace Tooark.Entities.EntityFrameworkCore.Interfaces;

/// <summary>
/// Interface que informa ao interceptor quem está fazendo a alteração.
/// </summary>
/// <remarks>
/// A implementação é da aplicação, porque a origem do usuário depende do host: em uma API ele vem das claims
/// da requisição, em um worker pode não existir. O pacote não traz uma implementação sobre o
/// <c>IHttpContextAccessor</c> para não depender do ASP.NET Core.
/// </remarks>
public interface ICurrentUser
{
  /// <summary>
  /// Identificador do usuário que está fazendo a alteração.
  /// </summary>
  /// <value>
  /// O identificador do usuário, ou <c>null</c> quando não há usuário autenticado.
  /// </value>
  Guid? Id { get; }
}
