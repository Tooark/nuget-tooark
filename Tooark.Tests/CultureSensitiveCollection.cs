namespace Tooark.Tests;

/// <summary>
/// Coleção para testes que alteram a cultura global do processo (via <c>Language.SetCulture</c>).
/// </summary>
/// <remarks>
/// Como a cultura é um estado compartilhado de processo, esses testes não podem rodar em paralelo
/// com outros (leitores ou escritores de cultura), sob pena de corridas não determinísticas —
/// especialmente quando os assemblies de teste de múltiplos TFMs (net8.0 e net10.0) competem por CPU.
/// <see cref="CollectionDefinitionAttribute.DisableParallelization"/> faz a coleção executar em fase isolada.
/// </remarks>
[CollectionDefinition("CultureSensitive", DisableParallelization = true)]
public class CultureSensitiveCollection;
