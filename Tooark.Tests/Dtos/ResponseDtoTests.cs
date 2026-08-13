using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tooark.Dtos;
using Tooark.Dtos.Injections;
using Tooark.Notifications;

namespace Tooark.Tests.Dtos;

public class ResponseDtoTests
{
  // Adiciona o serviço JsonStringLocalizer com injeção de dependência.
  public ResponseDtoTests()
  {
    var services = new ServiceCollection();
    services.AddTooarkDtos();
  }

  // Cria uma classe que estende notificação para testes
  public class ResponseTest : Notification
  {
    public string Message { get; set; } = null!;

    // Expõe os mutadores protegidos para uso nos testes.
    public new void AddNotification(string message, string key) => base.AddNotification(message, key);

    public new void AddNotification(string message, string key, string code) => base.AddNotification(message, key, code);
  }


  // Teste com o construtor que recebe dados
  [Fact]
  public void Constructor_WithData_ShouldReturnOnlyData()
  {
    // Arrange
    ResponseTest response = new() { Message = "Test" };

    // Act
    ResponseDto<ResponseTest> responseDto = new(response);

    // Assert
    Assert.Equal(response, responseDto.Data);
    Assert.Empty(responseDto.Errors);
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe dados e lista de erros
  [Fact]
  public void Constructor_WithDataAndErrors_ShouldReturnDataAndListErrors()
  {
    // Arrange
    ResponseTest response = new() { Message = "Test" };
    List<string> errors = ["Error 1", "Error 2"];

    // Act
    ResponseDto<ResponseTest> responseDto = new(response, errors);

    // Assert
    Assert.Equal(response, responseDto.Data);
    Assert.Equal(errors, responseDto.Errors);
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe dados e define a paginação
  [Fact]
  public void Constructor_WithDataTotalRequest_ShouldReturnPagination()
  {
    // Arrange
    List<ResponseTest> response = [
      new() { Message = "Test 1" },
      new() { Message = "Test 2" },
      new() { Message = "Test 3" },
      new() { Message = "Test 4" },
      new() { Message = "Test 5" }
    ];
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("example.com");
    context.Request.Path = "/api/test";

    // Act
    ResponseDto<List<ResponseTest>> responseDto = new(response, response.Count, context.Request);

    // Assert
    Assert.Equal(response, responseDto.Data);
    Assert.Empty(responseDto.Errors);
    Assert.NotNull(responseDto.Pagination);
    Assert.Equal(response.Count, responseDto.Pagination.Total);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe string de erro
  [Fact]
  public void Constructor_WithError_ShouldReturnListErrors()
  {
    // Arrange
    string error = "Error 1";

    // Act
    ResponseDto<ResponseTest> responseDto = new(error);

    // Assert
    Assert.Null(responseDto.Data);
    Assert.Single(responseDto.Errors);
    Assert.Equal(error, responseDto.Errors.First());
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe lista de erros
  [Fact]
  public void Constructor_WithErrors_ShouldReturnListErrors()
  {
    // Arrange
    List<string> errors = ["Error 1", "Error 2"];

    // Act
    ResponseDto<ResponseTest> responseDto = new(errors);

    // Assert
    Assert.Null(responseDto.Data);
    Assert.Equal(errors, responseDto.Errors);
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe string e booleano de sucesso
  [Fact]
  public void Constructor_WithStringAndBooleanTrue_ShouldReturnData()
  {
    // Arrange
    string response = "Test";

    // Act
    ResponseDto<string> responseDto = new(response, true);

    // Assert
    Assert.Equal(response, responseDto.Data);
    Assert.Empty(responseDto.Errors);
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe string e booleano de falha
  [Fact]
  public void Constructor_WithStringAndBooleanFalse_ShouldReturnError()
  {
    // Arrange
    string error = "Test";

    // Act
    ResponseDto<string> responseDto = new(error, false);

    // Assert
    Assert.Null(responseDto.Data);
    Assert.Single(responseDto.Errors);
    Assert.Equal(error, responseDto.Errors.First());
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe notificação
  [Fact]
  public void Constructor_WithNotification_ShouldReturnListError()
  {
    // Arrange
    var message = "Error 1";
    var notification = new ResponseTest();
    notification.AddNotification(message, "key");

    // Act
    ResponseDto<ResponseTest> responseDto = new(notification);

    // Assert
    Assert.Null(responseDto.Data?.Messages);
    Assert.Equal(message, responseDto.Errors.First());
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe lista de itens de notificação
  [Fact]
  public void Constructor_WithListNotificationItem_ShouldReturnListError()
  {
    // Arrange
    var message = "Error 1";
    var notification = new ResponseTest();
    notification.AddNotification(message, "key");
    var notifications = notification.Notifications;

    // Act
    ResponseDto<ResponseTest> responseDto = new(notifications);

    // Assert
    Assert.Null(responseDto.Data);
    Assert.Equal(message, responseDto.Errors.First());
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe dados e define a paginação
  [Fact]
  public void SetPagination_WithData_ShouldReturnPagination()
  {
    // Arrange
    List<ResponseTest> response = [
      new() { Message = "Test 1" },
      new() { Message = "Test 2" },
      new() { Message = "Test 3" },
      new() { Message = "Test 4" },
      new() { Message = "Test 5" }
    ];
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("example.com");
    context.Request.Path = "/api/test";
    PaginationDto paginationDto = new(response.Count, context.Request);
    ResponseDto<List<ResponseTest>> responseDto = new(response);

    // Act
    responseDto.SetPagination(paginationDto);

    // Assert
    Assert.Equal(response, responseDto.Data);
    Assert.Empty(responseDto.Errors);
    Assert.Equal(paginationDto, responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe dados e define metadados
  [Fact]
  public void SetMetadata_WithData_ShouldReturnMetadata()
  {
    // Arrange
    ResponseTest response = new() { Message = "Test 1" };
    List<MetadataDto> metadata = [
      new("Key 1", "Value 1"),
      new("Key 2", "Value 2"),
      new("Key 3", "Value 3"),
      new("Key 4", "Value 4"),
      new("Key 5", "Value 5")
    ];
    ResponseDto<ResponseTest> responseDto = new(response);

    // Act
    responseDto.SetMetadata(metadata);

    // Assert
    Assert.Equal(response, responseDto.Data);
    Assert.Empty(responseDto.Errors);
    Assert.Null(responseDto.Pagination);
    Assert.Equal(metadata, responseDto.Metadata);
  }

  // Teste com o construtor que recebe dados e adiciona metadados
  [Fact]
  public void AddMetadata_WithData_ShouldReturnMetadata()
  {
    // Arrange
    ResponseTest response = new() { Message = "Test 1" };
    MetadataDto metadata = new("Key 1", "Value 1");
    ResponseDto<ResponseTest> responseDto = new(response);

    // Act
    responseDto.AddMetadata(metadata);

    // Assert
    Assert.Equal(response, responseDto.Data);
    Assert.Empty(responseDto.Errors);
    Assert.Null(responseDto.Pagination);
    Assert.Single(responseDto.Metadata);
    Assert.Equal(metadata, responseDto.Metadata.First());
  }

  // Teste com o construtor que recebe uma exceção
  [Fact]
  public void Constructor_WithException_ShouldReturnListErrors()
  {
    // Arrange
    string errorMessage = "Error 1";
    var exception = new Exception(errorMessage);

    // Act
    ResponseDto<ResponseTest> responseDto = new(exception);

    // Assert
    Assert.Null(responseDto.Data);
    Assert.Single(responseDto.Errors);
    Assert.Equal(errorMessage, responseDto.Errors.First());
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe notificação e opção de uso de código de erro parâmetro false
  [Fact]
  public void Constructor_WithNotificationAndWithoutCode_ShouldReturnListError()
  {
    // Arrange
    var message = "Error 1";
    var key = "key";
    var code = "Test";
    var withCode = false;
    var notification = new ResponseTest();
    notification.AddNotification(message, key, code);

    // Act
    ResponseDto<ResponseTest> responseDto = new(notification, withCode);

    // Assert
    Assert.Null(responseDto.Data?.Messages);
    Assert.Equal(message, responseDto.Errors.First());
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }

  // Teste com o construtor que recebe notificação e opção de uso de código de erro parâmetro true
  [Fact]
  public void Constructor_WithNotificationAndWithCode_ShouldReturnListError()
  {
    // Arrange
    var message = "Error 1";
    var key = "key";
    var code = "Test";
    var withCode = true;
    var notification = new ResponseTest();
    notification.AddNotification(message, key, code);

    // Act
    ResponseDto<ResponseTest> responseDto = new(notification, withCode);

    // Assert
    Assert.Null(responseDto.Data?.Messages);
    Assert.Equal($"{code}: {message}", responseDto.Errors.First());
    Assert.Null(responseDto.Pagination);
    Assert.Empty(responseDto.Metadata);
  }
}
