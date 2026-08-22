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
  /// <param name="modelState">O <see cref="ModelStateDictionary"/> a ser verificado.</param>
  /// <returns>Uma lista de erros de validação.</returns>
  public static IList<string> GetErrors(this ModelStateDictionary modelState)
  {
    // Cria uma lista de erros
    List<string> result = [];

    // Itera sobre os valores do ModelState
    foreach (var item in modelState.Values)
    {
      // Adiciona os erros de validação
      result.AddRange(item.Errors.Select(error => error.ErrorMessage));
    }

    // Retorna a lista de erros
    return result;
  }
}
