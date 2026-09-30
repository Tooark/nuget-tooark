using System.Text.Json;
using Tooark.Secrets.Configuration;
using Tooark.Secrets.Vault.Clients;
using Tooark.Secrets.Vault.Options;

namespace Tooark.Secrets.Vault.Configuration;

/// <summary>
/// Fonte de configuração com os segredos do KV v2 abaixo de um caminho.
/// </summary>
/// <param name="options">As opções já validadas.</param>
/// <param name="client">Cria o cliente do cofre, usado só durante a carga.</param>
internal sealed class VaultSecretsConfigurationProvider(VaultSecretsOptions options, Func<VaultClient> client)
  : SecretsConfigurationProvider(options, "Vault")
{
  #region Constants

  /// <summary>
  /// Profundidade máxima de pastas abaixo do caminho configurado.
  /// </summary>
  private const int MaxDepth = 10;

  #endregion

  #region Protected Methods

  /// <inheritdoc/>
  protected override async Task<IReadOnlyList<SecretEntry>> LoadEntriesAsync(CancellationToken cancellationToken)
  {
    using var vault = client();
    var entries = new List<SecretEntry>();

    await WalkAsync(vault, options.Path!.Trim('/'), string.Empty, 0, entries, cancellationToken);

    return entries;
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Lê o segredo de um caminho e percorre as pastas abaixo dele.
  /// </summary>
  /// <param name="vault">O cliente do cofre.</param>
  /// <param name="root">O caminho configurado.</param>
  /// <param name="relative">O caminho relativo à raiz, vazio na própria raiz.</param>
  /// <param name="depth">A profundidade atual.</param>
  /// <param name="entries">As entradas em montagem.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  private static async Task WalkAsync(
    VaultClient vault,
    string root,
    string relative,
    int depth,
    List<SecretEntry> entries,
    CancellationToken cancellationToken
  )
  {
    var path = Join(root, relative);

    // Um caminho pode ser segredo e pasta ao mesmo tempo
    await AddSecretAsync(vault, path, relative, entries, cancellationToken);

    if (depth >= MaxDepth)
    {
      return;
    }

    foreach (var key in await vault.ListAsync(path, cancellationToken))
    {
      var child = Join(relative, key.TrimEnd('/'));

      if (key.EndsWith('/'))
      {
        await WalkAsync(vault, root, child, depth + 1, entries, cancellationToken);
      }
      else
      {
        await AddSecretAsync(vault, Join(root, child), child, entries, cancellationToken);
      }
    }
  }

  /// <summary>
  /// Acrescenta os campos de um segredo, cada um como uma entrada.
  /// </summary>
  /// <param name="vault">O cliente do cofre.</param>
  /// <param name="path">O caminho completo do segredo.</param>
  /// <param name="relative">O caminho relativo, que vira o começo da chave.</param>
  /// <param name="entries">As entradas em montagem.</param>
  /// <param name="cancellationToken">Token de cancelamento.</param>
  private static async Task AddSecretAsync(
    VaultClient vault,
    string path,
    string relative,
    List<SecretEntry> entries,
    CancellationToken cancellationToken
  )
  {
    if (await vault.ReadAsync(path, cancellationToken) is not { } fields)
    {
      return;
    }

    foreach (var field in fields.EnumerateObject())
    {
      var value = field.Value.ValueKind switch
      {
        JsonValueKind.String => field.Value.GetString(),
        JsonValueKind.Null => null,
        _ => field.Value.GetRawText()
      };

      entries.Add(new SecretEntry(Join(relative, field.Name), value));
    }
  }

  /// <summary>
  /// Junta dois trechos de caminho.
  /// </summary>
  /// <param name="left">O trecho de cima, vazio na raiz.</param>
  /// <param name="right">O trecho de baixo.</param>
  /// <returns>O caminho combinado.</returns>
  private static string Join(string left, string right) =>
    left.Length == 0 ? right : right.Length == 0 ? left : $"{left}/{right}";

  #endregion
}
