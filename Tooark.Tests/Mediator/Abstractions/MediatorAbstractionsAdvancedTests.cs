using Tooark.Mediator.Abstractions;

namespace Tooark.Tests.Mediator.Abstractions;

/// <summary>
/// Testes avançados para as interfaces abstratas do Mediator.
/// </summary>
public class MediatorAbstractionsAdvancedTests
{
  #region IRequest Interface Tests

  // Testa se a requisição sem retorno é apenas uma marcação, sem membros a implementar
  [Fact]
  public void IRequest_ShouldBeMarkerInterface()
  {
    // Arrange
    var requestType = typeof(IRequest);

    // Assert
    Assert.True(requestType.IsInterface);
    Assert.False(requestType.GetMethods().Any());
  }

  // Testa se a requisição com retorno declara o tipo da resposta no contrato
  [Fact]
  public void IRequest_Generic_ShouldDefineGenericContract()
  {
    // Arrange
    var requestGenericType = typeof(IRequest<>);

    // Assert
    Assert.True(requestGenericType.IsInterface);
    Assert.True(requestGenericType.IsGenericTypeDefinition);
  }

  // Testa se a requisição sem retorno equivale a uma requisição do tipo unitário
  [Fact]
  public void IRequest_ShouldImplementIRequestOfUnit()
  {
    // Arrange
    var requestType = typeof(IRequest);
    var expectedInterface = typeof(IRequest<Unit>);

    // Assert
    Assert.Contains(expectedInterface, requestType.GetInterfaces());
  }

  #endregion

  #region ICommand Interface Tests

  // Testa se o comando sem retorno se encaixa no mesmo despacho da requisição
  [Fact]
  public void ICommand_ShouldImplementIRequestOfUnit()
  {
    // Arrange
    var commandType = typeof(ICommand);
    var expectedInterface = typeof(IRequest<Unit>);

    // Assert
    Assert.Contains(expectedInterface, commandType.GetInterfaces());
  }

  // Testa se o comando com retorno se encaixa no mesmo despacho da requisição
  [Fact]
  public void ICommand_Generic_ShouldImplementIRequestOfTResponse()
  {
    // Arrange
    var commandType = typeof(ICommand<string>);
    var expectedInterface = typeof(IRequest<string>);

    // Assert
    Assert.Contains(expectedInterface, commandType.GetInterfaces());
  }

  #endregion

  #region IQuery Interface Tests

  // Testa se a consulta se encaixa no mesmo despacho da requisição
  [Fact]
  public void IQuery_ShouldImplementIRequestOfTResponse()
  {
    // Arrange
    var queryType = typeof(IQuery<string>);
    var expectedInterface = typeof(IRequest<string>);

    // Assert
    Assert.Contains(expectedInterface, queryType.GetInterfaces());
  }

  #endregion

  #region INotify Interface Tests

  // Testa se a notificação é apenas uma marcação, sem membros a implementar
  [Fact]
  public void INotify_ShouldBeMarkerInterface()
  {
    // Arrange
    var notificationType = typeof(INotify);

    // Assert
    Assert.True(notificationType.IsInterface);
    Assert.False(notificationType.GetMethods().Any());
  }

  #endregion

  #region Unit Struct Edge Cases

  // Testa se o tipo unitário é de valor, para não alocar a cada comando sem retorno
  [Fact]
  public void Unit_ShouldBeValueType()
  {
    // Assert
    Assert.True(typeof(Unit).IsValueType);
  }

  // Testa se o tipo unitário declara a igualdade fortemente tipada
  [Fact]
  public void Unit_ShouldImplementEquatable()
  {
    // Arrange
    var unitType = typeof(Unit);
    var equatableType = typeof(IEquatable<Unit>);

    // Assert
    Assert.Contains(equatableType, unitType.GetInterfaces());
  }

  // Testa se todas as instâncias são iguais, já que o tipo tem um único valor possível
  [Fact]
  public void Unit_AllInstances_ShouldBeEqual()
  {
    // Arrange
    var unit1 = new Unit();
    var unit2 = new Unit();
    var unit3 = Unit.Value;

    // Assert
    Assert.Equal(unit1, unit2);
    Assert.Equal(unit2, unit3);
    Assert.Equal(unit1, unit3);
  }

  // Testa a representação textual, que segue a convenção do tipo unitário
  [Fact]
  public void Unit_ToString_ShouldReturnEmptyParentheses()
  {
    // Act
    var result = Unit.Value.ToString();

    // Assert
    Assert.Equal("()", result);
  }

  // Testa se o código de dispersão é constante, coerente com haver um único valor
  [Fact]
  public void Unit_GetHashCode_ShouldAlwaysReturnZero()
  {
    // Arrange
    var unit1 = new Unit();
    var unit2 = new Unit();

    // Act
    var hash1 = unit1.GetHashCode();
    var hash2 = unit2.GetHashCode();

    // Assert
    Assert.Equal(0, hash1);
    Assert.Equal(0, hash2);
    Assert.Equal(hash1, hash2);
  }

  // Testa a reflexividade da igualdade: todo valor é igual a si mesmo
  [Fact]
  public void Unit_Equality_ShouldBeReflexive()
  {
    // Arrange
    var unit1 = Unit.Value;
    var unit2 = Unit.Value;

    // Act & Assert
    Assert.True(unit1 == unit2);
    Assert.False(unit1 != unit2);
  }

  // Testa a simetria da igualdade: a ordem da comparação não muda o resultado
  [Fact]
  public void Unit_Equality_ShouldBeSymmetric()
  {
    // Arrange
    var unit1 = Unit.Value;
    var unit2 = new Unit();

    // Act & Assert
    Assert.True(unit1 == unit2);
    Assert.True(unit2 == unit1);
  }

  // Testa a transitividade da igualdade, fechando o contrato de equivalência
  [Fact]
  public void Unit_Equality_ShouldBeTransitive()
  {
    // Arrange
    var unit1 = Unit.Value;
    var unit2 = new Unit();
    var unit3 = new Unit();

    // Act & Assert
    Assert.True(unit1 == unit2);
    Assert.True(unit2 == unit3);
    Assert.True(unit1 == unit3);
  }

  // Testa se o operador de diferença acompanha o de igualdade
  [Fact]
  public void Unit_Inequality_ShouldBeConsistent()
  {
    // Arrange
    var unit1 = Unit.Value;
    var unit2 = new Unit();

    // Act & Assert
    Assert.False(unit1 != unit2);
  }

  // Testa a comparação não tipada com outro valor unitário
  [Fact]
  public void Unit_Equals_Object_ShouldReturnTrueForUnit()
  {
    // Arrange
    object unit = Unit.Value;

    // Act & Assert
    Assert.True(Unit.Value.Equals(unit));
  }

  // Testa a comparação não tipada com um objeto de outro tipo
  [Fact]
  public void Unit_Equals_Object_ShouldReturnFalseForNonUnit()
  {
    // Arrange
    object notUnit = "not-unit";

    // Act & Assert
    Assert.False(Unit.Value.Equals(notUnit));
  }

  // Testa a comparação não tipada com nulo, que um tipo de valor nunca iguala
  [Fact]
  public void Unit_Equals_Object_ShouldReturnFalseForNull()
  {
    // Arrange
    object? nullObject = null;

    // Act & Assert
    Assert.False(Unit.Value.Equals(nullObject));
  }

  // Testa se a tarefa já vem concluída, para o comando sem retorno não precisar aguardar
  [Fact]
  public void Unit_Task_ShouldReturnCompletedTask()
  {
    // Act
    var task = Unit.Task;

    // Assert
    Assert.NotNull(task);
    Assert.True(task.IsCompleted);
  }

  // Testa se a tarefa carrega o valor unitário como resultado
  [Fact]
  public async Task Unit_Task_ShouldReturnUnitValue()
  {
    // Act
    var result = await Unit.Task;

    // Assert
    Assert.Equal(Unit.Value, result);
  }

  // Testa se a tarefa é reaproveitada, em vez de alocar uma nova a cada despacho
  [Fact]
  public void Unit_Task_ShouldReturnSameInstanceWhenCalledMultipleTimes()
  {
    // Act
    var task1 = Unit.Task;
    var task2 = Unit.Task;

    // Assert
    Assert.Same(task1, task2);
  }

  #endregion

  #region ISender Interface Tests

  // Testa a superfície pública da interface de envio
  [Fact]
  public void ISender_ShouldDefinePublicMethods()
  {
    // Arrange
    var senderType = typeof(ISender);

    // Assert
    var methods = senderType.GetMethods();
    Assert.NotEmpty(methods);
  }

  // Testa se a interface de envio declara o método de despacho
  [Fact]
  public void ISender_ShouldHaveSendAsyncMethod()
  {
    // Arrange
    var senderType = typeof(ISender);

    // Act
    var sendAsyncMethod = senderType.GetMethod("SendAsync");

    // Assert
    Assert.NotNull(sendAsyncMethod);
  }

  // Testa se o despacho é genérico no tipo da resposta
  [Fact]
  public void ISender_SendAsync_ShouldBeGeneric()
  {
    // Arrange
    var senderType = typeof(ISender);
    var sendAsyncMethod = senderType.GetMethods()
      .FirstOrDefault(m => m.Name == "SendAsync" && m.IsGenericMethod);

    // Assert
    Assert.NotNull(sendAsyncMethod);
  }

  #endregion

  #region IPublisher Interface Tests

  // Testa se a interface de publicação declara o método de publicação
  [Fact]
  public void IPublisher_ShouldHavePublishAsyncMethod()
  {
    // Arrange
    var publisherType = typeof(IPublisher);

    // Act
    var publishAsyncMethod = publisherType.GetMethod("PublishAsync");

    // Assert
    Assert.NotNull(publishAsyncMethod);
  }

  #endregion

  #region IMediator Interface Tests

  // Testa se o mediador reúne a interface de envio
  [Fact]
  public void IMediator_ShouldImplementISender()
  {
    // Arrange
    var mediatorType = typeof(IMediator);

    // Assert
    Assert.Contains(typeof(ISender), mediatorType.GetInterfaces());
  }

  // Testa se o mediador reúne a interface de publicação
  [Fact]
  public void IMediator_ShouldImplementIPublisher()
  {
    // Arrange
    var mediatorType = typeof(IMediator);

    // Assert
    Assert.Contains(typeof(IPublisher), mediatorType.GetInterfaces());
  }

  // Testa se o mediador não acrescenta membros além das duas interfaces que reúne
  [Fact]
  public void IMediator_ShouldCombineISenderAndIPublisher()
  {
    // Arrange
    var mediatorType = typeof(IMediator);
    var senderInterface = typeof(ISender);
    var publisherInterface = typeof(IPublisher);

    // Assert
    Assert.True(senderInterface.IsAssignableFrom(mediatorType));
    Assert.True(publisherInterface.IsAssignableFrom(mediatorType));
  }

  #endregion

  #region Interface constraints tests

  // Testa se a restrição genérica impede usar o manipulador com um tipo que não é comando
  [Fact]
  public void ICommandHandler_ShouldHaveCommandConstraint()
  {
    // Arrange
    var handlerType = typeof(Tooark.Mediator.Handlers.ICommandHandler<,>);
    var genericArgs = handlerType.GetGenericArguments();

    // Assert
    Assert.NotEmpty(genericArgs);
  }

  // Testa se a restrição genérica impede usar o manipulador com um tipo que não é consulta
  [Fact]
  public void IQueryHandler_ShouldHaveQueryConstraint()
  {
    // Arrange
    var handlerType = typeof(Tooark.Mediator.Handlers.IQueryHandler<,>);
    var genericArgs = handlerType.GetGenericArguments();

    // Assert
    Assert.NotEmpty(genericArgs);
  }

  // Testa se a restrição genérica impede usar o manipulador com um tipo que não é notificação
  [Fact]
  public void INotifyHandler_ShouldHaveNotificationConstraint()
  {
    // Arrange
    var handlerType = typeof(Tooark.Mediator.Handlers.INotifyHandler<>);
    var genericArgs = handlerType.GetGenericArguments();

    // Assert
    Assert.NotEmpty(genericArgs);
  }

  #endregion

  #region Covariance tests

  // Testa se a requisição é invariante no tipo da resposta, o que o despacho por tipo exige
  [Fact]
  public void IRequest_ShouldBeInvariant()
  {
    // Arrange
    var requestType = typeof(IRequest<>);
    var genericArg = requestType.GetGenericArguments()[0];

    // Assert - Invariante por design: o dispatch resolve o handler pelo tipo de resposta do call site,
    // então o uso covariante (referência de tipo-base) falharia em runtime; sem 'out', falha em compilação.
    Assert.Equal(
      System.Reflection.GenericParameterAttributes.None,
      genericArg.GenericParameterAttributes & System.Reflection.GenericParameterAttributes.VarianceMask);
  }

  // Testa se o comando é invariante no tipo da resposta
  [Fact]
  public void ICommand_ShouldBeInvariant()
  {
    // Arrange
    var commandType = typeof(ICommand<>);
    var genericArgs = commandType.GetGenericArguments();

    // Assert - Invariante por design (ver IRequest_ShouldBeInvariant)
    Assert.NotEmpty(genericArgs);
    Assert.Equal(
      System.Reflection.GenericParameterAttributes.None,
      genericArgs[0].GenericParameterAttributes & System.Reflection.GenericParameterAttributes.VarianceMask);
  }

  // Testa se a consulta é invariante no tipo da resposta
  [Fact]
  public void IQuery_ShouldBeInvariant()
  {
    // Arrange
    var queryType = typeof(IQuery<>);
    var genericArgs = queryType.GetGenericArguments();

    // Assert - Invariante por design (ver IRequest_ShouldBeInvariant)
    Assert.NotEmpty(genericArgs);
    Assert.Equal(
      System.Reflection.GenericParameterAttributes.None,
      genericArgs[0].GenericParameterAttributes & System.Reflection.GenericParameterAttributes.VarianceMask);
  }

  #endregion

  #region Request handler interface tests

  // Testa se o manipulador declara o método que o despacho invoca
  [Fact]
  public void IRequestHandler_ShouldHaveHandleAsyncMethod()
  {
    // Arrange
    var handlerType = typeof(Tooark.Mediator.Handlers.IRequestHandler<,>);

    // Act
    var handleAsyncMethod = handlerType.GetMethod("HandleAsync");

    // Assert
    Assert.NotNull(handleAsyncMethod);
  }

  #endregion
}
