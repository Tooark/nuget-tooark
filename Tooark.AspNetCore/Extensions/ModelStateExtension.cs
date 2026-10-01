using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tooark.AspNetCore.Extensions;

/// <summary>
/// Método de extensão para a classe ModelStateDictionary.
/// </summary>
public static class ModelStateExtension
{
  /// <summary>
  /// Obtém os erros de validação do <see cref="ModelStateDictionary"/>.
  /// </summary>
  /// <remarks>
  /// Um erro sem texto, registrado só com a exceção ou com uma mensagem em branco, vira a chave
  /// <c>Field.Invalid;{campo}</c>, ou <c>BadRequest</c> quando não está ligado a um campo. A mensagem da
  /// exceção não é usada: o próprio ASP.NET Core só registra o texto de uma exceção quando ele é seguro para
  /// chegar ao cliente.
  /// <para>
  /// As mensagens vêm como da validação que as registrou. Um campo não anulável com atributo do
  /// <c>Tooark.Attributes</c>, quando ausente, traz a chave <c>Field.Required</c> e o texto do <c>[Required]</c>
  /// que o MVC infere, até a aplicação registrar o
  /// <see cref="Injections.TooarkDependencyInjection.AddTooarkValidationAttributes"/>.
  /// </para>
  /// </remarks>
  /// <param name="modelState">O <see cref="ModelStateDictionary"/> a ser verificado.</param>
  /// <returns>Uma lista de erros de validação.</returns>
  public static IList<string> GetErrors(this ModelStateDictionary modelState)
  {
    // Cria uma lista de erros
    List<string> result = [];

    // Itera sobre as entradas do ModelState, na ordem das chaves
    foreach (var (key, entry) in modelState)
    {
      // Adiciona os erros de validação, sem deixar um erro sem texto virar uma mensagem vazia
      result.AddRange(entry.Errors.Select(error =>
        string.IsNullOrWhiteSpace(error.ErrorMessage) ? MessageWithoutText(key) : error.ErrorMessage));
    }

    // Retorna a lista de erros
    return result;
  }

  /// <summary>
  /// Chave de tradução para um erro registrado sem texto.
  /// </summary>
  /// <param name="key">Chave do campo no <see cref="ModelStateDictionary"/>.</param>
  /// <returns>A chave de campo inválido, ou de requisição inválida quando não há campo.</returns>
  private static string MessageWithoutText(string key) =>
    string.IsNullOrEmpty(key) ? "BadRequest" : $"Field.Invalid;{key}";
}
