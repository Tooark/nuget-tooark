using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Entities.EntityFrameworkCore.Extensions;
using Tooark.Entities.EntityFrameworkCore.Injections;
using Tooark.Entities.EntityFrameworkCore.Interfaces;
using Tooark.Exceptions;
using Tooark.Mediator.Abstractions;
using Tooark.Mediator.EntityFrameworkCore.Injections;
using Tooark.Mediator.Handlers;

namespace Tooark.Tests.Entities.EntityFrameworkCore;

public class TooarkEntityInterceptorTests : EntityFrameworkCoreTestBase
{
  #region Criação

  // Testa se a entidade adicionada sem autor recebe o usuário atual, pelos caminhos síncrono e assíncrono
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Add_ShouldStampCreation_WhenCreatedByIsEmpty(bool async)
  {
    // Arrange
    var provider = BuildProvider();
    var context = NewContext(provider);

    // Act
    context.Articles.Add(new Article { Title = "Novidades" });
    await SaveAsync(context, async);

    // Assert - a criação também é a primeira atualização
    var reloaded = await NewContext(provider).Articles.SingleAsync(Token);

    Assert.Equal(UserId, reloaded.CreatedById);
    Assert.Equal(UserId, reloaded.UpdatedById);
    Assert.Equal(reloaded.CreatedAt, reloaded.UpdatedAt);
  }

  // Testa se a entidade que a aplicação já criou com autor não é carimbada de novo, o que lançaria
  [Fact]
  public async Task Add_ShouldKeepCreation_WhenApplicationAlreadySetCreatedBy()
  {
    // Arrange
    var provider = BuildProvider();
    var context = NewContext(provider);

    // Act
    context.Articles.Add(new Article(OtherUserId) { Title = "Novidades" });
    await context.SaveChangesAsync(Token);

    // Assert
    var reloaded = await NewContext(provider).Articles.SingleAsync(Token);

    Assert.Equal(OtherUserId, reloaded.CreatedById);
    Assert.Equal(OtherUserId, reloaded.UpdatedById);
  }

  // Testa se as entidades que só têm os campos de criação (InitialEntity e FileEntity) são carimbadas
  [Fact]
  public async Task Add_ShouldStampCreation_WhenEntityHasOnlyCreationFields()
  {
    // Arrange
    var provider = BuildProvider();
    var context = NewContext(provider);

    // Act
    context.Notes.Add(new Note { Text = "Lembrete" });
    context.Attachments.Add(new Attachment());
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);

    Assert.Equal(UserId, (await verification.Notes.SingleAsync(Token)).CreatedById);
    Assert.Equal(UserId, (await verification.Attachments.SingleAsync(Token)).CreatedById);
  }

  // Testa se a criação não incrementa a versão
  [Fact]
  public async Task Add_ShouldKeepVersionOne_WhenEntityIsVersioned()
  {
    // Arrange
    var provider = BuildProvider();
    var context = NewContext(provider);

    // Act
    context.Documents.Add(new Document { Title = "Contrato" });
    context.Orders.Add(new Order { Number = "P-1" });
    await context.SaveChangesAsync(Token);

    // Assert
    var verification = NewContext(provider);

    Assert.Equal(1, (await verification.Documents.SingleAsync(Token)).Version);
    Assert.Equal(1, (await verification.Orders.SingleAsync(Token)).Version);
  }

  #endregion

  #region Atualização

  // Testa se a entidade modificada recebe o usuário atual como autor da atualização
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Update_ShouldStampUpdate_WhenEntityIsDetailed(bool async)
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Articles.Add(new Article { Title = "Novidades" });
    await SaveAsync(seed, async);

    // Act - outro usuário altera o artigo
    user.Id = OtherUserId;

    var context = NewContext(provider);
    (await context.Articles.SingleAsync(Token)).Title = "Atualizado";
    await SaveAsync(context, async);

    // Assert - os valores vêm da linha relida, e não da instância em memória
    var reloaded = await NewContext(provider).Articles.SingleAsync(Token);

    Assert.Equal("Atualizado", reloaded.Title);
    Assert.Equal(UserId, reloaded.CreatedById);
    Assert.Equal(OtherUserId, reloaded.UpdatedById);
    Assert.True(reloaded.UpdatedAt > reloaded.CreatedAt);
  }

  // Testa se a entidade só com os campos de criação é atualizada sem carimbo e sem precisar de usuário
  [Fact]
  public async Task Update_ShouldNotNeedUser_WhenEntityHasOnlyCreationFields()
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Notes.Add(new Note { Text = "Lembrete" });
    await seed.SaveChangesAsync(Token);

    // Act
    user.Id = null;

    var context = NewContext(provider);
    (await context.Notes.SingleAsync(Token)).Text = "Alterado";
    await context.SaveChangesAsync(Token);

    // Assert
    var reloaded = await NewContext(provider).Notes.SingleAsync(Token);

    Assert.Equal("Alterado", reloaded.Text);
    Assert.Equal(UserId, reloaded.CreatedById);
  }

  // Testa se a versão sobe uma única vez quando o handler não carimba a entidade
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Update_ShouldIncrementVersionOnce_WhenHandlerDoesNotStamp(bool async)
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    seed.Orders.Add(new Order { Number = "P-1" });
    await SaveAsync(seed, async);

    // Act
    user.Id = OtherUserId;

    var context = NewContext(provider);
    (await context.Documents.SingleAsync(Token)).Title = "Aditivo";
    (await context.Orders.SingleAsync(Token)).Number = "P-2";
    await SaveAsync(context, async);

    // Assert
    var verification = NewContext(provider);
    var document = await verification.Documents.SingleAsync(Token);
    var order = await verification.Orders.SingleAsync(Token);

    Assert.Equal(2, document.Version);
    Assert.Equal(OtherUserId, document.UpdatedById);
    Assert.Equal(2, order.Version);
    Assert.Equal(OtherUserId, order.UpdatedById);
  }

  // Testa se a versão sobe uma única vez quando o handler já chamou SetUpdatedBy
  [Fact]
  public async Task Update_ShouldIncrementVersionOnce_WhenHandlerAlreadyStamped()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    seed.Orders.Add(new Order { Number = "P-1" });
    await seed.SaveChangesAsync(Token);

    // Act - o handler carimba com um autor diferente do usuário atual
    var context = NewContext(provider);
    var handled = await context.Documents.SingleAsync(Token);
    handled.Title = "Aditivo";
    handled.SetUpdatedBy(OtherUserId);

    var handledOrder = await context.Orders.SingleAsync(Token);
    handledOrder.Number = "P-2";
    handledOrder.SetUpdatedBy(OtherUserId);

    await context.SaveChangesAsync(Token);

    // Assert - o carimbo do handler é mantido, sem um segundo incremento
    var verification = NewContext(provider);
    var document = await verification.Documents.SingleAsync(Token);
    var order = await verification.Orders.SingleAsync(Token);

    Assert.Equal(2, document.Version);
    Assert.Equal(OtherUserId, document.UpdatedById);
    Assert.Equal(2, order.Version);
    Assert.Equal(OtherUserId, order.UpdatedById);
  }

  // Testa se salvar duas vezes no mesmo contexto incrementa a versão uma vez por gravação que alterou algo
  [Fact]
  public async Task Update_ShouldIncrementVersionOncePerSave_WhenSavingTwiceInTheSameContext()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    await seed.SaveChangesAsync(Token);

    var context = NewContext(provider);
    var document = await context.Documents.SingleAsync(Token);

    // Act & Assert - primeira alteração
    document.Title = "Aditivo 1";
    await context.SaveChangesAsync(Token);

    Assert.Equal(2, (await NewContext(provider).Documents.SingleAsync(Token)).Version);

    // Act & Assert - gravação sem alteração no meio
    await context.SaveChangesAsync(Token);

    Assert.Equal(2, (await NewContext(provider).Documents.SingleAsync(Token)).Version);

    // Act & Assert - segunda alteração
    document.Title = "Aditivo 2";
    await context.SaveChangesAsync(Token);

    var reloaded = await NewContext(provider).Documents.SingleAsync(Token);

    Assert.Equal(3, reloaded.Version);
    Assert.Equal("Aditivo 2", reloaded.Title);
  }

  // Testa se a entidade carregada e salva sem alteração real não é carimbada
  [Fact]
  public async Task Update_ShouldNotStamp_WhenNothingChanged()
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    await seed.SaveChangesAsync(Token);

    var before = await NewContext(provider).Documents.SingleAsync(Token);

    // Act - atribuir o mesmo valor não é uma alteração
    user.Id = OtherUserId;

    var context = NewContext(provider);
    (await context.Documents.SingleAsync(Token)).Title = "Contrato";
    await context.SaveChangesAsync(Token);

    // Assert
    var reloaded = await NewContext(provider).Documents.SingleAsync(Token);

    Assert.Equal(1, reloaded.Version);
    Assert.Equal(UserId, reloaded.UpdatedById);
    Assert.Equal(before.UpdatedAt, reloaded.UpdatedAt);
  }

  // Testa se o carimbo chega ao UPDATE mesmo com a detecção automática de alterações desligada
  [Fact]
  public async Task Update_ShouldStamp_WhenAutoDetectChangesIsDisabled()
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    await seed.SaveChangesAsync(Token);

    // Act - com a detecção manual, o SaveChanges não detecta o que o interceptor alterou
    user.Id = OtherUserId;

    var context = NewContext(provider);
    context.ChangeTracker.AutoDetectChangesEnabled = false;
    (await context.Documents.SingleAsync(Token)).Title = "Aditivo";
    context.ChangeTracker.DetectChanges();
    await context.SaveChangesAsync(Token);

    // Assert
    var reloaded = await NewContext(provider).Documents.SingleAsync(Token);

    Assert.Equal("Aditivo", reloaded.Title);
    Assert.Equal(2, reloaded.Version);
    Assert.Equal(OtherUserId, reloaded.UpdatedById);
  }

  // Testa se a entidade desconectada anexada com Update é carimbada, ainda que todas as propriedades venham marcadas
  [Fact]
  public async Task Update_ShouldStamp_WhenDetachedEntityIsAttachedWithUpdate()
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    await seed.SaveChangesAsync(Token);

    var detached = await NewContext(provider).Documents.AsNoTracking().SingleAsync(Token);

    // Act
    user.Id = OtherUserId;
    detached.Title = "Aditivo";

    var context = NewContext(provider);
    context.Documents.Update(detached);
    await context.SaveChangesAsync(Token);

    // Assert
    var reloaded = await NewContext(provider).Documents.SingleAsync(Token);

    Assert.Equal("Aditivo", reloaded.Title);
    Assert.Equal(2, reloaded.Version);
    Assert.Equal(OtherUserId, reloaded.UpdatedById);
  }

  // Testa a limitação da entidade desconectada que o handler carimbou antes de anexar: ela é carimbada de novo
  [Fact]
  public async Task Update_ShouldStampAgain_WhenDetachedEntityWasStampedBeforeUpdate()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Articles.Add(new Article { Title = "Novidades" });
    await seed.SaveChangesAsync(Token);

    var detached = await NewContext(provider).Articles.AsNoTracking().SingleAsync(Token);

    // Act - o Update não deixa no rastreio o valor original que denunciaria o carimbo do handler
    detached.Title = "Atualizado";
    detached.SetUpdatedBy(OtherUserId);

    var context = NewContext(provider);
    context.Articles.Update(detached);
    await context.SaveChangesAsync(Token);

    // Assert - o autor informado pelo handler foi substituído pelo usuário atual
    var reloaded = await NewContext(provider).Articles.SingleAsync(Token);

    Assert.Equal(UserId, reloaded.UpdatedById);
  }

  // Testa se a entidade versionada desconectada, carimbada antes de anexar, falha na concorrência
  [Fact]
  public async Task Update_ShouldThrowConcurrency_WhenDetachedVersionedEntityWasStampedBeforeUpdate()
  {
    // Arrange
    var provider = BuildProvider();

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    await seed.SaveChangesAsync(Token);

    var detached = await NewContext(provider).Documents.AsNoTracking().SingleAsync(Token);

    // Act - a versão que o Update toma como original já não é a que está no banco
    detached.Title = "Aditivo";
    detached.SetUpdatedBy(OtherUserId);

    var context = NewContext(provider);
    context.Documents.Update(detached);

    // Assert
    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync(Token));
  }

  // Testa a limitação das partes e dos filhos: a alteração só neles não modifica o dono, que fica sem carimbo
  [Fact]
  public async Task Update_ShouldNotStampOwner_WhenOnlyOwnedTypeOrChildChanged()
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Orders.Add(new Order
    {
      Number = "P-1",
      Address = new Address { Street = "Rua A", City = "Recife" },
      Items = [new OrderItem { Name = "Caneta" }]
    });
    await seed.SaveChangesAsync(Token);

    // Act
    user.Id = OtherUserId;

    var context = NewContext(provider);
    var order = await context.Orders.Include(o => o.Items).SingleAsync(Token);
    order.Address!.City = "Olinda";
    order.Items.Single().Name = "Lápis";
    await context.SaveChangesAsync(Token);

    // Assert - a parte e o filho foram gravados, o filho foi carimbado e o dono ficou como estava
    var reloaded = await NewContext(provider).Orders.Include(o => o.Items).SingleAsync(Token);

    Assert.Equal("Olinda", reloaded.Address!.City);
    Assert.Equal("Lápis", reloaded.Items.Single().Name);
    Assert.Equal(OtherUserId, reloaded.Items.Single().UpdatedById);
    Assert.Equal(1, reloaded.Version);
    Assert.Equal(UserId, reloaded.UpdatedById);
  }

  // Testa se a entidade que não é do Tooark passa pelo interceptor sem carimbo e sem precisar de usuário
  [Fact]
  public async Task Save_ShouldIgnoreEntity_WhenItIsNotATooarkEntity()
  {
    // Arrange
    var provider = BuildProvider(new TestCurrentUser(null));

    // Act
    var context = NewContext(provider);
    var setting = new Setting { Value = "a" };
    context.Settings.Add(setting);
    await context.SaveChangesAsync(Token);

    setting.Value = "b";
    await context.SaveChangesAsync(Token);

    // Assert
    Assert.Equal("b", (await NewContext(provider).Settings.SingleAsync(Token)).Value);

    // Act
    context.Settings.Remove(setting);
    await context.SaveChangesAsync(Token);

    // Assert
    Assert.Equal(0, await NewContext(provider).Settings.CountAsync(Token));
  }

  #endregion

  #region Opções

  // Testa se o carimbo desligado deixa a criação e a atualização por conta da aplicação
  [Fact]
  public async Task Save_ShouldNotStamp_WhenStampAuditIsDisabled()
  {
    // Arrange
    var provider = BuildProvider(options => options.StampAudit = false);

    // Act
    var context = NewContext(provider);
    var document = new Document { Title = "Contrato" };
    context.Documents.Add(document);
    await context.SaveChangesAsync(Token);

    // Assert - a entidade criada sem autor é gravada sem autor
    var created = await NewContext(provider).Documents.SingleAsync(Token);

    Assert.Equal(Guid.Empty, created.CreatedById);

    // Act
    document.Title = "Aditivo";
    await context.SaveChangesAsync(Token);

    // Assert
    var updated = await NewContext(provider).Documents.SingleAsync(Token);

    Assert.Equal("Aditivo", updated.Title);
    Assert.Equal(1, updated.Version);
    Assert.Equal(Guid.Empty, updated.UpdatedById);
    Assert.Equal(created.UpdatedAt, updated.UpdatedAt);
  }

  #endregion

  #region Usuário

  // Testa se a gravação que precisa de autor falha sem usuário atual e sem usuário de sistema
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Save_ShouldThrow_WhenThereIsNoUser(bool async)
  {
    // Arrange
    var provider = BuildProvider(new TestCurrentUser(null));
    var context = NewContext(provider);

    context.Articles.Add(new Article { Title = "Novidades" });

    // Act
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(() => SaveAsync(context, async));

    // Assert - nada foi gravado
    Assert.Contains("Entities.Audit.CurrentUserRequired", exception.Message);
    Assert.Equal(0, await NewContext(provider).Articles.CountAsync(Token));
  }

  // Testa se o identificador vazio do usuário atual e o do usuário de sistema contam como ausentes
  [Fact]
  public async Task Save_ShouldThrow_WhenUserAndSystemUserAreEmpty()
  {
    // Arrange
    var provider = BuildProvider(new TestCurrentUser(Guid.Empty), options => options.SystemUserId = Guid.Empty);
    var context = NewContext(provider);

    context.Articles.Add(new Article { Title = "Novidades" });

    // Act
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(() => context.SaveChangesAsync(Token));

    // Assert
    Assert.Contains("Entities.Audit.CurrentUserRequired", exception.Message);
  }

  // Testa se a atualização também falha sem usuário, e se a nova tentativa com usuário carimba uma única vez
  [Fact]
  public async Task Save_ShouldStampOnce_WhenRetriedAfterMissingUser()
  {
    // Arrange
    var user = new TestCurrentUser(UserId);
    var provider = BuildProvider(user);

    var seed = NewContext(provider);
    seed.Documents.Add(new Document { Title = "Contrato" });
    await seed.SaveChangesAsync(Token);

    user.Id = null;

    var context = NewContext(provider);
    (await context.Documents.SingleAsync(Token)).Title = "Aditivo";

    // Act
    var exception = await Assert.ThrowsAsync<InternalServerErrorException>(() => context.SaveChangesAsync(Token));

    user.Id = OtherUserId;
    await context.SaveChangesAsync(Token);

    // Assert
    var reloaded = await NewContext(provider).Documents.SingleAsync(Token);

    Assert.Contains("Entities.Audit.CurrentUserRequired", exception.Message);
    Assert.Equal(2, reloaded.Version);
    Assert.Equal(OtherUserId, reloaded.UpdatedById);
  }

  // Testa se o usuário de sistema é usado quando o usuário atual não tem identificador
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Save_ShouldUseSystemUser_WhenCurrentUserHasNoId(bool emptyId)
  {
    // Arrange
    var provider = BuildProvider(new TestCurrentUser(emptyId ? Guid.Empty : null), options => options.SystemUserId = SystemUserId);
    var context = NewContext(provider);

    // Act
    context.Articles.Add(new Article { Title = "Novidades" });
    await context.SaveChangesAsync(Token);

    // Assert
    Assert.Equal(SystemUserId, (await NewContext(provider).Articles.SingleAsync(Token)).CreatedById);
  }

  // Testa se o usuário de sistema basta quando nenhum usuário atual está registrado, como em um worker
  [Fact]
  public async Task Save_ShouldUseSystemUser_WhenCurrentUserIsNotRegistered()
  {
    // Arrange
    var provider = BuildProvider(user: null, options => options.SystemUserId = SystemUserId);

    var seed = NewContext(provider);
    seed.Orders.Add(new Order { Number = "P-1" });
    await seed.SaveChangesAsync(Token);

    // Act
    var context = NewContext(provider);
    context.Orders.Remove(await context.Orders.SingleAsync(Token));
    await context.SaveChangesAsync(Token);

    // Assert
    var reloaded = await NewContext(provider).Orders.IgnoreQueryFilters().SingleAsync(Token);

    Assert.Equal(SystemUserId, reloaded.CreatedById);
    Assert.Equal(SystemUserId, reloaded.DeletedById);
  }

  // Testa se o usuário atual prevalece sobre o usuário de sistema
  [Fact]
  public async Task Save_ShouldPreferCurrentUser_WhenSystemUserIsAlsoConfigured()
  {
    // Arrange
    var provider = BuildProvider(new TestCurrentUser(UserId), options => options.SystemUserId = SystemUserId);
    var context = NewContext(provider);

    // Act
    context.Articles.Add(new Article { Title = "Novidades" });
    await context.SaveChangesAsync(Token);

    // Assert
    Assert.Equal(UserId, (await NewContext(provider).Articles.SingleAsync(Token)).CreatedById);
  }

  #endregion

  #region Unidade de trabalho do Mediator

  // Testa se o handler que carimba a entidade e deixa a gravação para o pipeline recebe um único incremento
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task UnitOfWork_ShouldIncrementVersionOnce_WhenPipelineSaves(bool handlerStamps)
  {
    // Arrange
    var services = new ServiceCollection();

    services.AddSingleton<ICurrentUser>(new TestCurrentUser(UserId));
    services.AddTooarkEntityFrameworkCore();
    services.AddDbContext<AuditDbContext>((sp, options) => options.UseSqlite(Connection).AddTooarkInterceptors(sp));
    services.AddTransient<IMediator, global::Tooark.Mediator.Mediator>();
    services.AddTransient<IRequestHandler<RenameDocument, Guid>, RenameDocumentHandler>();
    services.AddTooarkMediatorUnitOfWork<AuditDbContext>();

    var provider = Track(services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }));

    var seed = NewContext(provider);
    seed.Database.EnsureCreated();
    seed.Documents.Add(new Document { Title = "Contrato" });
    await seed.SaveChangesAsync(Token);

    // Act
    var scope = Track(provider.CreateScope());
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    await mediator.SendAsync(new RenameDocument("Aditivo", handlerStamps), Token);

    // Assert
    var reloaded = await NewContext(provider).Documents.SingleAsync(Token);

    Assert.Equal("Aditivo", reloaded.Title);
    Assert.Equal(2, reloaded.Version);
    Assert.Equal(handlerStamps ? OtherUserId : UserId, reloaded.UpdatedById);
  }

  public sealed record RenameDocument(string Title, bool Stamp) : ICommand<Guid>;

  public sealed class RenameDocumentHandler(AuditDbContext context) : IRequestHandler<RenameDocument, Guid>
  {
    public async Task<Guid> HandleAsync(RenameDocument request, CancellationToken cancellationToken = default)
    {
      var document = await context.Documents.SingleAsync(cancellationToken);

      document.Title = request.Title;

      // O handler pode carimbar a entidade por conta própria: o interceptor reconhece e não repete
      if (request.Stamp)
      {
        document.SetUpdatedBy(OtherUserId);
      }

      // Sem SaveChanges: a persistência é responsabilidade do pipeline
      return document.Id;
    }
  }

  #endregion
}
