using System.Linq.Expressions;
using System.Reflection;
using Tooark.Utils;

namespace Tooark.Extensions;

/// <summary>
/// Método de extensão para Enumerable.
/// </summary>
public static class EnumerableExtensions
{
  /// <summary>
  /// Ordena uma sequência de objetos por uma propriedade e de forma ascendente.
  /// </summary>
  /// <typeparam name="T">Tipo do objeto a ser ordenado.</typeparam>
  /// <param name="source">Sequência de objetos a ser ordenada.</param>
  /// <param name="sortProperty">Propriedade a ser utilizada para ordenar.</param>
  /// <returns>Retorna a sequência de objetos ordenados de forma ascendente.</returns>
  public static IEnumerable<T> OrderByProperty<T>(this IEnumerable<T> source, string sortProperty)
  {
    return InternalEnumerableExtensions.OrderByProperty(source, sortProperty, true);
  }

  /// <summary>
  /// Ordena uma sequência de objetos por uma propriedade e de forma descendente.
  /// </summary>
  /// <typeparam name="T">Tipo do objeto a ser ordenado.</typeparam>
  /// <param name="source">Sequência de objetos a ser ordenada.</param>
  /// <param name="sortProperty">Propriedade a ser utilizada para ordenar.</param>
  /// <returns>Retorna a sequência de objetos ordenados de forma descendente.</returns>
  public static IEnumerable<T> OrderByPropertyDescending<T>(this IEnumerable<T> source, string sortProperty)
  {
    return InternalEnumerableExtensions.OrderByProperty(source, sortProperty, false);
  }
}

/// <summary>
/// Método interno de extensão para Enumerable.
/// </summary>
internal static class InternalEnumerableExtensions
{
  #region Private Static Fields

  /// <summary>
  /// Método de seleção.
  /// </summary>
  private static readonly MethodInfo SelectMethod = typeof(Enumerable)
    .GetMethods(BindingFlags.Static | BindingFlags.Public)
    .First(m =>
      m.Name == "Select" &&
      m.GetParameters().Length == 2);

  /// <summary>
  /// Método de primeiro ou padrão.
  /// </summary>
  private static readonly MethodInfo FirstOrDefaultMethod = typeof(Enumerable)
    .GetMethods(BindingFlags.Static | BindingFlags.Public)
    .First(m =>
      m.Name == "FirstOrDefault" &&
      m.GetParameters().Length == 1 &&
      m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

  #endregion

  #region Internal Methods

  /// <summary>
  /// Ordena uma sequência de objetos por uma propriedade.
  /// </summary>
  /// <typeparam name="T">Tipo do objeto a ser ordenado.</typeparam>
  /// <param name="source">Sequência de objetos a ser ordenada.</param>
  /// <param name="sortProperty">Propriedade a ser utilizada para ordenar.</param>
  /// <param name="ascending">Indica se a ordenação é ascendente.</param>
  /// <returns>Retorna a sequência de objetos ordenados.</returns>
  internal static IEnumerable<T> OrderByProperty<T>(this IEnumerable<T> source, string sortProperty, bool ascending = true)
  {
    // Verifica se a propriedade existe no tipo de objeto
    if (!PropertyExists<T>(sortProperty))
    {
      // Retorna a sequência de objetos se a propriedade não existir no tipo de objeto
      return source;
    }

    // Quebra a string para verificar se existe a propriedade ou campo
    var properties = sortProperty.Split('.');

    // Cria uma expressão de parâmetro
    ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "Param_0");

    // Monta a expressão de propriedade para ordenação
    Expression propertyExpression = BuildExpression(parameterExpression, properties);

    // Cria uma expressão lambda para a propriedade
    var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(propertyExpression, typeof(object)), parameterExpression);

    // Retorna a sequência de objetos ordenada, ascendente ou descendente
    return ascending ? source.OrderBy(lambda.Compile()) : source.OrderByDescending(lambda.Compile());
  }

  #endregion

  #region Private Methods

  /// <summary>
  /// Expressão de igualdade com nulo.
  /// </summary>
  /// <param name="expression">Expressão a ser utilizada.</param>
  /// <returns>Retorna a expressão de igualdade com nulo.</returns>
  private static BinaryExpression EqualNull(Expression expression)
  {
    // Pega o tipo da expressão
    Type type = expression.Type;

    // Verifica se o tipo é anulável
    if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
    {
      // Torna o tipo anulável
      type = typeof(Nullable<>).MakeGenericType(type);
    }

    // Cria a constante nula com o tipo ajustado
    ConstantExpression nullConstant = Expression.Constant(null, type);

    // Converte a expressão para o tipo ajustado se necessário
    Expression convertedExpression = expression.Type == type ? expression : Expression.Convert(expression, type);

    // Retorna a expressão de igualdade com nulo
    return Expression.Equal(convertedExpression, nullConstant);
  }

  /// <summary>
  /// Expressão lambda de igualdade com nulo.
  /// </summary>
  /// <param name="expression">Expressão a ser utilizada.</param>
  /// <param name="member">Membro a ser utilizado.</param>
  /// <returns>Retorna a expressão lambda de igualdade com nulo.</returns>
  private static LambdaExpression LambdaNull(Expression expression, MemberExpression member)
  {
    // Cria uma expressão de parâmetro
    ParameterExpression parameterExpression = Expression.Parameter(expression.Type, "Param_0");

    // Retorna a expressão lambda
    return Expression
      .Lambda(
        EqualNull(member),
        parameterExpression
      );
  }

  /// <summary>
  /// Monta expressão condicional.
  /// </summary>
  /// <param name="lambda">Expressão lambda a ser utilizada.</param>
  /// <param name="expression">Expressão a ser utilizada.</param>
  /// <returns>Retorna a expressão condicional.</returns>
  private static ConditionalExpression MountConditional(LambdaExpression lambda, Expression expression)
  {
    // Pega a constante da expressão
    ConstantExpression constantExpression = GetDefaultValue(expression.Type);

    // Converte a constante para o tipo da expressão, se necessário
    Expression convertedConstantExpression = constantExpression.Type == expression.Type
      ? constantExpression
      : Expression.Convert(constantExpression, expression.Type);

    // Pega o corpo da expressão lambda
    var body = lambda.Body;

    // Retorna a expressão condicional
    return Expression.Condition(
      body,
      convertedConstantExpression,
      expression
    );
  }

  /// <summary>
  /// Monta a expressão de propriedade para ordenação.
  /// </summary>
  /// <param name="expression">Expressão a ser utilizada.</param>
  /// <param name="properties">Propriedades a serem utilizadas.</param>
  /// <param name="index">Índice a ser utilizado. Padrão é 0.</param>
  /// <param name="lambdaNull">Expressão lambda nula a ser utilizada. Padrão é nulo.</param>
  /// <returns>Retorna a expressão de propriedade para ordenação.</returns>
  private static Expression BuildExpression(
    Expression expression,
    string[] properties,
    int index = 0,
    LambdaExpression lambdaNull = null!
  )
  {
    // Verifica se o índice é maior ou igual ao tamanho das propriedades
    if (index >= properties.Length)
    {
      // Retorna a expressão se o índice for maior ou igual ao tamanho das propriedades
      return expression;
    }

    // Pega a propriedade
    string property = properties[index];

    // Cria uma expressão recursiva
    Expression? recursiveExpression;

    // Pega a expressão da propriedade ou campo
    MemberExpression propertyOrField = GetPropertyOrField(expression, property);

    // Cria uma expressão lambda para a condição de igualdade com nulo sobre a propriedade ou campo
    lambdaNull = LambdaNull(expression, propertyOrField) ?? lambdaNull;

    // Verifica se o tipo é uma coleção
    if (IsCollectionType(propertyOrField.Type))
    {
      // Pega a expressão padrão
      Expression defaultExpression = propertyOrField;

      // Pega a propriedade ou campo do elemento da coleção
      var elementType = propertyOrField.Type.GetGenericArguments()[0];

      // Cria uma expressão de parâmetro para a coleção
      ParameterExpression collectionParameterExpression = Expression.Parameter(elementType, GenerateString.Sequential(index + 1));

      // Pega a expressão da propriedade ou campo
      var collectionExpression = BuildExpression(collectionParameterExpression, properties, index + 1, lambdaNull);

      // Cria uma expressão lambda para o Select
      var lambdaSelect = Expression.Lambda(collectionExpression, collectionParameterExpression);

      // Aplica Select à coleção
      var selectMethod = SelectMethod.MakeGenericMethod(elementType, collectionExpression.Type);
      var selectExpression = Expression.Call(selectMethod, defaultExpression, lambdaSelect);

      // Aplica FirstOrDefault à coleção
      var firstOrDefaultMethod = FirstOrDefaultMethod.MakeGenericMethod(collectionExpression.Type);
      var firstOrDefaultExpression = Expression.Call(firstOrDefaultMethod, selectExpression);

      // Atualiza a expressão padrão
      recursiveExpression = firstOrDefaultExpression;
    }
    else
    {
      // Chama recursivamente a função
      recursiveExpression = BuildExpression(propertyOrField, properties, index + 1, lambdaNull);
    }

    // Retorna a expressão condicional recursiva se a expressão recursiva não for nula
    return recursiveExpression == null ? expression : MountConditional(lambdaNull!, recursiveExpression);
  }

  /// <summary>
  /// Verifica se o tipo é uma coleção.
  /// </summary>
  /// <param name="type">Tipo a ser verificado.</param>
  /// <returns>Retorna verdadeiro se o tipo for uma coleção.</returns>
  private static bool IsCollectionType(Type type) => type.IsGenericType && new[]
    {
      typeof(IList<>),              // IList<T> é uma interface que representa uma lista de objetos do tipo T que pode ser acessada por índice.
      typeof(IReadOnlyList<>),      // IReadOnlyList<T> é uma interface que representa uma lista somente leitura de objetos do tipo T.
      typeof(IEnumerable<>),        // IEnumerable<T> é uma interface que expõe um enumerador, que dá suporte a uma iteração simples sobre uma coleção de objetos.
      typeof(ICollection<>),        // ICollection<T> é uma interface que representa uma coleção de objetos do tipo T que podem ser acessados por índice e cujo tamanho é mutável.
      typeof(IReadOnlyCollection<>) // IReadOnlyCollection<T> é uma interface que representa uma coleção somente leitura de objetos do tipo T.
    }.Contains(type.GetGenericTypeDefinition());

  /// <summary>
  /// Confirma se a propriedade existe no tipo de objeto.
  /// </summary>
  /// <typeparam name="T">Tipo do objeto a ser ordenado.</typeparam>
  /// <param name="sortProperty">Propriedade a ser utilizada para ordenar.</param>
  /// <returns>Retorna verdadeiro se a propriedade existir no tipo de objeto.</returns>
  private static bool PropertyExists<T>(string sortProperty)
  {
    // Retorna falso se a propriedade for nula ou vazia
    if (string.IsNullOrEmpty(sortProperty))
    {
      // Retorna falso se a propriedade for nula ou vazia
      return false;
    }

    // Quebra a string para verificar se existe a propriedade ou campo
    var properties = sortProperty.Split('.');

    // Pega o tipo do objeto
    Type type = typeof(T);

    // Itera sobre as propriedades a serem ordenadas
    foreach (var property in properties)
    {
      // Verifica se a propriedade é uma coleção
      if (IsCollectionType(type))
      {
        // Pega o tipo do elemento da coleção
        type = type.GetGenericArguments()[0];
      }

      // Verifica se a propriedade ou campo existe no tipo de objeto
      var tempType = PropertyType(type, property) ?? FieldType(type, property);

      // Verifica se a propriedade ou campo é nulo
      if (tempType == null)
      {
        // Retorna falso se a propriedade ou campo não existir
        return false;
      }

      // Pega o tipo da propriedade ou campo
      type = tempType;
    }

    // Retorna verdadeiro se todas as propriedades ou campos existirem
    return true;
  }

  /// <summary>
  /// Pega o tipo da propriedade do objeto.
  /// </summary>
  /// <param name="type">Tipo do objeto verificado.</param>
  /// <param name="property">Propriedade a ser verificada.</param>
  /// <returns>Retorna o tipo da propriedade do objeto.</returns>
  private static Type? PropertyType(Type type, string property)
  {
    // Retorna o tipo da propriedade do objeto
    return type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.PropertyType;
  }

  /// <summary>
  /// Pega o tipo do campo do objeto.
  /// </summary>
  /// <param name="type">Tipo do objeto verificado.</param>
  /// <param name="field">Campo a ser verificado.</param>
  /// <returns>Retorna o tipo do campo do objeto.</returns>
  private static Type? FieldType(Type type, string field)
  {
    // Retorna o tipo do campo do objeto
    return type.GetField(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.FieldType;
  }

  /// <summary>
  /// Representa o acesso a uma propriedade ou campo.
  /// </summary>
  /// <param name="expression">Expressão do objeto.</param>
  /// <param name="property">Nome da propriedade.</param>
  /// <returns>Retorna a representação da propriedade ou campo.</returns>
  private static MemberExpression GetPropertyOrField(Expression expression, string property)
  {
    // Retorna a propriedade ou o campo se existir
    return Expression.PropertyOrField(expression, property);
  }

  /// <summary>
  /// Pega o valor padrão de um tipo.
  /// </summary>
  /// <param name="type">Tipo do objeto.</param>
  /// <returns>Retorna o valor padrão do tipo.</returns>
  /// <exception cref="ArgumentNullException">Lança uma exceção se o tipo for nulo.</exception>
  private static ConstantExpression GetDefaultValue(Type type)
  {
    // Verifica se o tipo não é um valor
    if (!type.IsValueType)
    {
      // Retorna null se o tipo não for um valor
      return Expression.Constant(null, type);
    }

    // Retorna o valor padrão do tipo
    return Type.GetTypeCode(type) switch
    {
      TypeCode.Object => Expression.Constant(default, typeof(object)),                //   1: Representa um tipo de objeto.
      TypeCode.Boolean => Expression.Constant(default(bool), typeof(bool)),           //   3: Representa um tipo simples que representa um valor booleano de verdadeiro ou falso.
      TypeCode.Char => Expression.Constant(default(char), typeof(char)),              //   4: Representa um tipo integral que representa valores de caracteres Unicode.
      TypeCode.SByte => Expression.Constant(default(sbyte), typeof(sbyte)),           //   5: Representa um tipo integral que representa valores de inteiros assinados de 8 bits com valores entre -128 e 127.
      TypeCode.Byte => Expression.Constant(default(byte), typeof(byte)),              //   6: Representa um tipo integral que representa valores de inteiros sem sinal de 8 bits com valores entre 0 e 255.
      TypeCode.Int16 => Expression.Constant(default(int), typeof(int)),               //   7: Representa um tipo integral que representa valores de inteiros assinados de 16 bits com valores entre -32768 e 32767.
      TypeCode.UInt16 => Expression.Constant(default(uint), typeof(uint)),            //   8: Representa um tipo integral que representa valores de inteiros sem sinal de 16 bits com valores entre 0 e 65535.
      TypeCode.Int32 => Expression.Constant(default(int), typeof(int)),               //   9: Representa um tipo integral que representa valores de inteiros assinados de 32 bits com valores entre -2147483648 e 2147483647.
      TypeCode.UInt32 => Expression.Constant(default(uint), typeof(uint)),            //  10: Representa um tipo integral que representa valores de inteiros sem sinal de 32 bits com valores entre 0 e 4294967295.
      TypeCode.Int64 => Expression.Constant(default(int), typeof(int)),               //  11: Representa um tipo integral que representa valores de inteiros assinados de 64 bits com valores entre -9223372036854775808 e 9223372036854775807.
      TypeCode.UInt64 => Expression.Constant(default(uint), typeof(uint)),            //  12: Representa um tipo integral que representa valores de inteiros sem sinal de 64 bits com valores entre 0 e 18446744073709551615.
      TypeCode.Single => Expression.Constant(default(float), typeof(float)),          //  13: Representa um tipo de ponto flutuante que representa valores variando de aproximadamente 1,5 x 10 -45 a 3,4 x 10 38 com uma precisão de 7 dígitos.
      TypeCode.Double => Expression.Constant(default(double), typeof(double)),        //  14: Representa um tipo de ponto flutuante que representa valores variando de aproximadamente 5,0 x 10 -324 a 1,7 x 10 308 com uma precisão de 15-16 dígitos.
      TypeCode.Decimal => Expression.Constant(default(decimal), typeof(decimal)),     //  15: Representa um tipo decimal de 128 bits com uma faixa de valores de -79.228.162.514.264.337.593.543.950.335 a 79.228.162.514.264.337.593.543.950.335 com uma precisão de 28-29 dígitos.
      TypeCode.DateTime => Expression.Constant(default(DateTime), typeof(DateTime)),  //  16: Representa um tipo que armazena valores de data e hora. O valor DateTime.MinValue é 00:00:00.0000000, 1 de janeiro de 0001.
      _ => Expression.Constant(Activator.CreateInstance(type), type)
    };
  }

  #endregion
}
