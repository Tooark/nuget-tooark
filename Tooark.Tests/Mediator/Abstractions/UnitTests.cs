using Tooark.Mediator.Abstractions;

namespace Tooark.Tests.Mediator.Abstractions;

public class UnitTests
{
  // Testa se a propriedade estática devolve o valor do tipo unitário
  [Fact]
  public void Value_ShouldReturnUnit()
  {
    // Act
    var value = Unit.Value;

    // Assert
    Assert.Equal(new Unit(), value);
  }

  // Testa se a tarefa já vem concluída, para o manipulador sem retorno não precisar aguardar
  [Fact]
  public async Task Task_ShouldReturnCompletedUnitTask()
  {
    // Act
    var result = await Unit.Task;

    // Assert
    Assert.Equal(Unit.Value, result);
  }

  // Testa a comparação com um objeto que é do tipo unitário
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

  // Testa a comparação com um objeto de outro tipo
  [Fact]
  public void EqualsObject_ShouldReturnFalse_WhenObjectIsNotUnit()
  {
    // Act
    var result = Unit.Value.Equals("not-unit");

    // Assert
    Assert.False(result);
  }

  // Testa se duas instâncias do tipo unitário são sempre iguais — ele tem um único valor possível
  [Fact]
  public void EqualsUnit_ShouldAlwaysReturnTrue()
  {
    // Act
    var result = Unit.Value.Equals(new Unit());

    // Assert
    Assert.True(result);
  }

  // Testa se o código de dispersão é constante, coerente com haver um único valor
  [Fact]
  public void GetHashCode_ShouldReturnZero()
  {
    // Act
    var hashCode = Unit.Value.GetHashCode();

    // Assert
    Assert.Equal(0, hashCode);
  }

  // Testa o operador de igualdade, que acompanha o Equals
  [Fact]
  public void EqualityOperator_ShouldAlwaysReturnTrue()
  {
    // Act
    var result = Unit.Value == new Unit();

    // Assert
    Assert.True(result);
  }

  // Testa o operador de diferença, que acompanha o Equals
  [Fact]
  public void InequalityOperator_ShouldAlwaysReturnFalse()
  {
    // Act
    var result = Unit.Value != new Unit();

    // Assert
    Assert.False(result);
  }

  // Testa a representação textual, que segue a convenção do tipo unitário em outras linguagens
  [Fact]
  public void ToString_ShouldReturnParentheses()
  {
    // Act
    var result = Unit.Value.ToString();

    // Assert
    Assert.Equal("()", result);
  }

  // Testa se a tarefa é reaproveitada entre acessos, em vez de alocar uma nova a cada despacho
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
