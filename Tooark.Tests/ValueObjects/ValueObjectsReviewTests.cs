using System.Reflection;
using Tooark.Attributes;
using Tooark.Exceptions;
using Tooark.ValueObjects;

namespace Tooark.Tests.ValueObjects;

/// <summary>
/// Testes dos comportamentos corrigidos na revisão do Tooark.ValueObjects.
/// </summary>
public class ValueObjectsReviewTests
{
  #region Valor ausente

  // Testa se ToString nunca devolve nulo, mesmo com value object inválido
  [Fact]
  public void ToString_ShouldNeverBeNull()
  {
    // Arrange: um representante de cada família
    var invalidos = new object[]
    {
      new Cpf("111"),
      new Cnpj("111"),
      new Rg("!"),
      new Document("111", Tooark.Enums.EDocumentType.CPF),
      new Email("nao-e-email"),
      new EmailDomain("!"),
      new Url("nao-e-url"),
      new LinkVideo("nao-e-video"),
      new Name(""),
      new Title(""),
      new Description(""),
      new Keyword(""),
      new Letter("123"),
      new Numeric("abc"),
      new LetterNumeric("!@#"),
      new LanguageCode("xx"),
      new ZipCode("!"),
      new ProtocolHttp("!"),
      new ProtocolFtp("!"),
      new ProtocolWs("!"),
      new ProtocolEmailSender("!"),
      new ProtocolEmailReceiver("!"),
      new DelimitedString(""),
    };

    // Act & Assert
    foreach (var vo in invalidos)
    {
      Assert.NotNull(vo.ToString());
      Assert.Equal(string.Empty, vo.ToString());
    }
  }

  // Testa se o valor de um value object inválido é string vazia, e não nulo
  [Fact]
  public void Value_ShouldBeEmpty_WhenInvalid()
  {
    // Arrange & Act & Assert
    Assert.Equal(string.Empty, new Cpf("111").Number);
    Assert.Equal(string.Empty, new Email("nao-e-email").Value);
    Assert.Equal(string.Empty, new Url("nao-e-url").Value);
    Assert.Equal(string.Empty, new LinkVideo("nao-e-video").Link);
    Assert.Equal(string.Empty, new LanguageCode("xx").Code);
  }

  // Testa se o tipo do documento inválido assume None, e não nulo
  [Fact]
  public void DocumentType_ShouldBeNone_WhenInvalid()
  {
    // Arrange & Act
    var document = new Document("111", Tooark.Enums.EDocumentType.CPF);

    // Assert
    Assert.False(document.IsValid);
    Assert.Equal(Tooark.Enums.EDocumentType.None, document.Type);
  }

  // Testa se interpolação e concatenação de value object inválido não somem com o texto vizinho
  [Fact]
  public void TextOperations_ShouldBeSafe_WhenInvalid()
  {
    // Arrange
    var cpf = new Cpf("111");

    // Act & Assert
    Assert.Equal("cpf: ", $"cpf: {cpf}");
    Assert.Equal("cpf: ", "cpf: " + cpf);
  }

  #endregion

  #region Conversão implícita a partir de instância ausente

  // Testa se a conversão implícita de instância ausente falha com contexto
  [Fact]
  public void ImplicitConversion_ShouldThrow_WhenInstanceIsMissing()
  {
    // Arrange & Act & Assert
    var excecao = Assert.Throws<InternalServerErrorException>(() =>
    {
      Cpf? nulo = null;
      string _ = nulo!;
    });

    // Assert
    Assert.Equal("Invalid.Parameter;null", excecao.Message);
  }

  // Testa se o mesmo vale para as conversões que devolvem Guid
  [Fact]
  public void ImplicitConversionToGuid_ShouldThrow_WhenInstanceIsMissing()
  {
    // Arrange & Act & Assert
    Assert.Throws<InternalServerErrorException>(() =>
    {
      CreatedBy? nulo = null;
      Guid _ = nulo!;
    });
  }

  // Testa a guarda em TODAS as conversões de saída do pacote, inclusive nas que vierem depois
  [Fact]
  public void EveryOutgoingConversion_ShouldThrow_WhenInstanceIsMissing()
  {
    // Arrange: as conversões implícitas que recebem o próprio value object
    var conversoes = typeof(ValueObject).Assembly
      .GetExportedTypes()
      .Where(t => t.IsClass && !t.IsAbstract && typeof(ValueObject).IsAssignableFrom(t))
      .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(m => m.Name == "op_Implicit" && m.GetParameters().SingleOrDefault()?.ParameterType == t))
      .ToList();

    var semGuarda = new List<string>();

    // Act
    foreach (var conversao in conversoes)
    {
      try
      {
        conversao.Invoke(null, [null]);
        semGuarda.Add($"{conversao.DeclaringType!.Name} -> {conversao.ReturnType.Name}: nao lancou");
      }
      catch (TargetInvocationException ex) when (ex.InnerException is InternalServerErrorException interna)
      {
        // A guarda precisa dizer qual foi o problema
        if (interna.Message != "Invalid.Parameter;null")
        {
          semGuarda.Add($"{conversao.DeclaringType!.Name}: mensagem \"{interna.Message}\"");
        }
      }
      catch (TargetInvocationException ex)
      {
        semGuarda.Add($"{conversao.DeclaringType!.Name} -> {conversao.ReturnType.Name}: {ex.InnerException?.GetType().Name}");
      }
    }

    // Assert
    Assert.NotEmpty(conversoes);
    Assert.Empty(semGuarda);
  }

  #endregion

  #region Password

  // Testa se a senha não vaza pelo texto do objeto
  [Fact]
  public void Password_ShouldMaskValueInText()
  {
    // Arrange
    var valor = "Senha@123";
    var senha = new Password(valor);

    // Act & Assert
    Assert.Equal(Password.Mask, senha.ToString());
    Assert.DoesNotContain(valor, $"{senha}", StringComparison.Ordinal);
    Assert.DoesNotContain(valor, "log: " + senha, StringComparison.Ordinal);
    Assert.Equal(valor, senha.Value);
  }

  // Testa se a senha inválida não devolve a máscara, que sugeriria haver valor
  [Fact]
  public void Password_ShouldReturnEmptyText_WhenInvalid()
  {
    // Arrange & Act
    var senha = new Password("fraca");

    // Assert
    Assert.Equal(string.Empty, senha.ToString());
    Assert.Equal(string.Empty, senha.Value);
  }

  // Testa se o atributo e o value object aplicam a mesma regra
  [Theory]
  [InlineData("abcdefgh", false, false, false, false, 8)]
  [InlineData("aB3@", true, true, true, true, 4)]
  [InlineData("Senha@12", true, true, true, true, 8)]
  [InlineData("senha", true, true, true, true, 8)]
  [InlineData("frase longa sem simbolo", false, false, false, false, 20)]
  public void Password_ShouldAgreeWithTheValidationAttribute(
    string valor, bool minuscula, bool maiuscula, bool numero, bool simbolo, int comprimento)
  {
    // Arrange
    var atributo = new PasswordValidationAttribute(minuscula, maiuscula, numero, simbolo, comprimento);
    var valueObject = new Password(valor, minuscula, maiuscula, numero, simbolo, comprimento);

    // Act & Assert
    Assert.Equal(atributo.IsValid(valor), valueObject.IsValid);
  }

  #endregion

  #region DelimitedString

  // Testa se a lista interna não é exposta
  [Fact]
  public void DelimitedString_ShouldNotExposeInternalArray()
  {
    // Arrange
    var delimitada = new DelimitedString("a;b;c");

    // Act
    delimitada.ToArray()[0] = "ALTERADO";
    delimitada.Values[0] = "ALTERADO";

    // Assert
    Assert.Equal("a;b;c", delimitada.Value);
    Assert.Equal(["a", "b", "c"], delimitada.Values);
  }

  // Testa se a array informada não fica ligada ao objeto
  [Fact]
  public void DelimitedString_ShouldCopyIncomingValues()
  {
    // Arrange
    var origem = new[] { "a", "b" };

    // Act
    var delimitada = new DelimitedString(origem);
    origem[0] = "ALTERADO";

    // Assert
    Assert.Equal(["a", "b"], delimitada.Values);
  }

  // Testa se coleção ausente é reprovada em vez de estourar
  [Fact]
  public void DelimitedString_ShouldRejectMissingCollections()
  {
    // Arrange & Act
    var comArray = new DelimitedString((string[])null!);
    var comLista = new DelimitedString((List<string>)null!);
    var comTexto = new DelimitedString((string)null!);

    // Assert
    Assert.False(comArray.IsValid);
    Assert.False(comLista.IsValid);
    Assert.False(comTexto.IsValid);
    Assert.Empty(comArray.Values);
    Assert.Empty(comLista.Values);
    Assert.Empty(comTexto.Values);
  }

  #endregion
}
