using Tooark.Entities.EntityFrameworkCore.Interfaces;

namespace Tooark.Entities.EntityFrameworkCore.Options;

/// <summary>
/// Classe que representa as opções da auditoria e da exclusão lógica das entidades.
/// </summary>
public class EntityAuditOptions
{
  #region Properties

  /// <summary>
  /// Identificador usado como autor quando não há usuário atual.
  /// </summary>
  /// <value>
  /// O identificador do usuário de sistema. O padrão é <c>null</c>.
  /// </value>
  /// <remarks>
  /// Vale quando <see cref="ICurrentUser.Id"/> é nulo ou vazio, ou quando nenhum <see cref="ICurrentUser"/>
  /// está registrado: jobs em segundo plano, seeders e workers. Sem usuário atual e sem este valor, a
  /// gravação que precisa de um autor falha em vez de registrar um autor vazio.
  /// </remarks>
  public Guid? SystemUserId { get; set; }

  /// <summary>
  /// Indica se o interceptor preenche os campos de criação e de atualização.
  /// </summary>
  /// <value>
  /// O padrão é <c>true</c>.
  /// </value>
  /// <remarks>
  /// Desligado, a aplicação volta a ser responsável por chamar <c>SetCreatedBy</c> e <c>SetUpdatedBy</c>. A
  /// exclusão lógica continua registrando o autor, porque é ela quem chama <c>SetDeleted</c>.
  /// </remarks>
  public bool StampAudit { get; set; } = true;

  /// <summary>
  /// Indica se o interceptor converte a remoção em exclusão lógica.
  /// </summary>
  /// <value>
  /// O padrão é <c>true</c>.
  /// </value>
  /// <remarks>
  /// Desligado, o <c>Remove()</c> apaga a linha também nas entidades que suportam exclusão lógica.
  /// </remarks>
  public bool SoftDelete { get; set; } = true;

  #endregion
}
