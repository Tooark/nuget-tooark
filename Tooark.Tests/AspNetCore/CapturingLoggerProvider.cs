using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Tooark.Tests.AspNetCore;

/// <summary>
/// Provedor de log que guarda cada entrada, para os testes conferirem o nível e a categoria do que foi registrado.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
  /// <summary>
  /// Uma entrada de log registrada.
  /// </summary>
  /// <param name="Category">Categoria do logger.</param>
  /// <param name="Level">Nível da entrada.</param>
  /// <param name="Exception">Exceção anexada à entrada, quando houver.</param>
  /// <param name="Message">Mensagem já formatada.</param>
  internal sealed record Entry(string Category, LogLevel Level, Exception? Exception, string Message);

  /// <summary>
  /// Entradas registradas, na ordem em que chegaram.
  /// </summary>
  internal ConcurrentQueue<Entry> Entries { get; } = new();

  /// <inheritdoc />
  public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

  /// <inheritdoc />
  public void Dispose() { }

  /// <summary>
  /// Logger que repassa cada entrada para a fila do provedor.
  /// </summary>
  /// <param name="category">Categoria do logger.</param>
  /// <param name="entries">Fila compartilhada com o provedor.</param>
  private sealed class CapturingLogger(string category, ConcurrentQueue<Entry> entries) : ILogger
  {
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
      entries.Enqueue(new Entry(category, logLevel, exception, formatter(state, exception)));
  }
}
