namespace Tooark.Tests.Moq.Util;

/// <summary>
/// Relógio de teste: devolve sempre o instante definido, e avança só quando o teste manda.
/// </summary>
/// <param name="start">Instante inicial.</param>
public sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
{
  private DateTimeOffset _now = start;

  /// <inheritdoc/>
  public override DateTimeOffset GetUtcNow() => _now;

  /// <summary>
  /// Avança o relógio.
  /// </summary>
  /// <param name="delta">Intervalo a avançar (negativo volta no tempo).</param>
  public void Advance(TimeSpan delta) => _now = _now.Add(delta);

  /// <summary>
  /// Define o instante atual.
  /// </summary>
  /// <param name="now">Novo instante.</param>
  public void SetUtcNow(DateTimeOffset now) => _now = now;
}
