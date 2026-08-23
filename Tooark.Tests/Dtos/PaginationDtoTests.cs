using Microsoft.AspNetCore.Http;
using Tooark.Dtos;
namespace Tooark.Tests.Dtos;

public class PaginationDtoTests
{
  // Teste para verificar se os valores padrões são atribuídos corretamente com apenas total.
  [Fact]
  public void PaginationDto_WithoutQueryString_ShouldSetTotalOnly()
  {
    // Arrange
    var total = 100;

    // Act
    var paginationDto = new PaginationDto(total);

    // Assert
    Assert.Equal(total, paginationDto.Total);
    Assert.Equal(10, paginationDto.PageSize);
    Assert.Equal(1, paginationDto.PageIndex);
    Assert.Null(paginationDto.Previous);
    Assert.Null(paginationDto.Next);
    Assert.Null(paginationDto.CurrentLink);
    Assert.Null(paginationDto.PreviousLink);
    Assert.Null(paginationDto.NextLink);
  }

  // Teste para verificar se os valores padrões são atribuídos corretamente com apenas request.
  [Fact]
  public void PaginationDto_WithoutQueryString_ShouldSetRequestOnly()
  {
    // Arrange
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("example.com");
    context.Request.Path = "/api/test";

    // Act
    var paginationDto = new PaginationDto(context.Request);

    // Assert
    Assert.Equal(0, paginationDto.Total);
    Assert.Equal(10, paginationDto.PageSize);
    Assert.Equal(1, paginationDto.PageIndex);
    Assert.Null(paginationDto.Previous);
    Assert.Null(paginationDto.Next);
    Assert.Equal("http://example.com/api/test", paginationDto.CurrentLink);
    Assert.Null(paginationDto.PreviousLink);
    Assert.Null(paginationDto.NextLink);
  }

  // Teste para verificar se os valores padrões são atribuídos corretamente.
  [Fact]
  public void PaginationDto_WithoutQueryString_ShouldSetDefaultValues()
  {
    // Arrange
    var total = 100;
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("example.com");
    context.Request.Path = "/api/test";

    // Act
    var paginationDto = new PaginationDto(total, context.Request);

    // Assert
    Assert.Equal(total, paginationDto.Total);
    Assert.Equal(0, paginationDto.PageSize);
    Assert.Equal(0, paginationDto.PageIndex);
    Assert.Null(paginationDto.Previous);
    Assert.Null(paginationDto.Next);
    Assert.Equal("http://example.com/api/test", paginationDto.CurrentLink);
    Assert.Null(paginationDto.PreviousLink);
    Assert.Null(paginationDto.NextLink);
  }

  // Teste para verificar se os valores são atribuídos corretamente com os parâmetros de paginação na request.
  [Fact]
  public void PaginationDto_WithParamRequest_ShouldSetValuesCorrectly()
  {
    // Arrange
    var total = 100;
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("example.com");
    context.Request.Path = "/api/test";
    context.Request.QueryString = new QueryString("?PageIndex=1&PageSize=10&Param=Abc123");

    // Act
    var paginationDto = new PaginationDto(total, context.Request);

    // Assert
    Assert.Equal(total, paginationDto.Total);
    Assert.Equal(10, paginationDto.PageSize);
    Assert.Equal(1, paginationDto.PageIndex);
    Assert.Null(paginationDto.Previous);
    Assert.Equal(2, paginationDto.Next);
    Assert.Equal("http://example.com/api/test?PageIndex=1&PageSize=10&Param=Abc123", paginationDto.CurrentLink);
    Assert.Null(paginationDto.PreviousLink);
    Assert.Equal("http://example.com/api/test?PageIndex=2&PageSize=10&Param=Abc123", paginationDto.NextLink);
  }

  // Teste para verificar se os valores são atribuídos corretamente com os parâmetros de paginação na request.
  [Fact]
  public void PaginationDto_WithParamSearch_ShouldSetValuesCorrectly()
  {
    // Arrange
    var total = 100;
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("example.com");
    context.Request.Path = "/api/test";
    SearchDto searchDto = new("Abc123", 1, 10);

    // Act
    var paginationDto = new PaginationDto(total, searchDto, context.Request);

    // Assert
    Assert.Equal(total, paginationDto.Total);
    Assert.Equal(10, paginationDto.PageSize);
    Assert.Equal(1, paginationDto.PageIndex);
    Assert.Null(paginationDto.Previous);
    Assert.Equal(2, paginationDto.Next);
    Assert.Equal("http://example.com/api/test", paginationDto.CurrentLink);
    Assert.Null(paginationDto.PreviousLink);
    Assert.Equal("http://example.com/api/test?Search=Abc123&PageSize=10&PageIndex=2", paginationDto.NextLink);
  }

  // Teste para verificar se os valores são atribuídos corretamente com os parâmetros de paginação no construtor e ignorando os da request.
  [Fact]
  public void PaginationDto_WithParamConstructor_ShouldSetValuesCorrectly()
  {
    // Arrange
    var total = 100;
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = "/api/test";
    context.Request.QueryString = new QueryString("?PageIndex=3&PageSize=20&Param=Abc123");

    // Act
    var paginationDto = new PaginationDto(total, 10, 1, 0, 2, context.Request);

    // Assert
    Assert.Equal(total, paginationDto.Total);
    Assert.Equal(10, paginationDto.PageSize);
    Assert.Equal(1, paginationDto.PageIndex);
    Assert.Null(paginationDto.Previous);
    Assert.Equal(2, paginationDto.Next);
    Assert.Equal("http://localhost/api/test?PageIndex=3&PageSize=20&Param=Abc123", paginationDto.CurrentLink);
    Assert.Null(paginationDto.PreviousLink);
    Assert.Equal("http://localhost/api/test?PageIndex=2&PageSize=10&Param=Abc123", paginationDto.NextLink);
  }

  // Teste para verificar se os valores são atribuídos corretamente com os parâmetros de paginação no construtor com request sem parâmetros de paginação.
  [Fact]
  public void PaginationDto_WithParamConstructor_WithoutQueryString_ShouldSetValuesCorrectly()
  {
    // Arrange
    var total = 100;
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = "/api/test";

    // Act
    var paginationDto = new PaginationDto(total, 10, 1, 0, 2, context.Request);

    // Assert
    Assert.Equal(total, paginationDto.Total);
    Assert.Equal(10, paginationDto.PageSize);
    Assert.Equal(1, paginationDto.PageIndex);
    Assert.Null(paginationDto.Previous);
    Assert.Equal(2, paginationDto.Next);
    Assert.Equal("http://localhost/api/test", paginationDto.CurrentLink);
    Assert.Null(paginationDto.PreviousLink);
    Assert.Equal("http://localhost/api/test?PageSize=10&PageIndex=2", paginationDto.NextLink);
  }

  // Teste para verificar limite de índice da página anterior.
  [Fact]
  public void PaginationDto_WithLimitPrevious_ShouldSetValuesCorrectly()
  {
    // Arrange
    var total = 100;
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = "/api/test";
    context.Request.QueryString = new QueryString("?PageIndex=1&PageSize=10&Param=Abc123");

    // Act
    var paginationDto = new PaginationDto(total, context.Request);

    // Assert
    Assert.Equal(total, paginationDto.Total);
    Assert.Equal(10, paginationDto.PageSize);
    Assert.Equal(1, paginationDto.PageIndex);
    Assert.Null(paginationDto.Previous);
    Assert.Equal(2, paginationDto.Next);
    Assert.Equal("http://localhost/api/test?PageIndex=1&PageSize=10&Param=Abc123", paginationDto.CurrentLink);
    Assert.Null(paginationDto.PreviousLink);
    Assert.Equal("http://localhost/api/test?PageIndex=2&PageSize=10&Param=Abc123", paginationDto.NextLink);
  }

  // Teste para verificar que a penúltima página aponta para a última.
  [Fact]
  public void PaginationDto_OnSecondToLastPage_ShouldPointToLastPage()
  {
    // Arrange
    var total = 100;
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = "/api/test";
    context.Request.QueryString = new QueryString("?PageIndex=9&PageSize=10&Param=Abc123");

    // Act
    var paginationDto = new PaginationDto(total, context.Request);

    // Assert
    Assert.Equal(total, paginationDto.Total);
    Assert.Equal(10, paginationDto.PageSize);
    Assert.Equal(9, paginationDto.PageIndex);
    Assert.Equal(8, paginationDto.Previous);
    Assert.Equal(10, paginationDto.Next);
    Assert.Equal("http://localhost/api/test?PageIndex=9&PageSize=10&Param=Abc123", paginationDto.CurrentLink);
    Assert.Equal("http://localhost/api/test?PageIndex=8&PageSize=10&Param=Abc123", paginationDto.PreviousLink);
    Assert.Equal("http://localhost/api/test?PageIndex=10&PageSize=10&Param=Abc123", paginationDto.NextLink);
  }

  // Teste para verificar que a última página não aponta para uma página seguinte.
  [Fact]
  public void PaginationDto_OnLastPage_ShouldNotHaveNext()
  {
    // Arrange
    var total = 100;
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = "/api/test";
    context.Request.QueryString = new QueryString("?PageIndex=10&PageSize=10");

    // Act
    var paginationDto = new PaginationDto(total, context.Request);

    // Assert
    Assert.Equal(9, paginationDto.Previous);
    Assert.Null(paginationDto.Next);
    Assert.Null(paginationDto.NextLink);
  }

  // Teste para verificar a última página parcial, quando o total não é múltiplo do tamanho.
  [Fact]
  public void PaginationDto_WithPartialLastPage_ShouldBeReachable()
  {
    // Arrange
    var total = 95;
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = "/api/test";
    context.Request.QueryString = new QueryString("?PageIndex=9&PageSize=10");

    // Act
    var paginationDto = new PaginationDto(total, context.Request);

    // Assert: a página 10 traz os registros de 91 a 95
    Assert.Equal(10, paginationDto.Next);
    Assert.Equal("http://localhost/api/test?PageIndex=10&PageSize=10", paginationDto.NextLink);
  }

  // Teste para verificar se os valores padrões são atribuídos corretamente com uma query sem informações de paginação na request.
  [Fact]
  public void PaginationDto_WithQueryString_WithoutParamPagination_ShouldSetDefaultValues()
  {
    // Arrange
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = "/api/test";
    context.Request.QueryString = new QueryString("?Param=Abc123");

    // Act
    var paginationDto = new PaginationDto(100, context.Request);

    // Assert
    Assert.Equal(100, paginationDto.Total);
    Assert.Equal(0, paginationDto.PageSize);
    Assert.Equal(0, paginationDto.PageIndex);
    Assert.Null(paginationDto.Previous);
    Assert.Null(paginationDto.Next);
    Assert.Equal("http://localhost/api/test?Param=Abc123", paginationDto.CurrentLink);
    Assert.Null(paginationDto.PreviousLink);
    Assert.Null(paginationDto.NextLink);
  }
}
