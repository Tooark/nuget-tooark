using Tooark.Securities.Dtos;
using Tooark.Securities.Extensions;
using Tooark.Securities.Options;

namespace Tooark.Tests.Securities.Extensions;

public class RoleClaimsExtensionTests
{
  // Teste para buscar os claims de um JwtTokenDto
  [Fact]
  public void GetClaims_DeveRetornarClaimsCorretos()
  {
    // Arrange
    var id = Guid.NewGuid().ToString();
    var login = "usuarioTeste";
    var security = Guid.NewGuid().ToString();
    var jwtToken = new JwtTokenDto(id, login, security);

    // Act
    var claims = jwtToken.GetClaims().ToList();

    // Assert
    Assert.Equal(3, claims.Count);
    Assert.Contains(claims, c => c.Type == "id" && c.Value == id);
    Assert.Contains(claims, c => c.Type == "login" && c.Value == login);
    Assert.Contains(claims, c => c.Type == "security" && c.Value == security);
  }

  // Teste para buscar os claims de um JwtTokenDto com valores vazios
  [Fact]
  public void GetClaims_DeveRetornarClaimsComValoresVazios()
  {
    // Arrange
    var jwtToken = new JwtTokenDto("", "", "");

    // Act
    var claims = jwtToken.GetClaims().ToList();

    // Assert
    Assert.Equal(3, claims.Count);
    Assert.Contains(claims, c => c.Type == "id" && c.Value == "");
    Assert.Contains(claims, c => c.Type == "login" && c.Value == "");
    Assert.Contains(claims, c => c.Type == "security" && c.Value == "");
  }

  // Testa se a sobrecarga com sinalizadores emite só as claims padrão quando as legadas estão desligadas
  [Fact]
  public void GetClaims_ShouldEmitStandardClaimsOnly_WhenLegacyIsOff()
  {
    // Arrange
    var jwtToken = new JwtTokenDto("42", "ana", "7");

    // Act
    var claims = jwtToken.GetClaims(emitLegacyClaims: false, emitStandardClaims: true).ToList();

    // Assert
    Assert.Equal(3, claims.Count);
    Assert.Contains(claims, c => c.Type == "sub" && c.Value == "42");
    Assert.Contains(claims, c => c.Type == "preferred_username" && c.Value == "ana");
    Assert.Contains(claims, c => c.Type == "security" && c.Value == "7");
    Assert.DoesNotContain(claims, c => c.Type == "id");
    Assert.DoesNotContain(claims, c => c.Type == "login");
  }

  // Testa se os dois conjuntos saem juntos quando os dois sinalizadores estão ligados
  [Fact]
  public void GetClaims_ShouldEmitBothClaimSets_WhenBothAreOn()
  {
    // Arrange
    var jwtToken = new JwtTokenDto("42", "ana", "7");

    // Act
    var claims = jwtToken.GetClaims(emitLegacyClaims: true, emitStandardClaims: true).ToList();

    // Assert
    Assert.Equal(5, claims.Count);
    Assert.Contains(claims, c => c.Type == "id" && c.Value == "42");
    Assert.Contains(claims, c => c.Type == "sub" && c.Value == "42");
    Assert.Contains(claims, c => c.Type == "login" && c.Value == "ana");
    Assert.Contains(claims, c => c.Type == "preferred_username" && c.Value == "ana");
    Assert.Single(claims, c => c.Type == "security");
  }

  // Testa se com os dois sinalizadores desligados só a security é emitida (o serviço barra essa configuração no startup)
  [Fact]
  public void GetClaims_ShouldEmitSecurityOnly_WhenBothAreOff()
  {
    // Arrange
    var jwtToken = new JwtTokenDto("42", "ana", "7");

    // Act
    var claims = jwtToken.GetClaims(emitLegacyClaims: false, emitStandardClaims: false).ToList();

    // Assert
    Assert.Single(claims);
    Assert.Equal("security", claims[0].Type);
  }

  // Testa se a sobrecarga com as opções segue os sinalizadores configurados
  [Fact]
  public void GetClaims_WithOptions_ShouldFollowConfiguredFlags()
  {
    // Arrange
    var jwtToken = new JwtTokenDto("42", "ana", "7");
    var options = new JwtOptions { EmitLegacyClaims = false, EmitStandardClaims = true };

    // Act
    var claims = jwtToken.GetClaims(options).ToList();

    // Assert
    Assert.Contains(claims, c => c.Type == "sub" && c.Value == "42");
    Assert.DoesNotContain(claims, c => c.Type == "id");
  }
}
