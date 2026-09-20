using System.Globalization;
using System.Text.Json;
using Tooark.Entities;
using Tooark.Exceptions;
using Tooark.Extensions;
using Tooark.ValueObjects;

namespace Tooark.Tests.Entities;

/// <summary>
/// Testes das correções feitas na revisão da v4.0.0 do Tooark.Entities.
/// </summary>
public class EntitiesReviewTests
{
  #region Dublês

  private sealed class Auditavel : AuditableEntity
  {
    public Auditavel() { }
    public Auditavel(Guid createdBy) : base(createdBy) { }
  }

  private sealed class Versionada : VersionedEntity
  {
    public Versionada() { }
    public Versionada(Guid createdBy) : base(createdBy) { }
  }

  private sealed class Excluivel : SoftDeletableEntity
  {
    public Excluivel() { }
    public Excluivel(Guid createdBy) : base(createdBy) { }
  }

  private sealed class Detalhada : DetailedEntity
  {
    public Detalhada(Guid createdBy) : base(createdBy) { }
  }

  private sealed class ComId : BaseEntity
  {
    public ComId(Guid id) : base(id) { }
  }

  private sealed class OutraComId : BaseEntity
  {
    public OutraComId(Guid id) : base(id) { }
  }

  #endregion

  #region A versão precisa incrementar também pela referência da classe base

  // Testa se SetUpdatedBy incrementa a versão quando chamado por referência de DetailedEntity.
  // Com 'new' no lugar de 'override', esta chamada executava o método da base e não incrementava.
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void SetUpdatedBy_ShouldIncrementVersion_WhenCalledThroughBaseReference(bool auditavel)
  {
    // Arrange
    var criador = Guid.NewGuid();
    DetailedEntity entidade = auditavel ? new Auditavel(criador) : new Versionada(criador);

    // Act
    entidade.SetUpdatedBy(Guid.NewGuid());

    // Assert
    var versao = auditavel ? ((Auditavel)entidade).Version : ((Versionada)entidade).Version;
    Assert.Equal(2, versao);
  }

  // Testa se as duas implementações de versionamento concordam sobre o mesmo roteiro.
  // AuditableEntity e VersionedEntity não podem herdar uma da outra, então a duplicação é
  // deliberada — este teste existe para que as cópias não voltem a divergir.
  [Fact]
  public void VersionedAndAuditable_ShouldAgree_OnVersionIncrement()
  {
    // Arrange
    var criador = Guid.NewGuid();
    var auditavel = new Auditavel(criador);
    var versionada = new Versionada(criador);

    // Act
    foreach (var _ in Enumerable.Range(0, 3))
    {
      var autor = Guid.NewGuid();
      auditavel.SetUpdatedBy(autor);
      versionada.SetUpdatedBy(autor);
    }

    // Assert
    Assert.Equal(versionada.Version, auditavel.Version);
    Assert.Equal(4, auditavel.Version);
  }

  #endregion

  #region Chamada recusada não pode deixar a entidade inutilizável

  // Testa se uma chamada recusada preserva a entidade utilizável para a chamada seguinte.
  // A validação era acumulada na entidade, então a segunda chamada — correta — era recusada
  // com a mensagem da primeira, e a entidade nunca mais podia ser excluída.
  [Fact]
  public void SetDeleted_ShouldStillWork_AfterARejectedCall()
  {
    // Arrange
    var entidade = new Auditavel(Guid.NewGuid());

    // Act
    Assert.Throws<BadRequestException>(() => entidade.SetDeleted(new DeletedBy(Guid.Empty)));
    entidade.SetDeleted(new DeletedBy(Guid.NewGuid()));

    // Assert
    Assert.True(entidade.IsValid);
    Assert.True(entidade.Deleted);
  }

  // Testa se o mesmo vale para as demais operações de escrita
  [Fact]
  public void SetUpdatedBy_ShouldStillWork_AfterARejectedCall()
  {
    // Arrange
    var entidade = new Detalhada(Guid.NewGuid());
    var autor = Guid.NewGuid();

    // Act
    Assert.Throws<BadRequestException>(() => entidade.SetUpdatedBy(new UpdatedBy(Guid.Empty)));
    entidade.SetUpdatedBy(new UpdatedBy(autor));

    // Assert
    Assert.True(entidade.IsValid);
    Assert.Equal(autor, entidade.UpdatedById);
  }

  // Testa se EnsureNotDeleted não é afetado por notificação anterior da própria entidade
  [Fact]
  public void EnsureNotDeleted_ShouldNotThrow_WhenEntityHasUnrelatedNotification()
  {
    // Arrange
    var entidade = new Excluivel(Guid.NewGuid());
    entidade.SetDeleted(Guid.NewGuid());
    entidade.SetRestored(Guid.NewGuid());

    // Act: deixa uma notificação registrada por outro caminho
    entidade.ValidateNotDeleted();

    // Assert
    Assert.False(entidade.Deleted);
    entidade.EnsureNotDeleted();
  }

  #endregion

  #region Valor ausente é campo obrigatório, e não formato inválido

  // Testa se argumento nulo produz a mensagem do campo, e não uma mensagem interna do framework
  [Fact]
  public void WriteOperations_ShouldReportMissingField_WhenArgumentIsNull()
  {
    // Arrange
    var auditavel = new Auditavel(Guid.NewGuid());
    var detalhada = new Detalhada(Guid.NewGuid());
    var inicial = new Auditavel();

    // Act & Assert
    Assert.Contains("Field.Required;DeletedBy",
      Assert.Throws<BadRequestException>(() => auditavel.SetDeleted(null!)).GetErrorMessages());
    Assert.Contains("Field.Required;RestoredBy",
      Assert.Throws<BadRequestException>(() => auditavel.SetRestored(null!)).GetErrorMessages());
    Assert.Contains("Field.Required;UpdatedBy",
      Assert.Throws<BadRequestException>(() => detalhada.SetUpdatedBy(null!)).GetErrorMessages());
    Assert.Contains("Field.Required;CreatedBy",
      Assert.Throws<BadRequestException>(() => inicial.SetCreatedBy(null!)).GetErrorMessages());
  }

  #endregion

  #region Excluir e restaurar são alterações, e precisam constar na auditoria

  // Testa se SetDeleted registra quem alterou. A entidade de auditoria completa era justamente
  // a que não atualizava UpdatedById nem UpdatedAt ao excluir.
  [Fact]
  public void SetDeleted_ShouldRecordWhoChanged()
  {
    // Arrange
    var entidade = new Auditavel(Guid.NewGuid());
    var anterior = entidade.UpdatedById;
    var autor = Guid.NewGuid();

    // Act
    entidade.SetDeleted(new DeletedBy(autor));

    // Assert
    Assert.NotEqual(anterior, entidade.UpdatedById);
    Assert.Equal(autor, entidade.UpdatedById);
    Assert.Equal(autor, entidade.DeletedById);
    Assert.Equal(2, entidade.Version);
  }

  // Testa se SetRestored faz o mesmo
  [Fact]
  public void SetRestored_ShouldRecordWhoChanged()
  {
    // Arrange
    var entidade = new Auditavel(Guid.NewGuid());
    entidade.SetDeleted(new DeletedBy(Guid.NewGuid()));
    var autor = Guid.NewGuid();

    // Act
    entidade.SetRestored(new RestoredBy(autor));

    // Assert
    Assert.Equal(autor, entidade.UpdatedById);
    Assert.Equal(autor, entidade.RestoredById);
    Assert.Equal(3, entidade.Version);
  }

  // Testa se a exclusão em AuditableEntity e em SoftDeletableEntity registra o autor do mesmo jeito
  [Fact]
  public void SoftDeletableAndAuditable_ShouldAgree_OnRecordingTheAuthor()
  {
    // Arrange
    var criador = Guid.NewGuid();
    var autor = Guid.NewGuid();
    var auditavel = new Auditavel(criador);
    var excluivel = new Excluivel(criador);

    // Act
    auditavel.SetDeleted(new DeletedBy(autor));
    excluivel.SetDeleted(new UpdatedBy(autor));

    // Assert
    Assert.True(auditavel.Deleted);
    Assert.True(excluivel.Deleted);
    Assert.Equal(autor, auditavel.UpdatedById);
    Assert.Equal(autor, excluivel.UpdatedById);
  }

  #endregion

  #region Repetir a operação não faz nada, e não avisa

  // Testa se excluir uma entidade já excluída é ignorado em silêncio, sem tocar na versão nem no
  // registro de quem excluiu. É comportamento idempotente de propósito, e vale para as duas classes.
  [Fact]
  public void SetDeleted_ShouldBeIgnored_WhenAlreadyDeleted()
  {
    // Arrange
    var auditavel = new Auditavel(Guid.NewGuid());
    var excluivel = new Excluivel(Guid.NewGuid());
    var primeiro = Guid.NewGuid();
    auditavel.SetDeleted(new DeletedBy(primeiro));
    excluivel.SetDeleted(new UpdatedBy(primeiro));

    // Act
    auditavel.SetDeleted(new DeletedBy(Guid.NewGuid()));
    excluivel.SetDeleted(new UpdatedBy(Guid.NewGuid()));

    // Assert
    Assert.Equal(primeiro, auditavel.DeletedById);
    Assert.Equal(primeiro, auditavel.UpdatedById);
    Assert.Equal(primeiro, excluivel.UpdatedById);
    Assert.Equal(2, auditavel.Version);
  }

  // Testa se restaurar uma entidade que não está excluída é ignorado em silêncio
  [Fact]
  public void SetRestored_ShouldBeIgnored_WhenNotDeleted()
  {
    // Arrange
    var auditavel = new Auditavel(Guid.NewGuid());
    var excluivel = new Excluivel(Guid.NewGuid());
    var criador = auditavel.UpdatedById;
    var criadorExcluivel = excluivel.UpdatedById;

    // Act
    auditavel.SetRestored(new RestoredBy(Guid.NewGuid()));
    excluivel.SetRestored(new UpdatedBy(Guid.NewGuid()));

    // Assert
    Assert.Null(auditavel.RestoredById);
    Assert.Equal(criador, auditavel.UpdatedById);
    Assert.Equal(criadorExcluivel, excluivel.UpdatedById);
    Assert.Equal(1, auditavel.Version);
  }

  #endregion

  #region A criação nasce com as duas marcas iguais

  // Testa se a criação define a data de atualização junto, em vez de deixá-la com o valor de
  // inicialização do campo
  [Fact]
  public void SetCreatedBy_ShouldAlignUpdatedAt_WithCreatedAt()
  {
    // Arrange & Act
    var entidade = new Detalhada(Guid.NewGuid());

    // Assert
    Assert.Equal(entidade.CreatedAt, entidade.UpdatedAt);
    Assert.Equal(entidade.CreatedById, entidade.UpdatedById);
  }

  #endregion

  #region Identidade

  // Testa se entidades de tipos diferentes com o mesmo identificador deixam de ser iguais
  [Fact]
  public void Equals_ShouldBeFalse_WhenTypesDiffer()
  {
    // Arrange
    var id = Guid.NewGuid();
    var uma = new ComId(id);
    var outra = new OutraComId(id);

    // Act & Assert
    Assert.False(uma.Equals(outra));
    Assert.False(outra.Equals(uma));
    Assert.False(uma == outra);
    Assert.True(uma != outra);
    // Antes da correção o Distinct colapsava as duas em uma, descartando um registro
    Assert.Equal(2, new BaseEntity[] { uma, outra }.Distinct().Count());
  }

  // Testa se entidades do mesmo tipo com o mesmo identificador seguem iguais
  [Fact]
  public void Equals_ShouldBeTrue_WhenTypeAndIdMatch()
  {
    // Arrange
    var id = Guid.NewGuid();
    var uma = new ComId(id);
    var outra = new ComId(id);

    // Act & Assert
    Assert.True(uma.Equals(outra));
    Assert.True(uma == outra);
    Assert.Equal(uma.GetHashCode(), outra.GetHashCode());
  }

  #endregion

  #region As chaves emitidas precisam existir nos arquivos de idioma

  // Testa se toda chave de mensagem emitida pelo pacote tem tradução nos três idiomas.
  // 'Empty;Id' e 'ChangeBlocked;Id' não existiam em nenhum arquivo, e a chave crua chegava
  // ao consumidor da API.
  [Theory]
  [InlineData("en-US")]
  [InlineData("es-ES")]
  [InlineData("pt-BR")]
  public void EveryEmittedKey_ShouldHaveTranslation(string idioma)
  {
    // Arrange: lê o recurso embutido no assembly, que é o que chega ao consumidor
    var assembly = typeof(JsonStringLocalizerExtension).Assembly;

    using var recurso = assembly.GetManifestResourceStream(
      $"{assembly.GetName().Name}.Resources.{idioma}.default.json")!;

    var traducoes = JsonSerializer.Deserialize<Dictionary<string, string>>(recurso)!;

    string[] chaves = [
      "Field.Empty",
      "Field.ChangeBlocked",
      "Field.Required",
      "Field.Invalid",
      "Record.Deleted"
    ];

    // Act & Assert
    foreach (var chave in chaves)
    {
      Assert.True(traducoes.ContainsKey(chave), $"{idioma} não tem a chave {chave}");
      Assert.False(string.IsNullOrWhiteSpace(traducoes[chave]));
    }
  }

  // Testa se a mensagem do identificador vazio chega traduzida, e não como chave
  [Fact]
  public void EmptyId_ShouldProduceATranslatableKey()
  {
    // Arrange
    var anterior = CultureInfo.CurrentCulture;

    try
    {
      CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

      // Act
      var entidade = new ComId(Guid.Empty);

      // Assert
      Assert.False(entidade.IsValid);
      Assert.Equal("Field.Empty;Id", entidade.Messages.First());
    }
    finally
    {
      CultureInfo.CurrentCulture = anterior;
    }
  }

  #endregion

  #region Cada emissor tem o próprio código

  // Testa se a exceção preserva o código do erro. A validação deixou de ser acumulada na entidade,
  // e o construtor de BadRequestException que recebe texto não carrega código — sem o transportador,
  // T.ENT.INI1, T.ENT.AUD1 e T.ENT.SOF1 sumiriam das exceções sem ninguém notar.
  [Fact]
  public void Exceptions_ShouldCarryTheirNotificationCode()
  {
    // Arrange
    var jaCriada = new Auditavel(Guid.NewGuid());
    var auditavelExcluida = new Auditavel(Guid.NewGuid());
    var excluivelExcluida = new Excluivel(Guid.NewGuid());
    auditavelExcluida.SetDeleted(new DeletedBy(Guid.NewGuid()));
    excluivelExcluida.SetDeleted(new UpdatedBy(Guid.NewGuid()));

    // Act
    var criacao = Assert.Throws<BadRequestException>(() => jaCriada.SetCreatedBy(Guid.NewGuid()));
    var auditavel = Assert.Throws<BadRequestException>(auditavelExcluida.EnsureNotDeleted);
    var excluivel = Assert.Throws<BadRequestException>(excluivelExcluida.EnsureNotDeleted);

    // Assert
    Assert.Equal("T.ENT.INI1", criacao.GetNotifications().Single().Code);
    Assert.Equal("T.ENT.AUD1", auditavel.GetNotifications().Single().Code);
    Assert.Equal("T.ENT.SOF1", excluivel.GetNotifications().Single().Code);

    // A entidade segue sem guardar nada das chamadas recusadas
    Assert.True(jaCriada.IsValid);
    Assert.True(auditavelExcluida.IsValid);
  }

  // Testa se SoftDeletableEntity deixou de usar o código do AuditableEntity
  [Fact]
  public void ValidateNotDeleted_ShouldUseItsOwnCode()
  {
    // Arrange
    var auditavel = new Auditavel(Guid.NewGuid());
    var excluivel = new Excluivel(Guid.NewGuid());
    auditavel.SetDeleted(new DeletedBy(Guid.NewGuid()));
    excluivel.SetDeleted(new UpdatedBy(Guid.NewGuid()));

    // Act
    auditavel.ValidateNotDeleted();
    excluivel.ValidateNotDeleted();

    // Assert
    Assert.Equal("T.ENT.AUD1", auditavel.Notifications.First().Code);
    Assert.Equal("T.ENT.SOF1", excluivel.Notifications.First().Code);
  }

  #endregion
}
