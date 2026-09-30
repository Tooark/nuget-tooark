using System.Collections.Concurrent;
using System.Text.Json;
using Tooark.Exceptions;
using Tooark.Secrets.Interfaces;
using Tooark.Secrets.Options;

namespace Tooark.Secrets;

/// <summary>
/// Classe base dos provedores de segredos, com cache e leitura de campo.
/// </summary>
/// <remarks>
/// O provedor implementa só a leitura de um segredo pelo nome. A base valida os argumentos, guarda o valor em cache
/// pelo tempo das opções e extrai o campo de um segredo JSON. Segredo inexistente não entra no cache, para que um
/// segredo criado depois seja lido na chamada seguinte.
/// </remarks>
public abstract class SecretServiceBase : ISecretService
{
  #region Private Fields

  /// <summary>
  /// Valores lidos, com o instante em que deixam de valer.
  /// </summary>
  private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

  /// <summary>
  /// Tempo de vida de um valor no cache.
  /// </summary>
  private readonly TimeSpan _cacheDuration;

  /// <summary>
  /// Relógio usado para a validade do cache.
  /// </summary>
  private readonly TimeProvider _timeProvider;

  #endregion

  #region Constructor

  /// <summary>
  /// Cria a base com as opções dos segredos.
  /// </summary>
  /// <param name="options">As opções dos segredos.</param>
  /// <param name="timeProvider">O relógio do cache. Opcional: sem ele, vale o do sistema.</param>
  protected SecretServiceBase(SecretsOptions options, TimeProvider? timeProvider = null)
  {
    _cacheDuration = TimeSpan.FromMinutes(options.CacheMinutes);
    _timeProvider = timeProvider ?? TimeProvider.System;
  }

  #endregion

  #region Methods

  /// <inheritdoc/>
  public async Task<string?> GetAsync(string name, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new BadRequestException("Secrets.NameRequired");
    }

    var now = _timeProvider.GetUtcNow();

    if (_cache.TryGetValue(name, out var cached) && cached.ExpiresAt > now)
    {
      return cached.Value;
    }

    var value = await ReadAsync(name, cancellationToken);

    if (value is not null && _cacheDuration > TimeSpan.Zero)
    {
      _cache[name] = new CacheEntry(value, now + _cacheDuration);
    }

    return value;
  }

  /// <inheritdoc/>
  public async Task<string?> GetFieldAsync(string name, string field, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(field))
    {
      throw new BadRequestException("Secrets.FieldRequired");
    }

    if (await GetAsync(name, cancellationToken) is not { } value)
    {
      return null;
    }

    try
    {
      using var document = JsonDocument.Parse(value);

      if (document.RootElement.ValueKind != JsonValueKind.Object)
      {
        throw new InternalServerErrorException($"Secrets.NotJsonObject;{name}");
      }

      if (!document.RootElement.TryGetProperty(field, out var property))
      {
        return null;
      }

      return property.ValueKind switch
      {
        JsonValueKind.String => property.GetString(),
        JsonValueKind.Null => null,
        _ => property.GetRawText()
      };
    }
    catch (JsonException)
    {
      throw new InternalServerErrorException($"Secrets.NotJsonObject;{name}");
    }
  }

  #endregion

  #region Protected Methods

  /// <summary>
  /// Lê o valor atual de um segredo no provedor.
  /// </summary>
  /// <param name="name">O nome do segredo, já validado.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  /// <returns>O valor do segredo, ou nulo quando ele não existe.</returns>
  protected abstract Task<string?> ReadAsync(string name, CancellationToken cancellationToken);

  #endregion

  #region Private Types

  /// <summary>
  /// Valor em cache e o instante em que deixa de valer.
  /// </summary>
  /// <param name="Value">O valor do segredo.</param>
  /// <param name="ExpiresAt">O instante em que o valor deixa de valer.</param>
  private sealed record CacheEntry(string Value, DateTimeOffset ExpiresAt);

  #endregion
}
