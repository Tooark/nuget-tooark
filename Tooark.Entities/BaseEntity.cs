using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tooark.Exceptions;
using Tooark.Notifications;
using Tooark.Validations.Messages;

namespace Tooark.Entities;

/// <summary>
/// Classe base abstrata para entidades.
/// </summary>
public abstract class BaseEntity : Notification, IEquatable<Guid>, IEquatable<BaseEntity>
{
  #region Constructors

  /// <summary>
  /// Construtor vazio para a entidade BaseEntity.
  /// </summary>
  /// <remarks>
  /// Utilizado pelo Entity Framework.
  /// </remarks>
  protected BaseEntity() => Id = Guid.NewGuid();

  /// <summary>
  /// Construtor para criar uma entidade com identificador definido.
  /// </summary>
  /// <remarks>
  /// Útil para cenários de seed/testes/factories onde o Id precisa ser determinístico. Não gera um
  /// identificador antes: o informado é o único atribuído.
  /// </remarks>
  /// <param name="id">Identificador único da entidade.</param>
  /// <exception cref="BadRequestException">Quando o identificador informado é vazio.</exception>
  protected BaseEntity(Guid id)
  {
    SetId(id);
  }

  #endregion

  #region Properties

  /// <summary>
  /// Identificador único para a entidade.
  /// </summary>
  /// <value>
  /// O identificador é do tipo <see cref="Guid"/> e é gerado automaticamente quando
  /// a entidade é criada.
  /// </value>
  /// <remarks>
  /// A coluna correspondente no banco de dados é 'id' é do tipo 'uuid' e é obrigatória.
  /// </remarks>
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  [Column("id", TypeName = "uuid")]
  [Required]
  public Guid Id { get; private set; }

  #endregion

  #region Protected Methods

  /// <summary>
  /// Define o identificador único para a entidade.
  /// </summary>
  /// <remarks>
  /// O Id deve ser efetivamente imutável após definido. Este método existe
  /// apenas para ser usado internamente (ex.: construtores/factories) e não
  /// permite trocar a identidade de uma entidade já criada.
  /// <para>
  /// O construtor sem parâmetros já gera um identificador, então chamar este método no construtor de uma
  /// classe derivada que não repassou o id para a base lança <c>T.ENT.BAS2</c>. Para criar a entidade com um
  /// identificador informado, use o construtor que recebe o id, que existe em cada classe base.
  /// </para>
  /// </remarks>
  /// <param name="id">O valor do identificador a ser definido.</param>
  /// <exception cref="BadRequestException">
  /// Quando o identificador informado é vazio, ou quando a entidade já tem outro identificador.
  /// </exception>
  protected void SetId(Guid id)
  {
    // Verifica se o identificador é vazio
    if (id == Guid.Empty)
    {
      throw Failure(ValidationErrorMessages.FieldEmpty("Id"), "Id", "T.ENT.BAS1");
    }

    // Não permitir mutação de identidade após definição
    if (Id != Guid.Empty && Id != id)
    {
      throw Failure(ValidationErrorMessages.FieldChangeBlocked("Id"), "Id", "T.ENT.BAS2");
    }

    Id = id;
  }

  /// <summary>
  /// Transporta uma notificação até a exceção sem registrá-la na entidade.
  /// </summary>
  private sealed class NotificationCarrier : Notification
  {
    /// <summary>
    /// Construtor que adiciona uma notificação com a mensagem, chave e código fornecidos.
    /// </summary>
    /// <param name="message">A mensagem da notificação.</param>
    /// <param name="key">A chave da notificação.</param>
    /// <param name="code">O código da notificação.</param>
    internal NotificationCarrier(string message, string key, string code) =>
      AddNotification(message, key, code);
  }

  /// <summary>
  /// Monta a exceção de requisição inválida preservando o código do erro.
  /// </summary>
  /// <param name="message">A chave da mensagem, com os parâmetros separados por ponto e vírgula.</param>
  /// <param name="key">O nome do campo ou do alvo da notificação.</param>
  /// <param name="code">O código do erro, no padrão <c>T.ENT.&lt;SIGLA&gt;&lt;N&gt;</c>.</param>
  /// <returns>A exceção pronta para ser lançada.</returns>
  protected static BadRequestException Failure(string message, string key, string code) =>
    new(new NotificationCarrier(message, key, code));

  /// <summary>
  /// Garante que o valor informado existe e é válido, sem alterar o estado da entidade.
  /// </summary>
  /// <param name="value">O objeto de valor a ser verificado.</param>
  /// <param name="field">O nome do campo, usado na mensagem quando o valor está ausente.</param>
  /// <exception cref="BadRequestException">Quando o valor está ausente ou é inválido.</exception>
  protected static void EnsureValid(Notification? value, string field)
  {
    // Verifica se o valor é nulo
    if (value is null)
    {
      throw new BadRequestException(ValidationErrorMessages.FieldRequired(field));
    }

    // Verifica se o valor é inválido
    if (!value.IsValid)
    {
      throw new BadRequestException(value);
    }
  }

  #endregion

  #region Equatable Implementation

  /// <summary>
  /// Sobrecarga do método GetHashCode.
  /// </summary>
  public override int GetHashCode() => Id.GetHashCode();

  /// <summary>
  /// Compara o identificador da entidade com outro identificador.
  /// </summary>
  /// <param name="id">O identificador a ser comparado.</param>
  /// <returns>Verdadeiro se os identificadores forem iguais, falso caso contrário.</returns>
  public bool Equals(Guid id) => Id == id;

  /// <summary>
  /// Compara a entidade atual com outra entidade.
  /// </summary>
  /// <param name="other">A outra entidade a ser comparada.</param>
  /// <returns>Verdadeiro se as entidades forem iguais, falso caso contrário.</returns>
  public bool Equals(BaseEntity? other)
  {
    // Verifica se a outra entidade é nula
    if (other is null)
    {
      return false;
    }

    // Verifica se as referências são iguais
    if (ReferenceEquals(this, other))
    {
      return true;
    }

    // Entidades de tipos diferentes não são a mesma entidade, ainda que compartilhem o identificador.
    // A comparação aceita herança nos dois sentidos para não quebrar com os proxies que o Entity
    // Framework gera para carregamento tardio, que são subclasses da entidade real.
    var type = GetType();
    var otherType = other.GetType();

    // Verifica se os tipos das entidades são compatíveis
    if (!type.IsAssignableFrom(otherType) && !otherType.IsAssignableFrom(type))
    {
      return false;
    }

    // Compara os identificadores das entidades
    return Id.Equals(other.Id);
  }

  /// <summary>
  /// Compara se a entidade atual é igual a um objeto.
  /// </summary>
  /// <param name="left">O objeto a ser comparado.</param>
  /// <param name="right">A outra entidade a ser comparada.</param>
  /// <returns>Verdadeiro se o objeto for igual à entidade, falso caso contrário.</returns>
  public static bool operator ==(BaseEntity? left, BaseEntity? right) => Equals(left, right);

  /// <summary>
  /// Compara se a entidade atual é diferente de um objeto.
  /// </summary>
  /// <param name="left">A entidade a ser comparada.</param>
  /// <param name="right">O objeto a ser comparado.</param>
  /// <returns>Verdadeiro se a entidade for diferente do objeto, falso caso contrário.</returns>
  public static bool operator !=(BaseEntity? left, BaseEntity? right) => !Equals(left, right);

  /// <summary>
  /// Compara se a entidade atual é igual a um objeto.
  /// </summary>
  /// <param name="obj">O objeto a ser comparado.</param>
  /// <returns>Verdadeiro se o objeto for igual à entidade, falso caso contrário.</returns>
  public override bool Equals(object? obj) => Equals(obj as BaseEntity);

  #endregion
}
