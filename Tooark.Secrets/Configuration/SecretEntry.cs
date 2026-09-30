namespace Tooark.Secrets.Configuration;

/// <summary>
/// Segredo ou parâmetro lido do cofre, com o nome relativo ao prefixo configurado.
/// </summary>
/// <remarks>
/// O nome usa <c>/</c> ou <c>__</c> como separador de níveis: <c>Storage/SecretKey</c> e <c>Storage__SecretKey</c>
/// viram a chave <c>Storage:SecretKey</c>. Nome vazio representa a raiz, que só faz sentido com um valor JSON
/// expandido.
/// </remarks>
/// <param name="Name">O nome relativo ao prefixo.</param>
/// <param name="Value">O valor lido.</param>
public readonly record struct SecretEntry(string Name, string? Value);
