using Microsoft.Extensions.Logging;
using Tooark.AspNetCore.ExceptionHandling;

namespace Tooark.Tests.AspNetCore.ExceptionHandling;

public class TooarkProblemDetailsOptionsTests
{
  // O nível de um status sai da faixa dele quando o status não tem nível próprio.
  [Theory]
  [InlineData(400, LogLevel.Debug)]
  [InlineData(404, LogLevel.Debug)]
  [InlineData(499, LogLevel.Debug)]
  [InlineData(500, LogLevel.Critical)]
  [InlineData(503, LogLevel.Critical)]
  public void GetLogLevel_ShouldUseTheRangeLevel(int statusCode, LogLevel expected)
  {
    // Arrange
    var options = new TooarkProblemDetailsOptions
    {
      ClientErrorLogLevel = LogLevel.Debug,
      ServerErrorLogLevel = LogLevel.Critical
    };

    // Act
    var logLevel = options.GetLogLevel(statusCode);

    // Assert
    Assert.Equal(expected, logLevel);
  }

  // O nível informado para um status vence o da faixa, nos 4xx e nos 5xx.
  [Fact]
  public void GetLogLevel_ShouldPreferTheStatusLevel()
  {
    // Arrange
    var options = new TooarkProblemDetailsOptions
    {
      ClientErrorLogLevel = LogLevel.Information,
      ServerErrorLogLevel = LogLevel.Error
    };

    options.StatusCodeLogLevels[401] = LogLevel.Warning;
    options.StatusCodeLogLevels[404] = LogLevel.None;
    options.StatusCodeLogLevels[503] = LogLevel.Warning;

    // Act & Assert
    Assert.Equal(LogLevel.Warning, options.GetLogLevel(401));
    Assert.Equal(LogLevel.None, options.GetLogLevel(404));
    Assert.Equal(LogLevel.Warning, options.GetLogLevel(503));
    Assert.Equal(LogLevel.Information, options.GetLogLevel(409));
    Assert.Equal(LogLevel.Error, options.GetLogLevel(500));
  }

  // Sem configuração, o resultado é o padrão da faixa: o método só lê as propriedades.
  [Fact]
  public void GetLogLevel_ShouldFollowTheDefaults()
  {
    // Arrange
    var options = new TooarkProblemDetailsOptions();

    // Act & Assert
    Assert.Equal(options.ClientErrorLogLevel, options.GetLogLevel(404));
    Assert.Equal(options.ServerErrorLogLevel, options.GetLogLevel(500));
  }
}
