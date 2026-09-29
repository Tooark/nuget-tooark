using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tooark.Securities.Dtos;

namespace Tooark.Tests.Securities.Dtos;

public class UserTokenDtoTests
{
  // Método auxiliar para criar um JwtSecurityToken com os dados necessários
  private static JwtSecurityToken CreateToken(string id, string login, string security)
  {
    var claims = new[]
    {
      new Claim("id", id),
      new Claim("login", login),
      new Claim("security", security)
    };

    return new JwtSecurityToken(claims: claims);
  }

  // Teste construtor com JwtSecurityToken
  [Fact]
  public void Constructor_WithToken_SetsPropertiesCorrectly()
  {
    // Arrange
    var token = CreateToken("1", "user", "sec");

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal("1", dto.Id);
    Assert.Equal("user", dto.Login);
    Assert.Equal("sec", dto.Security);
    Assert.Equal(string.Empty, dto.ErrorToken);
  }

  // Teste construtor com string erro
  [Fact]
  public void Constructor_WithError_SetsErrorToken()
  {
    // Arrange
    var errMessage = "error message";

    // Act
    var dto = new UserTokenDto(errMessage);

    // Assert
    Assert.Equal(errMessage, dto.ErrorToken);
    Assert.Equal(string.Empty, dto.Id);
    Assert.Equal(string.Empty, dto.Login);
    Assert.Equal(string.Empty, dto.Security);
  }

  // Teste função para obter Guid do Id com Id válido
  [Fact]
  public void GetGuidId_ReturnsGuid_WhenValid()
  {
    // Arrange
    var id = Guid.NewGuid();
    var token = CreateToken(id.ToString(), "user", "sec");

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(id.ToString(), dto.Id);
    Assert.Equal(id, dto.GetGuidId);
  }

  // Teste função para obter Guid do Id com Id inválido
  [Fact]
  public void GetGuidId_ReturnsEmpty_WhenInvalid()
  {
    // Arrange
    var id = "not-a-guid";
    var token = CreateToken(id.ToString(), "user", "sec");

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(id, dto.Id);
    Assert.Equal(Guid.Empty, dto.GetGuidId);
  }

  // Teste função para obter Int do Id com Id válido
  [Fact]
  public void GetIntId_ReturnsInt_WhenValid()
  {
    // Arrange
    var id = 123;
    var token = CreateToken(id.ToString(), "user", "sec");

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(id.ToString(), dto.Id);
    Assert.Equal(id, dto.GetIntId);
  }

  // Teste função para obter Int do Id com Id inválido
  [Fact]
  public void GetIntId_ReturnsZero_WhenInvalid()
  {
    // Arrange
    var id = "not-a-int";
    var token = CreateToken(id.ToString(), "user", "sec");

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(id, dto.Id);
    Assert.Equal(0, dto.GetIntId);
  }

  // Teste função para obter Guid do Security com Security válido
  [Fact]
  public void GetGuidSecurity_ReturnsGuid_WhenValid()
  {
    // Arrange
    var security = Guid.NewGuid();
    var token = CreateToken("sec", "user", security.ToString());

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(security.ToString(), dto.Security);
    Assert.Equal(security, dto.GetGuidSecurity);
  }

  // Teste função para obter Guid do Security com Security inválido
  [Fact]
  public void GetGuidSecurity_ReturnsEmpty_WhenInvalid()
  {
    // Arrange
    var security = "not-a-guid";
    var token = CreateToken("sec", "user", security.ToString());

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(security, dto.Security);
    Assert.Equal(Guid.Empty, dto.GetGuidSecurity);
  }

  // Teste função para obter Int do Security com Security válido
  [Fact]
  public void GetIntSecurity_ReturnsInt_WhenValid()
  {
    // Arrange
    var security = 123;
    var token = CreateToken("sec", "user", security.ToString());

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(security.ToString(), dto.Security);
    Assert.Equal(security, dto.GetIntSecurity);
  }

  // Teste função para obter Int do Security com Security inválido
  [Fact]
  public void GetIntSecurity_ReturnsZero_WhenInvalid()
  {
    // Arrange
    var security = "not-a-int";
    var token = CreateToken("sec", "user", security.ToString());

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(security, dto.Security);
    Assert.Equal(0, dto.GetIntSecurity);
  }

  // Teste: claims ausentes resultam em valores vazios (sem lançar exceção)
  [Fact]
  public void Constructor_MissingClaims_ShouldUseEmptyValues()
  {
    // Arrange: token sem as claims "id" e "security"
    var claims = new[]
    {
      new Claim("login", "user")
      // note: intentionally omitting "id" and "security"
    };

    var token = new JwtSecurityToken(claims: claims);

    // Act - antes lançava InvalidOperationException, virando erro interno na validação
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(string.Empty, dto.Id);
    Assert.Equal("user", dto.Login);
    Assert.Equal(string.Empty, dto.Security);
    Assert.Equal(string.Empty, dto.ErrorToken);
  }

  // Teste: construtor com JsonWebToken (handler atual) extrai as claims
  [Fact]
  public void Constructor_WithJsonWebToken_ShouldExtractClaims()
  {
    // Arrange - constrói um token com as claims esperadas e o materializa como JsonWebToken
    var claims = new[]
    {
      new Claim("id", "1"),
      new Claim("login", "user"),
      new Claim("security", "2")
    };
    var handler = new JwtSecurityTokenHandler();
    var encoded = handler.WriteToken(new JwtSecurityToken(claims: claims));
    var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(encoded);

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal("1", dto.Id);
    Assert.Equal("user", dto.Login);
    Assert.Equal("2", dto.Security);
    Assert.Equal(string.Empty, dto.ErrorToken);
  }

  // Teste: construtor com JsonWebToken sem as claims esperadas usa valores vazios
  [Fact]
  public void Constructor_WithJsonWebToken_MissingClaims_ShouldUseEmptyValues()
  {
    // Arrange - token sem as claims id/login/security
    var handler = new JwtSecurityTokenHandler();
    var encoded = handler.WriteToken(new JwtSecurityToken(claims: [new Claim("login", "user")]));
    var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(encoded);

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(string.Empty, dto.Id);
    Assert.Equal("user", dto.Login);
    Assert.Equal(string.Empty, dto.Security);
  }

  // Teste: claims extras do token ficam disponíveis no DTO
  [Fact]
  public void Claims_ShouldExposeExtraClaims()
  {
    // Arrange
    var token = new JwtSecurityToken(claims:
    [
      new Claim("id", "1"),
      new Claim("login", "user"),
      new Claim("security", "2"),
      new Claim("department", "IT")
    ]);

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal(4, dto.Claims.Count);
    Assert.Equal("IT", dto.GetClaim("department"));
    Assert.True(dto.HasClaim("department"));
  }

  // Teste: claims extras chegam pelo construtor com JsonWebToken (handler atual)
  [Fact]
  public void Claims_WithJsonWebToken_ShouldExposeExtraClaims()
  {
    // Arrange
    var handler = new JwtSecurityTokenHandler();
    var encoded = handler.WriteToken(new JwtSecurityToken(claims:
    [
      new Claim("id", "1"),
      new Claim("department", "IT")
    ]));
    var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(encoded);

    // Act
    var dto = new UserTokenDto(token);

    // Assert
    Assert.Equal("1", dto.Id);
    Assert.Equal("IT", dto.GetClaim("department"));
  }

  // Teste: claim ausente resulta em vazio, sem lançar exceção
  [Fact]
  public void GetClaim_ReturnsEmpty_WhenMissing()
  {
    // Arrange
    var dto = new UserTokenDto(CreateToken("1", "user", "sec"));

    // Act & Assert
    Assert.Equal(string.Empty, dto.GetClaim("department"));
    Assert.Empty(dto.GetClaims("department"));
    Assert.False(dto.HasClaim("department"));
  }

  // Teste: claim presente com valor vazio é diferente de claim ausente
  [Fact]
  public void HasClaim_ReturnsTrue_WhenValueIsEmpty()
  {
    // Arrange
    var dto = new UserTokenDto(new JwtSecurityToken(claims: [new Claim("department", "")]));

    // Act & Assert
    Assert.True(dto.HasClaim("department"));
    Assert.Equal(string.Empty, dto.GetClaim("department"));
  }

  // Teste: claim com vários valores devolve todos, na ordem do token
  [Fact]
  public void GetClaims_ReturnsAllValues_WhenMultiValued()
  {
    // Arrange
    var dto = new UserTokenDto(new JwtSecurityToken(claims:
    [
      new Claim("role", "admin"),
      new Claim("role", "user")
    ]));

    // Act
    var roles = dto.GetClaims("role");

    // Assert
    Assert.Equal(["admin", "user"], roles);
    Assert.Equal("admin", dto.GetClaim("role"));
  }

  // Teste: conversão tipada de claims válidas
  [Fact]
  public void GetClaimTyped_ReturnsParsedValue_WhenValid()
  {
    // Arrange
    var tenant = Guid.NewGuid();
    var dto = new UserTokenDto(new JwtSecurityToken(claims:
    [
      new Claim("tenant", tenant.ToString()),
      new Claim("level", "3"),
      new Claim("active", "true"),
      new Claim("limit", "1234.56")
    ]));

    // Act & Assert
    Assert.Equal(tenant, dto.GetClaim<Guid>("tenant"));
    Assert.Equal(3, dto.GetClaim<int>("level"));
    Assert.True(dto.GetClaim<bool>("active"));
    Assert.Equal(1234.56m, dto.GetClaim<decimal>("limit"));
  }

  // Teste: conversão tipada devolve o padrão do tipo quando a claim é ausente ou inválida
  [Fact]
  public void GetClaimTyped_ReturnsDefault_WhenMissingOrInvalid()
  {
    // Arrange
    var dto = new UserTokenDto(new JwtSecurityToken(claims: [new Claim("tenant", "not-a-guid")]));

    // Act & Assert
    Assert.Equal(Guid.Empty, dto.GetClaim<Guid>("tenant"));
    Assert.Equal(0, dto.GetClaim<int>("level"));
    Assert.False(dto.GetClaim<bool>("active"));
  }

  // Teste: DTO de erro não possui claims
  [Fact]
  public void Claims_IsEmpty_WhenError()
  {
    // Act
    var dto = new UserTokenDto("Token.Invalid");

    // Assert
    Assert.Empty(dto.Claims);
    Assert.Equal(string.Empty, dto.GetClaim("id"));
    Assert.Equal(Guid.Empty, dto.GetClaim<Guid>("id"));
  }
}
