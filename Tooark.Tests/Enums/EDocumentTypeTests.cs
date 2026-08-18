using Tooark.Enums;
using Tooark.Exceptions;
using Tooark.Validations.Patterns;

namespace Tooark.Tests.Enums;

public class EDocumentTypeTests
{
  // Teste para converter de int para EDocumentType e retornar o int e a string corretas.
  [Theory]
  [InlineData(0, "None")]
  [InlineData(1, "CPF")]
  [InlineData(2, "RG")]
  [InlineData(3, "CNH")]
  [InlineData(4, "CNPJ")]
  [InlineData(5, "CPF_CNPJ")]
  [InlineData(6, "CPF_RG")]
  [InlineData(7, "CPF_RG_CNH")]
  public void EDocumentType_ShouldBeValid_WhenGivenId(int id, string description)
  {
    // Arrange
    EDocumentType documentType = id;

    // Act
    int documentTypeId = documentType;
    string documentTypeDescription = documentType;

    // Assert
    Assert.Equal(id, documentTypeId);
    Assert.Equal(description, documentTypeDescription);
  }

  // Teste para converter de string para EDocumentType e retornar a string e o int corretos.
  [Theory]
  [InlineData("None", 0)]
  [InlineData("CPF", 1)]
  [InlineData("RG", 2)]
  [InlineData("CNH", 3)]
  [InlineData("CNPJ", 4)]
  [InlineData("CPF_CNPJ", 5)]
  [InlineData("CPF_RG", 6)]
  [InlineData("CPF_RG_CNH", 7)]
  public void EDocumentType_ShouldBeValid_WhenGivenDescription(string description, int id)
  {
    // Arrange
    EDocumentType documentType = description;

    // Act
    string documentTypeDescription = documentType;
    int documentTypeId = documentType;

    // Assert
    Assert.Equal(description, documentTypeDescription);
    Assert.Equal(id, documentTypeId);
  }

  // Teste para converter de EDocumentType com ToInt.
  [Fact]
  public void EDocumentType_ShouldConvertWithToInt()
  {
    // Arrange & Act
    int none = 0;
    int cpf = 1;
    int rg = 2;
    int cnh = 3;
    int cnpj = 4;
    int cpfCnpj = 5;
    int cpfRg = 6;
    int cpfRgCnh = 7;

    // Assert
    Assert.Equal(none, EDocumentType.None.ToInt());
    Assert.Equal(cpf, EDocumentType.CPF.ToInt());
    Assert.Equal(rg, EDocumentType.RG.ToInt());
    Assert.Equal(cnh, EDocumentType.CNH.ToInt());
    Assert.Equal(cnpj, EDocumentType.CNPJ.ToInt());
    Assert.Equal(cpfCnpj, EDocumentType.CPF_CNPJ.ToInt());
    Assert.Equal(cpfRg, EDocumentType.CPF_RG.ToInt());
    Assert.Equal(cpfRgCnh, EDocumentType.CPF_RG_CNH.ToInt());
  }

  // Teste para converter de EDocumentType com ToString.
  [Fact]
  public void EDocumentType_ShouldConvertWithToString()
  {
    // Arrange & Act
    string none = "None";
    string cpf = "CPF";
    string rg = "RG";
    string cnh = "CNH";
    string cnpj = "CNPJ";
    string cpfCnpj = "CPF_CNPJ";
    string cpfRg = "CPF_RG";
    string cpfRgCnh = "CPF_RG_CNH";

    // Assert
    Assert.Equal(none, EDocumentType.None.ToString());
    Assert.Equal(cpf, EDocumentType.CPF.ToString());
    Assert.Equal(rg, EDocumentType.RG.ToString());
    Assert.Equal(cnh, EDocumentType.CNH.ToString());
    Assert.Equal(cnpj, EDocumentType.CNPJ.ToString());
    Assert.Equal(cpfCnpj, EDocumentType.CPF_CNPJ.ToString());
    Assert.Equal(cpfRg, EDocumentType.CPF_RG.ToString());
    Assert.Equal(cpfRgCnh, EDocumentType.CPF_RG_CNH.ToString());
  }

  // Teste para converter de EDocumentType com ToString.
  [Fact]
  public void EDocumentType_ShouldConvertWithToRegex()
  {
    // Arrange & Act
    string none = @"^[a-zA-Z0-9.-]*$";
    string cpf = RegexPattern.Cpf;
    string rg = RegexPattern.Rg;
    string cnh = RegexPattern.Cnh;
    string cnpj = RegexPattern.Cnpj;
    string cpfCnpj = RegexPattern.CpfCnpj;
    string cpfRg = RegexPattern.CpfRg;
    string cpfRgCnh = RegexPattern.CpfRgCnh;

    // Assert
    Assert.Equal(none, EDocumentType.None.ToRegex());
    Assert.Equal(cpf, EDocumentType.CPF.ToRegex());
    Assert.Equal(rg, EDocumentType.RG.ToRegex());
    Assert.Equal(cnh, EDocumentType.CNH.ToRegex());
    Assert.Equal(cnpj, EDocumentType.CNPJ.ToRegex());
    Assert.Equal(cpfCnpj, EDocumentType.CPF_CNPJ.ToRegex());
    Assert.Equal(cpfRg, EDocumentType.CPF_RG.ToRegex());
    Assert.Equal(cpfRgCnh, EDocumentType.CPF_RG_CNH.ToRegex());
  }

  // Teste para converter de EDocumentType para int.
  [Fact]
  public void EDocumentType_ShouldImplicitConversionToInt()
  {
    // Arrange & Act
    int none = EDocumentType.None;
    int cpf = EDocumentType.CPF;
    int rg = EDocumentType.RG;
    int cnh = EDocumentType.CNH;
    int cnpj = EDocumentType.CNPJ;
    int cpfCnpj = EDocumentType.CPF_CNPJ;
    int cpfRg = EDocumentType.CPF_RG;
    int cpfRgCnh = EDocumentType.CPF_RG_CNH;

    // Assert
    Assert.Equal(0, none);
    Assert.Equal(1, cpf);
    Assert.Equal(2, rg);
    Assert.Equal(3, cnh);
    Assert.Equal(4, cnpj);
    Assert.Equal(5, cpfCnpj);
    Assert.Equal(6, cpfRg);
    Assert.Equal(7, cpfRgCnh);
  }

  // Teste para converter de EDocumentType para string.
  [Fact]
  public void EDocumentType_ShouldImplicitConversionToString()
  {
    // Arrange & Act
    string none = EDocumentType.None;
    string cpf = EDocumentType.CPF;
    string rg = EDocumentType.RG;
    string cnh = EDocumentType.CNH;
    string cnpj = EDocumentType.CNPJ;
    string cpfCnpj = EDocumentType.CPF_CNPJ;
    string cpfRg = EDocumentType.CPF_RG;
    string cpfRgCnh = EDocumentType.CPF_RG_CNH;

    // Assert
    Assert.Equal("None", none);
    Assert.Equal("CPF", cpf);
    Assert.Equal("RG", rg);
    Assert.Equal("CNH", cnh);
    Assert.Equal("CNPJ", cnpj);
    Assert.Equal("CPF_CNPJ", cpfCnpj);
    Assert.Equal("CPF_RG", cpfRg);
    Assert.Equal("CPF_RG_CNH", cpfRgCnh);
  }

  // Teste para retornar o tipo de documento "None" quando o id for inválido.
  [Theory]
  [InlineData(-1)]
  [InlineData(8)]
  public void EDocumentType_ShouldDefault_WhenIdOutRange(int id)
  {
    // Arrange & Act
    EDocumentType documentType = id;

    // Assert
    Assert.Equal(documentType, EDocumentType.None);
  }

  // Teste para retornar o tipo de documento "None" quando a descrição for inválida.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("invalid")]
  [InlineData("unknown")]
  public void EDocumentType_ShouldDefault_WhenDescriptionOutRange(string? description)
  {
    // Arrange & Act
    EDocumentType documentType = description!;

    // Assert
    Assert.Equal(documentType, EDocumentType.None);
  }

  // Teste para garantir que a conversao de instancia nula falha com erro claro.
  [Fact]
  public void EDocumentType_ShouldThrow_WhenInstanceIsNull()
  {
    // Arrange
    EDocumentType documentType = null!;

    // Act & Assert
    var toInt = Assert.Throws<InternalServerErrorException>(() => (int)documentType);
    var toString = Assert.Throws<InternalServerErrorException>(() => (string)documentType);

    Assert.Contains("Invalid.Parameter;null", toInt.GetErrorMessages());
    Assert.Contains("Invalid.Parameter;null", toString.GetErrorMessages());
  }

  // Teste para garantir que a descricao nao diferencia caixa nem espacos.
  [Theory]
  [InlineData("cpf")]
  [InlineData("CPF")]
  [InlineData("  Cpf  ")]
  public void EDocumentType_ShouldBeCpf_WhenDescriptionVariesInCase(string description)
  {
    // Arrange & Act
    EDocumentType documentType = description;

    // Assert
    Assert.Equal(EDocumentType.CPF, documentType);
  }

  // Teste para garantir que IsValid nao lanca com entrada invalida.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("ABCDEFGHIJK")]
  [InlineData("AB.CDE.FGH")]
  [InlineData("nao e documento")]
  public void EDocumentType_IsValid_ShouldNotThrow_WhenValueIsInvalid(string? value)
  {
    // Act & Assert - antes estourava FormatException e NullReferenceException
    Assert.False(EDocumentType.CNH.IsValid(value!));
    Assert.False(EDocumentType.RG.IsValid(value!));
    Assert.False(EDocumentType.CPF.IsValid(value!));
    Assert.False(EDocumentType.CNPJ.IsValid(value!));
  }

  // Teste para garantir que CNH e RG conferem o digito verificador.
  [Theory]
  [InlineData("02650306461", true)]
  [InlineData("12345678900", true)]
  [InlineData("12345678901", false)]
  public void EDocumentType_Cnh_ShouldValidateCheckDigits(string value, bool expected)
  {
    // Act & Assert
    Assert.Equal(expected, EDocumentType.CNH.IsValid(value));
  }

  // Teste para garantir que o RG confere o digito quando informado.
  [Theory]
  [InlineData("12.345.678-2", true)]
  [InlineData("12.345.678", true)]
  [InlineData("10.000.006-X", true)]
  [InlineData("12.345.678-9", false)]
  public void EDocumentType_Rg_ShouldValidateCheckDigit(string value, bool expected)
  {
    // Act & Assert
    Assert.Equal(expected, EDocumentType.RG.IsValid(value));
  }
}
