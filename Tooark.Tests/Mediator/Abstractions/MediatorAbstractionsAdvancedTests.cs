using Tooark.Mediator.Abstractions;

namespace Tooark.Tests.Mediator.Abstractions;

/// <summary>
/// Testes avançados para as interfaces abstratas do Mediator.
/// </summary>
public class MediatorAbstractionsAdvancedTests
{
  #region IRequest Interface Tests

  [Fact]
  public void IRequest_ShouldBeMarkerInterface()
  {
    // Arrange
    var requestType = typeof(IRequest);

    // Assert
    Assert.True(requestType.IsInterface);
    Assert.False(requestType.GetMethods().Any());
  }

  [Fact]
  public void IRequest_Generic_ShouldDefineGenericContract()
  {
    // Arrange
    var requestGenericType = typeof(IRequest<>);

    // Assert
    Assert.True(requestGenericType.IsInterface);
    Assert.True(requestGenericType.IsGenericTypeDefinition);
  }

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

  [Fact]
  public void ICommand_ShouldImplementIRequestOfUnit()
  {
    // Arrange
    var commandType = typeof(ICommand);
    var expectedInterface = typeof(IRequest<Unit>);

    // Assert
    Assert.Contains(expectedInterface, commandType.GetInterfaces());
  }

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

  [Fact]
  public void Unit_ShouldBeValueType()
  {
    // Assert
    Assert.True(typeof(Unit).IsValueType);
  }

  [Fact]
  public void Unit_ShouldImplementEquatable()
  {
    // Arrange
    var unitType = typeof(Unit);
    var equatableType = typeof(IEquatable<Unit>);

    // Assert
    Assert.Contains(equatableType, unitType.GetInterfaces());
  }

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

  [Fact]
  public void Unit_ToString_ShouldReturnEmptyParentheses()
  {
    // Act
    var result = Unit.Value.ToString();

    // Assert
    Assert.Equal("()", result);
  }

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

  [Fact]
  public void Unit_Inequality_ShouldBeConsistent()
  {
    // Arrange
    var unit1 = Unit.Value;
    var unit2 = new Unit();

    // Act & Assert
    Assert.False(unit1 != unit2);
  }

  [Fact]
  public void Unit_Equals_Object_ShouldReturnTrueForUnit()
  {
    // Arrange
    object unit = Unit.Value;

    // Act & Assert
    Assert.True(Unit.Value.Equals(unit));
  }

  [Fact]
  public void Unit_Equals_Object_ShouldReturnFalseForNonUnit()
  {
    // Arrange
    object notUnit = "not-unit";

    // Act & Assert
    Assert.False(Unit.Value.Equals(notUnit));
  }

  [Fact]
  public void Unit_Equals_Object_ShouldReturnFalseForNull()
  {
    // Arrange
    object? nullObject = null;

    // Act & Assert
    Assert.False(Unit.Value.Equals(nullObject));
  }

  [Fact]
  public void Unit_Task_ShouldReturnCompletedTask()
  {
    // Act
    var task = Unit.Task;

    // Assert
    Assert.NotNull(task);
    Assert.True(task.IsCompleted);
  }

  [Fact]
  public async Task Unit_Task_ShouldReturnUnitValue()
  {
    // Act
    var result = await Unit.Task;

    // Assert
    Assert.Equal(Unit.Value, result);
  }

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

  [Fact]
  public void ISender_ShouldDefinePublicMethods()
  {
    // Arrange
    var senderType = typeof(ISender);

    // Assert
    var methods = senderType.GetMethods();
    Assert.NotEmpty(methods);
  }

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

  [Fact]
  public void IMediator_ShouldImplementISender()
  {
    // Arrange
    var mediatorType = typeof(IMediator);

    // Assert
    Assert.Contains(typeof(ISender), mediatorType.GetInterfaces());
  }

  [Fact]
  public void IMediator_ShouldImplementIPublisher()
  {
    // Arrange
    var mediatorType = typeof(IMediator);

    // Assert
    Assert.Contains(typeof(IPublisher), mediatorType.GetInterfaces());
  }

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

  [Fact]
  public void ICommandHandler_ShouldHaveCommandConstraint()
  {
    // Arrange
    var handlerType = typeof(Tooark.Mediator.Handlers.ICommandHandler<,>);
    var genericArgs = handlerType.GetGenericArguments();

    // Assert
    Assert.NotEmpty(genericArgs);
  }

  [Fact]
  public void IQueryHandler_ShouldHaveQueryConstraint()
  {
    // Arrange
    var handlerType = typeof(Tooark.Mediator.Handlers.IQueryHandler<,>);
    var genericArgs = handlerType.GetGenericArguments();

    // Assert
    Assert.NotEmpty(genericArgs);
  }

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
