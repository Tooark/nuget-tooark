using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Tooark.Dtos;
using Tooark.Dtos.Injections;
using Tooark.Notifications;
using Tooark.Utils;

namespace Tooark.Tests.Dtos;

/// <summary>
/// Testes dos comportamentos corrigidos na revisão do Tooark.Dtos.
/// </summary>
[Collection("CultureSensitive")]
public class DtosReviewTests
{
  // Monta uma requisição com a query string informada
  private static HttpRequest Requisicao(string queryString)
  {
    var contexto = new DefaultHttpContext();
    contexto.Request.Scheme = "https";
    contexto.Request.Host = new HostString("api.teste.com");
    contexto.Request.Path = "/pessoas";
    contexto.Request.QueryString = new QueryString(queryString);

    return contexto.Request;
  }

  // DTO que amplia o limite de página do endpoint
  private sealed class RelatorioSearchDto : SearchDto
  {
    protected override long PageSizeMax => 5000;
  }

  // Notificação concreta para os testes
  private sealed class NotificacaoTeste : Notification
  {
    public void Adicionar(string mensagem, string chave, string codigo) => AddNotification(mensagem, chave, codigo);
  }

  #region SearchDto

  // Testa se o tamanho da página é limitado pelo teto padrão
  [Theory]
  [InlineData(10, 10)]
  [InlineData(100, 100)]
  [InlineData(101, 100)]
  [InlineData(1_000_000, 100)]
  [InlineData(long.MaxValue, 100)]
  public void PageSize_ShouldBeCappedByDefaultMaximum(long pedido, long esperado)
  {
    // Arrange & Act
    var dto = new SearchDto { PageSize = pedido };

    // Assert
    Assert.Equal(esperado, dto.PageSize);
  }

  // Testa se o teto pode ser ampliado por um DTO próprio
  [Theory]
  [InlineData(4999, 4999)]
  [InlineData(5000, 5000)]
  [InlineData(5001, 5000)]
  public void PageSize_ShouldHonorOverriddenMaximum(long pedido, long esperado)
  {
    // Arrange & Act
    var dto = new RelatorioSearchDto { PageSize = pedido };

    // Assert
    Assert.Equal(esperado, dto.PageSize);
  }

  // Testa se o teto também vale quando o valor vem pelo construtor
  [Fact]
  public void PageSize_ShouldBeCappedWhenSetByConstructor()
  {
    // Arrange & Act
    var dto = new SearchDto(pageIndex: 1, pageSize: 100_000);

    // Assert
    Assert.Equal(SearchDto.DefaultPageSizeMax, dto.PageSize);
  }

  // Testa se o valor negativo continua significando ignorar o tamanho
  [Fact]
  public void PageSize_ShouldBeZero_WhenNegative()
  {
    // Arrange & Act
    var dto = new SearchDto { PageSize = -5 };

    // Assert
    Assert.Equal(0, dto.PageSize);
  }

  #endregion

  #region PaginationDto

  // Testa se a última página é alcançável a partir da penúltima
  [Theory]
  [InlineData(100, 10, 9, 10L)]
  [InlineData(100, 10, 10, null)]
  [InlineData(95, 10, 9, 10L)]
  [InlineData(95, 10, 10, null)]
  [InlineData(101, 10, 10, 11L)]
  [InlineData(101, 10, 11, null)]
  public void Next_ShouldReachTheLastPage(long total, long tamanho, long indice, long? esperado)
  {
    // Arrange & Act
    var pagination = new PaginationDto(total, Requisicao($"?PageIndex={indice}&PageSize={tamanho}"));

    // Assert
    Assert.Equal(esperado, pagination.Next);
  }

  // Testa se os links anterior e seguinte não interferem um no outro
  [Fact]
  public void Links_ShouldNotInterfereWithEachOther()
  {
    // Arrange & Act
    var pagination = new PaginationDto(100, Requisicao("?PageIndex=5&PageSize=10&status=ativo"));

    // Assert
    Assert.Contains("PageIndex=4", pagination.PreviousLink, StringComparison.Ordinal);
    Assert.Contains("PageIndex=6", pagination.NextLink, StringComparison.Ordinal);
    Assert.Contains("status=ativo", pagination.PreviousLink, StringComparison.Ordinal);
    Assert.Contains("status=ativo", pagination.NextLink, StringComparison.Ordinal);
  }

  // Testa se a requisição ausente falha com erro claro
  [Fact]
  public void Constructor_ShouldThrow_WhenRequestIsNull()
  {
    // Arrange & Act & Assert
    Assert.Throws<ArgumentNullException>(() => new PaginationDto((HttpRequest)null!));
    Assert.Throws<ArgumentNullException>(() => new PaginationDto(10, (HttpRequest)null!));
    Assert.Throws<ArgumentNullException>(() => new PaginationDto(10, new SearchDto(), null!));
    Assert.Throws<ArgumentNullException>(() => new PaginationDto(10, (SearchDto)null!, Requisicao("")));
  }

  #endregion

  #region MetadataDto

  // Testa se chave e valor são independentes
  [Theory]
  [InlineData("cor", "azul", "cor", "azul")]
  [InlineData("", "azul", "", "azul")]
  [InlineData(null, "azul", "", "azul")]
  [InlineData("cor", null, "cor", "")]
  [InlineData(null, null, "", "")]
  public void MetadataDto_ShouldKeepKeyAndValueIndependent(string? chave, string? valor, string esperadoChave, string esperadoValor)
  {
    // Arrange & Act
    var metadata = new MetadataDto(chave, valor);

    // Assert
    Assert.Equal(esperadoChave, metadata.Key);
    Assert.Equal(esperadoValor, metadata.Value);
  }

  #endregion

  #region ResponseDto

  // Testa se as coleções expostas rejeitam alteração externa
  [Fact]
  public void Collections_ShouldRejectExternalChanges()
  {
    // Arrange
    var response = new ResponseDto<string>("Field.Required;Email");
    response.AddMetadata(new MetadataDto("cor", "azul"));

    // Act & Assert: ReadOnlyCollection implementa IList, mas recusa a alteração
    Assert.Throws<NotSupportedException>(() => ((IList<string>)response.Errors).Add("injetado"));
    Assert.Throws<NotSupportedException>(() => ((IList<MetadataDto>)response.Metadata).Clear());

    // Assert: o conteúdo original permanece
    Assert.Single(response.Errors);
    Assert.Single(response.Metadata);
  }

  // Testa se a mesma coleção é reaproveitada a cada leitura
  [Fact]
  public void Collections_ShouldBeReusedBetweenReads()
  {
    // Arrange
    var response = new ResponseDto<string>("Field.Required;Email");

    // Act & Assert
    Assert.Same(response.Errors, response.Errors);
    Assert.Same(response.Metadata, response.Metadata);
  }

  // Testa se os metadados não podem ser alterados de fora
  [Fact]
  public void Metadata_ShouldBeCopiedOnSet()
  {
    // Arrange
    var response = new ResponseDto<string>("dados");
    var lista = new List<MetadataDto> { new("cor", "azul") };

    // Act
    response.SetMetadata(lista);
    lista.Add(new MetadataDto("tamanho", "grande"));

    // Assert
    Assert.Single(response.Metadata);
  }

  // Testa se argumentos ausentes não derrubam a resposta
  [Fact]
  public void Constructors_ShouldAcceptMissingArguments()
  {
    // Arrange & Act & Assert
    Assert.Empty(new ResponseDto<string>((IList<string>?)null).Errors);
    Assert.Empty(new ResponseDto<string>((Exception?)null).Errors);
    Assert.Empty(new ResponseDto<string>((string?)null).Errors);
    Assert.Empty(new ResponseDto<string>((Notification?)null, true).Errors);
    Assert.Empty(new ResponseDto<string>((IReadOnlyCollection<NotificationItem>?)null).Errors);

    var response = new ResponseDto<string>("dados");
    response.SetMetadata(null);
    response.AddMetadata(null);
    Assert.Empty(response.Metadata);
  }

  // Testa se a mensagem passa uma única vez pelo localizador
  [Theory]
  [InlineData(false, "O campo Nome é obrigatório")]
  [InlineData(true, "T.VLD.STR5: O campo Nome é obrigatório")]
  public void Notification_ShouldBeLocalizedOnce(bool comCodigo, string esperado)
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var notificacao = new NotificacaoTeste();
    notificacao.Adicionar("Field.Required;Nome", "Nome", "T.VLD.STR5");

    // Act
    var response = new ResponseDto<string>(notificacao, comCodigo);

    // Assert
    Assert.Equal([esperado], response.Errors);
  }

  // Testa se a tradução funciona sem nenhum registro no container
  [Fact]
  public void Localizer_ShouldWorkWithoutDependencyInjection()
  {
    // Arrange
    Language.SetCulture("pt-BR");

    // Act
    var response = new ResponseDto<string>("Field.Required;Email");

    // Assert
    Assert.Equal(["O campo E-mail é obrigatório"], response.Errors);
  }

  #endregion

  #region Injeção de dependência

  // Testa se o registro não deixa duplicatas nem constrói um provedor descartável
  [Fact]
  public void AddTooarkDtos_ShouldNotDuplicateRegistrations()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddTooarkDtos();

    // Assert
    Assert.Equal(1, services.Count(d => d.ServiceType == typeof(IStringLocalizer)));
  }

  // Testa se o localizador resolvido é o do Tooark, independente da ordem de registro
  [Fact]
  public void AddTooarkDtos_ShouldResolveTheTooarkLocalizer()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddTooarkDtos();

    // Act
    var localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer>();

    // Assert
    Assert.IsType<Tooark.Extensions.JsonStringLocalizerExtension>(localizer);
  }

  // Testa se a paginação não é montada quando não há registros
  [Fact]
  public void Pagination_ShouldNotBuildLinks_WhenThereAreNoRecords()
  {
    // Arrange & Act
    var pagination = new PaginationDto(0, Requisicao("?PageIndex=1&PageSize=10"));

    // Assert
    Assert.Equal(0, pagination.Total);
    Assert.Null(pagination.Previous);
    Assert.Null(pagination.Next);
    Assert.Null(pagination.PreviousLink);
    Assert.Null(pagination.NextLink);
  }

  // Testa se a paginação não é montada quando a página cobre todos os registros
  [Fact]
  public void Pagination_ShouldNotBuildLinks_WhenPageCoversEverything()
  {
    // Arrange & Act
    var pagination = new PaginationDto(5, Requisicao("?PageIndex=1&PageSize=10"));

    // Assert
    Assert.Null(pagination.Previous);
    Assert.Null(pagination.Next);
  }

  // Testa se um índice de página não numérico é tratado como ausente
  [Theory]
  [InlineData("?PageIndex=abc&PageSize=10")]
  [InlineData("?PageSize=10")]
  [InlineData("")]
  public void Pagination_ShouldIgnoreUnparsableQueryValues(string queryString)
  {
    // Arrange & Act
    var pagination = new PaginationDto(100, Requisicao(queryString));

    // Assert
    Assert.Equal(0, pagination.PageIndex);
    Assert.Null(pagination.Previous);
    Assert.Null(pagination.Next);
  }

  // Testa o construtor que recebe os índices vizinhos prontos
  [Theory]
  [InlineData(100, 10, 5, 4, 6, 4L, 6L)]
  [InlineData(100, 10, 1, 0, 2, null, 2L)]
  [InlineData(100, 10, 10, 9, 11, 9L, null)]
  [InlineData(100, 10, 5, 4, 0, 4L, null)]
  public void Pagination_WithExplicitNeighbours_ShouldRespectBoundaries(
    long total, long tamanho, long indice, long anterior, long seguinte, long? esperadoAnterior, long? esperadoSeguinte)
  {
    // Arrange & Act
    var pagination = new PaginationDto(total, tamanho, indice, anterior, seguinte, Requisicao("?Param=Abc"));

    // Assert
    Assert.Equal(esperadoAnterior, pagination.Previous);
    Assert.Equal(esperadoSeguinte, pagination.Next);
  }

  // Testa se o construtor com índices vizinhos não monta links quando a página cobre todos os registros
  [Fact]
  public void Pagination_WithExplicitNeighbours_ShouldNotBuildLinks_WhenPageCoversEverything()
  {
    // Arrange & Act
    var pagination = new PaginationDto(5, 10, 1, 0, 2, Requisicao(""));

    // Assert
    Assert.Null(pagination.Previous);
    Assert.Null(pagination.Next);
  }

  // Testa se o termo de busca entra na query string dos links quando informado
  [Theory]
  [InlineData("maria", true)]
  [InlineData("", false)]
  [InlineData(null, false)]
  public void Pagination_WithSearchDto_ShouldCarrySearchInLinks(string? busca, bool esperado)
  {
    // Arrange
    var filtro = new SearchDto(busca, 2, 10);

    // Act
    var pagination = new PaginationDto(100, filtro, Requisicao("?PageIndex=2&PageSize=10"));

    // Assert
    Assert.Equal(esperado, pagination.NextLink!.Contains("Search=", StringComparison.Ordinal));
  }

  // Testa se a paginação com busca não monta links quando a página cobre todos os registros
  [Fact]
  public void Pagination_WithSearchDto_ShouldNotBuildLinks_WhenPageCoversEverything()
  {
    // Arrange & Act
    var pagination = new PaginationDto(5, new SearchDto("maria", 1, 10), Requisicao(""));

    // Assert
    Assert.Null(pagination.Previous);
    Assert.Null(pagination.Next);
  }

  // Testa se a busca ausente não é normalizada
  [Fact]
  public void SearchNormalized_ShouldBeNull_WhenSearchIsMissing()
  {
    // Arrange & Act
    var dto = new SearchDto((string?)null);

    // Assert
    Assert.Null(dto.SearchNormalized);
  }

  // Testa se a busca definida como nula invalida a normalização anterior
  [Fact]
  public void SearchNormalized_ShouldBeInvalidated_WhenSearchChanges()
  {
    // Arrange
    var dto = new SearchDto("Olá Mundo");

    // Act
    var primeiro = dto.SearchNormalized;
    dto.Search = null;
    var segundo = dto.SearchNormalized;

    // Assert
    Assert.Equal("OLAMUNDO", primeiro);
    Assert.Null(segundo);
  }

  // Testa a resposta de sucesso com mensagem ausente
  [Fact]
  public void Response_ShouldHaveDefaultData_WhenSuccessMessageIsMissing()
  {
    // Arrange & Act
    var response = new ResponseDto<string>(null, isSuccess: true);

    // Assert
    Assert.Null(response.Data);
    Assert.Empty(response.Errors);
  }

  // Testa a resposta de sucesso com mensagem convertida para o tipo declarado
  [Fact]
  public void Response_ShouldConvertSuccessMessage()
  {
    // Arrange & Act
    var texto = new ResponseDto<string>("tudo certo", isSuccess: true);
    var numero = new ResponseDto<int>("42", isSuccess: true);

    // Assert
    Assert.Equal("tudo certo", texto.Data);
    Assert.Equal(42, numero.Data);
  }

  // Testa se itens nulos dentro da coleção de notificações são descartados
  [Fact]
  public void Response_ShouldIgnoreNullNotificationItems()
  {
    // Arrange
    Language.SetCulture("pt-BR");
    var itens = new List<NotificationItem> { new("Field.Required;Nome"), null! };

    // Act
    var response = new ResponseDto<string>(itens);

    // Assert
    Assert.Single(response.Errors);
  }

  // Testa se os dados e os erros convivem no construtor que recebe os dois
  [Fact]
  public void Response_ShouldKeepDataAndErrorsTogether()
  {
    // Arrange
    Language.SetCulture("pt-BR");

    // Act
    var response = new ResponseDto<string>("parcial", ["Field.Required;Nome"]);

    // Assert
    Assert.Equal("parcial", response.Data);
    Assert.Equal(["O campo Nome é obrigatório"], response.Errors);
  }

  // Testa a resposta com paginação montada a partir do total e da requisição
  [Fact]
  public void Response_ShouldBuildPaginationFromRequest()
  {
    // Arrange & Act
    var response = new ResponseDto<string>("dados", 100, Requisicao("?PageIndex=2&PageSize=10"));

    // Assert
    Assert.NotNull(response.Pagination);
    Assert.Equal(100, response.Pagination.Total);
    Assert.Equal(3, response.Pagination.Next);
  }

  // Testa se a paginação pode ser removida da resposta
  [Fact]
  public void Response_ShouldAcceptNullPagination()
  {
    // Arrange
    var response = new ResponseDto<string>("dados", 100, Requisicao(""));

    // Act
    response.SetPagination(null);

    // Assert
    Assert.Null(response.Pagination);
  }

  // Testa se a normalização é calculada uma única vez por valor de busca
  [Fact]
  public void SearchNormalized_ShouldBeCachedBetweenReads()
  {
    // Arrange
    var dto = new SearchDto("Olá Mundo");

    // Act
    var primeiro = dto.SearchNormalized;
    var segundo = dto.SearchNormalized;

    // Assert
    Assert.Equal("OLAMUNDO", primeiro);
    Assert.Same(primeiro, segundo);
  }

  // Testa a resposta com coleção de notificações vazia
  [Fact]
  public void Response_ShouldHaveNoErrors_WhenNotificationCollectionIsEmpty()
  {
    // Arrange & Act
    var response = new ResponseDto<string>(new List<NotificationItem>());

    // Assert
    Assert.Empty(response.Errors);
  }

  #endregion
}
