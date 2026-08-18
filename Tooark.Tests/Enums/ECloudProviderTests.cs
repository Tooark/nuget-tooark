using Tooark.Enums;
using Tooark.Exceptions;

namespace Tooark.Tests.Enums;

public class ECloudProviderTests
{
  // Teste para converter de int para ECloudProvider e retornar o int e a string corretas.
  [Theory]
  [InlineData(0, "None")]
  [InlineData(1, "AWS")]
  [InlineData(2, "GCP")]
  [InlineData(3, "Azure")]
  public void ECloudProvider_ShouldBeValid_WhenGivenId(int id, string description)
  {
    // Arrange
    ECloudProvider cloudProvider = id;

    // Act
    int cloudProviderId = cloudProvider;
    string cloudProviderDescription = cloudProvider;

    // Assert
    Assert.Equal(id, cloudProviderId);
    Assert.Equal(description, cloudProviderDescription);
  }

  // Teste para converter de string para ECloudProvider e retornar a string e o int corretos.
  [Theory]
  [InlineData("None", 0, "None")]
  [InlineData("AWS", 1, "AWS")]
  [InlineData("GCP", 2, "GCP")]
  [InlineData("Azure", 3, "Azure")]
  [InlineData("Amazon", 1, "AWS")]
  [InlineData("Google", 2, "GCP")]
  [InlineData("Microsoft", 3, "Azure")]
  public void ECloudProvider_ShouldBeValid_WhenGivenDescription(string description, int id, string descriptionExpected)
  {
    // Arrange
    ECloudProvider cloudProvider = description;

    // Act
    string cloudProviderDescription = cloudProvider;
    int cloudProviderId = cloudProvider;

    // Assert
    Assert.Equal(descriptionExpected, cloudProviderDescription);
    Assert.Equal(id, cloudProviderId);
  }

  // Teste para converter de ECloudProvider com ToInt.
  [Fact]
  public void ECloudProvider_ShouldConvertWithToInt()
  {
    // Arrange & Act
    int none = 0;
    int aws = 1;
    int gcp = 2;
    int azure = 3;

    // Assert
    Assert.Equal(none, ECloudProvider.None.ToInt());
    Assert.Equal(aws, ECloudProvider.Amazon.ToInt());
    Assert.Equal(gcp, ECloudProvider.Google.ToInt());
    Assert.Equal(azure, ECloudProvider.Microsoft.ToInt());
  }

  // Teste para converter de ECloudProvider com ToString.
  [Fact]
  public void ECloudProvider_ShouldConvertWithToString()
  {
    // Arrange & Act
    string none = "None";
    string aws = "AWS";
    string gcp = "GCP";
    string azure = "Azure";

    // Assert
    Assert.Equal(none, ECloudProvider.None.ToString());
    Assert.Equal(aws, ECloudProvider.Amazon.ToString());
    Assert.Equal(gcp, ECloudProvider.Google.ToString());
    Assert.Equal(azure, ECloudProvider.Microsoft.ToString());
  }

  // Teste para converter de ECloudProvider para int.
  [Fact]
  public void ECloudProvider_ShouldImplicitConversionToInt()
  {
    // Arrange & Act
    int none = ECloudProvider.None;
    int aws = ECloudProvider.Amazon;
    int gcp = ECloudProvider.Google;
    int azure = ECloudProvider.Microsoft;

    // Assert
    Assert.Equal(0, none);
    Assert.Equal(1, aws);
    Assert.Equal(2, gcp);
    Assert.Equal(3, azure);
  }

  // Teste para converter de ECloudProvider para string.
  [Fact]
  public void ECloudProvider_ShouldImplicitConversionToString()
  {
    // Arrange & Act
    string none = ECloudProvider.None;
    string aws = ECloudProvider.Amazon;
    string gcp = ECloudProvider.Google;
    string azure = ECloudProvider.Microsoft;

    // Assert
    Assert.Equal("None", none);
    Assert.Equal("AWS", aws);
    Assert.Equal("GCP", gcp);
    Assert.Equal("Azure", azure);
  }

  // Teste para retornar o tipo de documento "None" quando o id for inválido.
  [Theory]
  [InlineData(-1)]
  [InlineData(8)]
  public void ECloudProvider_ShouldDefault_WhenIdOutRange(int id)
  {
    // Arrange & Act
    ECloudProvider cloudProvider = id;

    // Assert
    Assert.Equal(cloudProvider, ECloudProvider.None);
  }

  // Teste para retornar o tipo de documento "None" quando a descrição for inválida.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("invalid")]
  [InlineData("unknown")]
  public void ECloudProvider_ShouldDefault_WhenDescriptionOutRange(string? description)
  {
    // Arrange & Act
    ECloudProvider cloudProvider = description!;

    // Assert
    Assert.Equal(cloudProvider, ECloudProvider.None);
  }

  // Teste para garantir que a descricao nao diferencia caixa nem espacos.
  [Theory]
  [InlineData("aws")]
  [InlineData("AWS")]
  [InlineData("Aws")]
  [InlineData("  aws  ")]
  [InlineData("amazon")]
  [InlineData("AMAZON")]
  public void ECloudProvider_ShouldBeAmazon_WhenDescriptionVariesInCase(string description)
  {
    // Arrange & Act
    ECloudProvider cloudProvider = description;

    // Assert
    Assert.Equal(ECloudProvider.Amazon, cloudProvider);
  }

  // Teste para garantir que as demais descricoes tambem ignoram a caixa.
  [Theory]
  [InlineData("gcp", "GCP")]
  [InlineData("google", "GCP")]
  [InlineData("azure", "Azure")]
  [InlineData("microsoft", "Azure")]
  public void ECloudProvider_ShouldResolve_WhenDescriptionIsLowercase(string description, string expected)
  {
    // Arrange & Act
    ECloudProvider cloudProvider = description;

    // Assert
    Assert.Equal(expected, cloudProvider.ToString());
  }

  // Teste para garantir que a conversao de instancia nula falha com erro claro.
  [Fact]
  public void ECloudProvider_ShouldThrow_WhenInstanceIsNull()
  {
    // Arrange
    ECloudProvider cloudProvider = null!;

    // Act & Assert
    var toInt = Assert.Throws<InternalServerErrorException>(() => (int)cloudProvider);
    var toString = Assert.Throws<InternalServerErrorException>(() => (string)cloudProvider);

    Assert.Contains("Invalid.Parameter;null", toInt.GetErrorMessages());
    Assert.Contains("Invalid.Parameter;null", toString.GetErrorMessages());
  }
}
