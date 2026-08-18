using Tooark.Validations;

namespace Tooark.Tests.Validations;

public class DocumentDigitTests
{
  private const string Property = "Documento";

  // Teste para verificar CPF com dígitos verificadores corretos.
  [Theory]
  [InlineData("529.982.247-25")]
  [InlineData("111.444.777-35")]
  public void IsCpf_ShouldNotAddNotification_WhenCheckDigitsAreValid(string value)
  {
    // Act
    var validation = new Validation().IsCpf(value, Property);

    // Assert
    Assert.True(validation.IsValid);
  }

  // Teste para verificar CPF com dígitos verificadores incorretos.
  [Theory]
  [InlineData("529.982.247-26")]
  [InlineData("529.982.247-15")]
  [InlineData("111.444.777-30")]
  public void IsCpf_ShouldAddNotification_WhenCheckDigitsAreInvalid(string value)
  {
    // Act - antes bastava o formato, então qualquer par de dígitos passava
    var validation = new Validation().IsCpf(value, Property);

    // Assert
    Assert.False(validation.IsValid);
  }

  // Teste para verificar CPF composto por um único dígito repetido.
  [Theory]
  [InlineData("111.111.111-11")]
  [InlineData("000.000.000-00")]
  [InlineData("999.999.999-99")]
  public void IsCpf_ShouldAddNotification_WhenAllDigitsAreEqual(string value)
  {
    // Act - sequências repetidas satisfazem o módulo 11, mas não são CPF válido
    var validation = new Validation().IsCpf(value, Property);

    // Assert
    Assert.False(validation.IsValid);
  }

  // Teste para verificar CNPJ numérico com dígitos verificadores corretos.
  [Theory]
  [InlineData("11.222.333/0001-81")]
  [InlineData("34.028.316/0001-03")]
  public void IsCnpj_ShouldNotAddNotification_WhenNumericCheckDigitsAreValid(string value)
  {
    // Act
    var validation = new Validation().IsCnpj(value, Property);

    // Assert
    Assert.True(validation.IsValid);
  }

  // Teste para verificar o CNPJ alfanumérico do material de referência do Serpro.
  [Fact]
  public void IsCnpj_ShouldNotAddNotification_WhenAlphanumericCheckDigitsAreValid()
  {
    // Arrange - exemplo do cálculo publicado: somatórios 459 e 424, dígitos 3 e 5
    var value = "12.ABC.345/01DE-35";

    // Act
    var validation = new Validation().IsCnpj(value, Property);

    // Assert
    Assert.True(validation.IsValid);
  }

  // Teste para verificar CNPJ com dígitos verificadores incorretos.
  [Theory]
  [InlineData("11.222.333/0001-82")]
  [InlineData("12.ABC.345/01DE-34")]
  [InlineData("12.ABC.345/01DE-53")]
  public void IsCnpj_ShouldAddNotification_WhenCheckDigitsAreInvalid(string value)
  {
    // Act
    var validation = new Validation().IsCnpj(value, Property);

    // Assert
    Assert.False(validation.IsValid);
  }

  // Teste para verificar CNPJ composto por um único caractere repetido.
  [Theory]
  [InlineData("00.000.000/0000-00")]
  [InlineData("AA.AAA.AAA/AAAA-45")]
  public void IsCnpj_ShouldAddNotification_WhenAllCharactersAreEqual(string value)
  {
    // Act - os dígitos informados conferem com o cálculo, então só a regra de repetição reprova
    var validation = new Validation().IsCnpj(value, Property);

    // Assert
    Assert.False(validation.IsValid);
  }

  // Teste para verificar que a caixa das letras do CNPJ alfanumérico é normalizada.
  [Theory]
  [InlineData("12.abc.345/01de-35")]
  [InlineData("12.Abc.345/01De-35")]
  public void IsCnpj_ShouldNotAddNotification_WhenAlphanumericIsLowerCase(string value)
  {
    // Act - o cálculo parte do ASCII das maiúsculas, então a caixa é normalizada antes da conta
    var validation = new Validation().IsCnpj(value, Property);

    // Assert
    Assert.True(validation.IsValid);
  }

  // Teste para verificar documento nulo ou vazio.
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void IsCpfAndIsCnpj_ShouldAddNotification_WhenValueIsNullOrEmpty(string? value)
  {
    // Act
    var cpf = new Validation().IsCpf(value!, Property);
    var cnpj = new Validation().IsCnpj(value!, Property);

    // Assert
    Assert.False(cpf.IsValid);
    Assert.False(cnpj.IsValid);
  }

  // Teste para verificar o documento combinado de CPF ou CNPJ.
  [Theory]
  [InlineData("529.982.247-25", true)]
  [InlineData("11.222.333/0001-81", true)]
  [InlineData("12.ABC.345/01DE-35", true)]
  [InlineData("529.982.247-26", false)]
  [InlineData("11.222.333/0001-82", false)]
  public void IsCpfCnpj_ShouldValidateCheckDigits_ForBothDocuments(string value, bool expected)
  {
    // Act
    var validation = new Validation().IsCpfCnpj(value, Property);

    // Assert
    Assert.Equal(expected, validation.IsValid);
  }

  // Teste para verificar RG com digito correto e RG informado sem digito.
  [Theory]
  [InlineData("12.345.678-2")]
  [InlineData("12.345.678")]
  public void IsRg_ShouldNotAddNotification_WhenFormatIsValid(string value)
  {
    // Act - o dígito é opcional; informado, precisa conferir
    var validation = new Validation().IsRg(value, Property);

    // Assert
    Assert.True(validation.IsValid);
  }

  // Teste para verificar o documento combinado de CPF ou RG.
  [Theory]
  [InlineData("529.982.247-25", true)]
  [InlineData("12.345.678-2", true)]
  [InlineData("529.982.247-26", false)]
  public void IsCpfRg_ShouldValidateCpfCheckDigits_AndRgFormat(string value, bool expected)
  {
    // Act
    var validation = new Validation().IsCpfRg(value, Property);

    // Assert
    Assert.Equal(expected, validation.IsValid);
  }

  // Teste para verificar o documento combinado de CPF, RG ou CNH.
  [Theory]
  [InlineData("529.982.247-25", true)]
  [InlineData("12.345.678-2", true)]
  [InlineData("12345678900", true)]
  [InlineData("529.982.247-26", false)]
  public void IsCpfRgCnh_ShouldValidateCpfCheckDigits_AndOtherFormats(string value, bool expected)
  {
    // Act
    var validation = new Validation().IsCpfRgCnh(value, Property);

    // Assert
    Assert.Equal(expected, validation.IsValid);
  }
}
