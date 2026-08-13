using Tooark.Mediator.Abstractions;

namespace Tooark.Tests.Mediator.Abstractions;

public class UnitTests
{
  [Fact]
  public void Value_ShouldReturnUnit()
  {
    // Act
    var value = Unit.Value;

    // Assert
    Assert.Equal(new Unit(), value);
  }

  [Fact]
  public async Task Task_ShouldReturnCompletedUnitTask()
  {
    // Act
    var result = await Unit.Task;

    // Assert
    Assert.Equal(Unit.Value, result);
  }

  [Fact]
  public void EqualsObject_ShouldReturnTrue_WhenObjectIsUnit()
  {
    // Arrange
    object other = Unit.Value;

    // Act
    var result = Unit.Value.Equals(other);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void EqualsObject_ShouldReturnFalse_WhenObjectIsNotUnit()
  {
    // Act
    var result = Unit.Value.Equals("not-unit");

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void EqualsUnit_ShouldAlwaysReturnTrue()
  {
    // Act
    var result = Unit.Value.Equals(new Unit());

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void GetHashCode_ShouldReturnZero()
  {
    // Act
    var hashCode = Unit.Value.GetHashCode();

    // Assert
    Assert.Equal(0, hashCode);
  }

  [Fact]
  public void EqualityOperator_ShouldAlwaysReturnTrue()
  {
    // Act
    var result = Unit.Value == new Unit();

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void InequalityOperator_ShouldAlwaysReturnFalse()
  {
    // Act
    var result = Unit.Value != new Unit();

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void ToString_ShouldReturnParentheses()
  {
    // Act
    var result = Unit.Value.ToString();

    // Assert
    Assert.Equal("()", result);
  }

  [Fact]
  public async Task Task_ShouldReturnSameInstance_OnEveryAccess()
  {
    // Act - antes cada acesso criava uma nova Task, contrariando a documentação do membro
    var first = Unit.Task;
    var second = Unit.Task;

    // Assert
    Assert.Same(first, second);
    Assert.True(first.IsCompletedSuccessfully);
    Assert.Equal(Unit.Value, await first);
  }
}
