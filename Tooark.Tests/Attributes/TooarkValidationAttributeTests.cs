using System.ComponentModel.DataAnnotations;
using Tooark.Attributes;
using Tooark.Exceptions;

namespace Tooark.Tests.Attributes;

/// <summary>
/// Testes do comportamento comum aos atributos de validação.
/// </summary>
public class TooarkValidationAttributeTests
{
  // Atributo que expõe o auxiliar protegido, para exercitar a guarda de tempo limite com um padrão
  // deliberadamente patológico — nenhum padrão do pacote retrocede mais, então sem isso a guarda
  // ficaria sem cobertura e o próximo a mexer não saberia se ainda funciona.
  private sealed class ComPadraoPatologico() : TooarkValidationAttribute("Patologico")
  {
    // Classes quantificadas aninhadas com conjuntos sobrepostos: o formato clássico do retrocesso
    private const string Patologico = "^(a+)+$";

    protected override bool IsSatisfied(string value) => Matches(value, Patologico);
  }

  // Modelo com a mensagem de erro configurada pelo consumidor
  private class ComMensagemPropria
  {
    [EmailValidation(ErrorMessage = "Informe um e-mail corporativo")]
    public string? Email { get; set; }
  }

  // Modelo sem mensagem configurada
  private class SemMensagemPropria
  {
    [EmailValidation]
    public string? Email { get; set; }
  }

  // Executa a validação completa do modelo e devolve as mensagens
  private static List<string?> Validar(object modelo)
  {
    var resultados = new List<ValidationResult>();
    Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, true);

    return [.. resultados.Select(r => r.ErrorMessage)];
  }

  // Testa se a mensagem configurada pelo consumidor é preservada
  [Fact]
  public void ErrorMessage_ShouldBePreserved_WhenConfiguredByConsumer()
  {
    // Arrange & Act
    var mensagens = Validar(new ComMensagemPropria { Email = "nao-e-email" });

    // Assert
    Assert.Equal(["Informe um e-mail corporativo"], mensagens);
  }

  // Testa se a mensagem configurada continua valendo depois de várias validações
  [Fact]
  public void ErrorMessage_ShouldSurviveRepeatedValidations()
  {
    // Arrange
    var modelo = new ComMensagemPropria();

    // Act
    modelo.Email = "valido@teste.com";
    var primeira = Validar(modelo);

    modelo.Email = "nao-e-email";
    var segunda = Validar(modelo);

    modelo.Email = "nao-e-email";
    var terceira = Validar(modelo);

    // Assert
    Assert.Empty(primeira);
    Assert.Equal(["Informe um e-mail corporativo"], segunda);
    Assert.Equal(["Informe um e-mail corporativo"], terceira);
  }

  // Testa se a chave padrão é usada quando o consumidor não configura mensagem
  [Fact]
  public void ErrorMessage_ShouldUseDefaultKey_WhenNotConfigured()
  {
    // Arrange & Act
    var mensagens = Validar(new SemMensagemPropria { Email = "nao-e-email" });

    // Assert
    Assert.Equal(["Field.Invalid;Email"], mensagens);
  }

  // Testa se o resultado aponta o membro validado
  [Fact]
  public void ValidationResult_ShouldCarryMemberName()
  {
    // Arrange
    var modelo = new SemMensagemPropria { Email = "nao-e-email" };
    var resultados = new List<ValidationResult>();

    // Act
    Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, true);

    // Assert
    Assert.Single(resultados);
    Assert.Equal(["Email"], resultados[0].MemberNames);
  }

  // Testa se o atributo não guarda estado entre validações
  [Fact]
  public void Attribute_ShouldNotKeepStateBetweenValidations()
  {
    // Arrange
    var atributo = new EmailValidationAttribute();

    // Act
    atributo.IsValid(null);
    var depoisDaFalha = atributo.ErrorMessage;

    atributo.IsValid("valido@teste.com");
    var depoisDoSucesso = atributo.ErrorMessage;

    // Assert
    Assert.Null(depoisDaFalha);
    Assert.Null(depoisDoSucesso);
  }

  // Testa se validações concorrentes não trocam mensagens entre si
  [Fact]
  public void Validation_ShouldNotMixMessages_WhenConcurrent()
  {
    // Arrange
    var atributo = new EmailValidationAttribute();
    var contexto = new ValidationContext(new object());
    var divergencias = 0;

    // Act
    Parallel.For(0, 20000, i =>
    {
      var vazio = i % 2 == 0;
      var valor = vazio ? "" : "sem-arroba";
      var esperado = vazio ? "Field.Required;Email" : "Field.Invalid;Email";

      var mensagem = atributo.GetValidationResult(valor, contexto)?.ErrorMessage;

      if (mensagem != esperado)
      {
        Interlocked.Increment(ref divergencias);
      }
    });

    // Assert
    Assert.Equal(0, divergencias);
  }

  // Testa se o nome do campo pode ser personalizado em todos os atributos
  [Theory]
  [InlineData("Field.Required;Contato")]
  public void PropertyName_ShouldBeCustomizable(string esperado)
  {
    // Arrange
    var atributo = new EmailValidationAttribute("Contato");

    // Act
    var mensagem = atributo.GetValidationResult(null, new ValidationContext(new object()))?.ErrorMessage;

    // Assert
    Assert.Equal(esperado, mensagem);
  }

  // Testa se valor composto apenas por espaços é tratado como ausente
  [Theory]
  [InlineData(" ")]
  [InlineData("   ")]
  [InlineData("\t")]
  public void Whitespace_ShouldBeTreatedAsMissing(string valor)
  {
    // Arrange
    var atributo = new EmailValidationAttribute();

    // Act
    var mensagem = atributo.GetValidationResult(valor, new ValidationContext(new object()))?.ErrorMessage;

    // Assert
    Assert.Equal("Field.Required;Email", mensagem);
  }

  // Testa se entrada longa e malformada é reprovada sem escapar exceção.
  // Até a v4.0.0 esta entrada provocava retrocesso catastrófico no padrão de email e era o que
  // exercitava a guarda de tempo limite. O padrão foi corrigido e não retrocede mais, então hoje ela
  // reprova por formato — a guarda passou a ser exercitada por Matches_ShouldRejectOnTimeout.
  [Fact]
  public void LongMalformedInput_ShouldBeRejected()
  {
    // Arrange
    var entrada = new string('a', 128) + "@" + new string('b', 128);
    var atributo = new EmailValidationAttribute();

    // Act
    var resultado = atributo.IsValid(entrada);

    // Assert
    Assert.False(resultado);
  }

  // Testa se o tipo de documento não reconhecido falha em vez de aceitar qualquer valor
  [Theory]
  [InlineData("CPFF")]
  [InlineData("xpto")]
  [InlineData("")]
  [InlineData(" ")]
  public void DocumentType_ShouldThrow_WhenNotRecognized(string tipo)
  {
    // Arrange
    var atributo = new DocumentValidationAttribute(tipo);

    // Act & Assert
    var excecao = Assert.Throws<InternalServerErrorException>(() => atributo.IsValid("52998224725"));

    // Assert
    Assert.Equal($"Attributes.DocumentTypeUnknown;{tipo.Trim()}", excecao.Message);
  }

  // Testa se o tipo None continua aceito quando pedido explicitamente
  [Theory]
  [InlineData("None")]
  [InlineData("none")]
  [InlineData("NONE")]
  public void DocumentType_ShouldAcceptNone_WhenExplicit(string tipo)
  {
    // Arrange
    var atributo = new DocumentValidationAttribute(tipo);

    // Act
    var resultado = atributo.IsValid("52998224725");

    // Assert
    Assert.True(resultado);
  }

  // Testa se o link de vídeo sem provedor habilitado falha em vez de reprovar tudo em silêncio
  [Fact]
  public void LinkVideo_ShouldThrow_WhenNoProviderEnabled()
  {
    // Arrange
    var atributo = new LinkVideoValidationAttribute("Video", youtube: false, vimeo: false, dailymotion: false);

    // Act & Assert
    var excecao = Assert.Throws<InternalServerErrorException>(
      () => atributo.IsValid("https://www.youtube.com/watch?v=b6-JNeXxN3s"));

    // Assert
    Assert.Equal("Attributes.LinkVideoNoProvider;Video", excecao.Message);
  }

  // Testa se a senha sem critérios de complexidade exige apenas o comprimento
  [Theory]
  [InlineData("abcdefgh", 8, true)]
  [InlineData("abcdefg", 8, false)]
  [InlineData("frase longa sem simbolo", 20, true)]
  public void Password_ShouldRequireOnlyLength_WhenNoCriteria(string senha, int comprimento, bool esperado)
  {
    // Arrange
    var atributo = new PasswordValidationAttribute(false, false, false, false, comprimento);

    // Act
    var resultado = atributo.IsValid(senha);

    // Assert
    Assert.Equal(esperado, resultado);
  }

  // Testa se o comprimento configurado é respeitado, e não elevado para oito
  [Theory]
  [InlineData("aB3@", 4, true)]
  [InlineData("aB3", 4, false)]
  public void Password_ShouldHonorConfiguredLength(string senha, int comprimento, bool esperado)
  {
    // Arrange
    var atributo = new PasswordValidationAttribute(length: comprimento);

    // Act
    var resultado = atributo.IsValid(senha);

    // Assert
    Assert.Equal(esperado, resultado);
  }

  // Testa se um valor que não é string é convertido antes da validação
  [Fact]
  public void NonStringValue_ShouldBeConverted()
  {
    // Arrange
    var uri = new Uri("https://www.youtube.com/watch?v=b6-JNeXxN3s");
    var atributo = new LinkVideoValidationAttribute();

    // Act
    var resultado = atributo.IsValid(uri);

    // Assert
    Assert.True(resultado);
  }

  // Testa se o estouro do tempo limite da expressão regular reprova em vez de escapar a exceção.
  // Com o padrão patológico e uma entrada que não casa, o motor tenta todas as combinações e estoura
  // os 300 ms; sem a guarda, isso viraria 500 em vez de 400 numa API.
  [Fact]
  public void Matches_ShouldRejectOnTimeout()
  {
    // Arrange
    var atributo = new ComPadraoPatologico();
    var entrada = new string('a', 40) + "!";

    // Act
    var resultado = atributo.IsValid(entrada);

    // Assert
    Assert.False(resultado);
  }
}
