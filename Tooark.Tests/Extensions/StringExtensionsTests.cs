using Tooark.Extensions;

namespace Tooark.Tests.Extensions;

public class StringExtensionsTests
{
  // Teste para verificar se a função ToNormalize normaliza uma string
  [Theory]
  [InlineData(null, "")]
  [InlineData("", "")]
  [InlineData(" ", "")]
  [InlineData("  ", "")]
  [InlineData("Hello World", "HELLOWORLD")]
  [InlineData("Ç Ñ Ÿ Ý Â Ä À Å Á Ã", "CNYYAAAAAA")]
  [InlineData("É Ê Ë È & Ï Î Ì Í Ô Ö Ò Ó Õ Ü Û Ù Ú", "EEEEANDIIIIOOOOOUUUU")]
  [InlineData("123 ABC", "123ABC")]
  [InlineData("!@#$%^&*()_+", "ATDOLLARAND")]
  [InlineData("çñÿâäàåáãéêëèïîìíôöòóðõüûùú", "CNYAAAAAAEEEEIIIIOOOODOUUUU")]
  [InlineData("123 abc ÇÑÝÂÄÀÅÁÃÉÊËÈÏÎÌÍÔÖÒÓÐÕÜÛÙÚ", "123ABCCNYAAAAAAEEEEIIIIOOOODOUUUU")]
  [InlineData("Olá, eu sou o Tooark!", "OLAEUSOUOTOOARK")]
  [InlineData("¿Qué tal? Me llamo Tooark.", "QUETALMELLAMOTOOARK")]
  [InlineData("Wie geht's? Ich heiße Tooark.", "WIEGEHTSICHHEISSETOOARK")]
  [InlineData("こんにちは、私はTooarkです。", "TOOARK")]
  [InlineData("你好，我叫Tooark。", "TOOARK")]
  [InlineData("안녕하세요, 저는 Tooark입니다.", "TOOARK")]
  [InlineData("Привет, я Tooark.", "TOOARK")]
  [InlineData("Cześć, nazywam się Tooark.", "CZESCNAZYWAMSIETOOARK")]
  [InlineData("Hello, I'm Tooark.", "HELLOIMTOOARK")]
  [InlineData("Γεια σας, είμαι ο Tooark.", "TOOARK")]
  [InlineData("Bună, eu sunt Tooark.", "BUNAEUSUNTTOOARK")]
  [InlineData("Sveiki, aš esu Tooark.", "SVEIKIASESUTOOARK")]
  [InlineData("Здравейте, аз съм Tooark.", "TOOARK")]
  [InlineData("Sawubona, ngiyi-Tooark.", "SAWUBONANGIYITOOARK")]
  [InlineData("Shwmae, fi yw Tooark.", "SHWMAEFIYWTOOARK")]
  [InlineData("สวัสดีครับ ผมชื่อ Tooark", "TOOARK")]
  [InlineData("नमस्ते, मेरा नाम Tooark है।", "TOOARK")]
  [InlineData("नमस्कार, माझे नाव Tooark आहे.", "TOOARK")]
  [InlineData("નમસ્તે, મારું નામ Tooark છે.", "TOOARK")]
  [InlineData("ਸਤਿ ਸ੍ਰੀ ਅਕਾਲ, ਮੇਰਾ ਨਾਮ Tooark ਹੈ.", "TOOARK")]
  [InlineData("வணக்கம், என் பெயர் Tooark.", "TOOARK")]
  [InlineData("నమస్కారం, నా పేరు Tooark.", "TOOARK")]
  [InlineData("ನಮಸ್ಕಾರ, ನನ್ನ ಹೆಸರು Tooark.", "TOOARK")]
  [InlineData("നമസ്കാരം, എന്റെ പേര് Tooark.", "TOOARK")]
  [InlineData("হ্যালো, আমার নাম Tooark.", "TOOARK")]
  [InlineData("sdùVWMäwúý", "SDUVWMAWUY")]
  [InlineData("ûëlbsSô1óv", "UELBSSO1OV")]
  [InlineData("33dòïêgDQR", "33DOIEGDQR")]
  [InlineData("0óaïJnYëMí", "0OAIJNYEMI")]
  [InlineData(" Nð 139 Qli ", "ND139QLI")]
  [InlineData("ÿm0oeTEmbã", "YM0OETEMBA")]
  [InlineData("äCjqåqjVZq", "ACJQAQJVZQ")]
  [InlineData("Tf4îJåzàge", "TF4IJAZAGE")]
  [InlineData("ù1nCÿ0ú7éW", "U1NCY0U7EW")]
  [InlineData("òî7gåP0siH", "OI7GAP0SIH")]
  [InlineData("SB8làbhûH1", "SB8LABHUH1")]
  [InlineData(" âdçd51KU8 ", "ADCD51KU8")]
  [InlineData("&@$€£¥©®™ºªßæœøØđÐðłŁþ", "ANDATDOLLAREUROPOUNDYENCRTMOASSAEOEOODDDLLTH")]
  public void ToNormalize_ShouldNormalizeString(string? input, string expected)
  {
    // Arrange & Act
    var result = input!.ToNormalize();

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para verificar se a função SnakeToPascalCase converte SnakeCase para PascalCase
  [Theory]
  [InlineData(null, null)]
  [InlineData("_", "")]
  [InlineData("_a", "A")]
  [InlineData("a_", "A")]
  [InlineData("snake_", "Snake")]
  [InlineData("_snake", "Snake")]
  [InlineData("_snake_", "Snake")]
  [InlineData("snake_case", "SnakeCase")]
  [InlineData("snake_s_case", "SnakeSCase")]
  [InlineData("SNAKE_CASE", "SnakeCase")]
  [InlineData("snake__case", "SnakeCase")]
  [InlineData("_snake__case", "SnakeCase")]
  [InlineData("snake__case_", "SnakeCase")]
  [InlineData("_snake__case_", "SnakeCase")]
  [InlineData("another_example_snake", "AnotherExampleSnake")]
  [InlineData("another__example_snake", "AnotherExampleSnake")]
  [InlineData("another__example__snake", "AnotherExampleSnake")]
  public void FromSnakeToPascalCase_ShouldConvertToPascalCase(string? input, string? expected)
  {
    // Act
    var result = input!.FromSnakeToPascalCase();

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para verificar se a função SnakeToCamelCase converte SnakeCase para CamelCase
  [Theory]
  [InlineData(null, null)]
  [InlineData("_", "")]
  [InlineData("_a", "a")]
  [InlineData("a_", "a")]
  [InlineData("snake_", "snake")]
  [InlineData("_snake", "snake")]
  [InlineData("_snake_", "snake")]
  [InlineData("snake_case", "snakeCase")]
  [InlineData("snake_s_case", "snakeSCase")]
  [InlineData("SNAKE_CASE", "snakeCase")]
  [InlineData("snake__case", "snakeCase")]
  [InlineData("_snake__case", "snakeCase")]
  [InlineData("snake__case_", "snakeCase")]
  [InlineData("_snake__case_", "snakeCase")]
  [InlineData("another_example_snake", "anotherExampleSnake")]
  [InlineData("another__example_snake", "anotherExampleSnake")]
  [InlineData("another__example__snake", "anotherExampleSnake")]
  public void FromSnakeToCamelCase_ShouldConvertToCamelCase(string? input, string? expected)
  {
    // Act
    var result = input!.FromSnakeToCamelCase();

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para verificar se a função SnakeToKebabCase converte SnakeCase para KebabCase
  [Theory]
  [InlineData(null, null)]
  [InlineData("_", "")]
  [InlineData("_a", "a")]
  [InlineData("a_", "a")]
  [InlineData("snake_", "snake")]
  [InlineData("_snake", "snake")]
  [InlineData("_snake_", "snake")]
  [InlineData("snake_case", "snake-case")]
  [InlineData("snake_s_case", "snake-s-case")]
  [InlineData("SNAKE_CASE", "snake-case")]
  [InlineData("snake__case", "snake-case")]
  [InlineData("_snake__case", "snake-case")]
  [InlineData("snake__case_", "snake-case")]
  [InlineData("_snake__case_", "snake-case")]
  [InlineData("another_example_snake", "another-example-snake")]
  [InlineData("another__example_snake", "another-example-snake")]
  [InlineData("another__example__snake", "another-example-snake")]
  public void FromSnakeToKebabCase_ShouldConvertToKebabCase(string? input, string? expected)
  {
    // Act
    var result = input!.FromSnakeToKebabCase();

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para verificar se a função PascalToSnakeCase converte PascalCase para SnakeCase
  [Theory]
  [InlineData(null, null)]
  [InlineData("", "")]
  [InlineData("A", "a")]
  [InlineData("Snake", "snake")]
  [InlineData("SNAKE", "s_n_a_k_e")]
  [InlineData("SnakeCase", "snake_case")]
  [InlineData("SnakeSCase", "snake_s_case")]
  [InlineData("AnotherExampleSnake", "another_example_snake")]
  public void FromPascalToSnakeCase_ShouldConvertToSnakeCase(string? input, string? expected)
  {
    // Act
    var result = input!.FromPascalToSnakeCase();

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para verificar se a função CamelToSnakeCase converte CamelCase para SnakeCase
  [Theory]
  [InlineData(null, null)]
  [InlineData("", "")]
  [InlineData("snake", "snake")]
  [InlineData("SNAKE", "s_n_a_k_e")]
  [InlineData("snakeCase", "snake_case")]
  [InlineData("snakeSCase", "snake_s_case")]
  [InlineData("anotherExampleSnake", "another_example_snake")]
  public void FromCamelToSnakeCase_ShouldConvertToSnakeCase(string? input, string? expected)
  {
    // Act
    var result = input!.FromCamelToSnakeCase();

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para verificar se a função KebabToSnakeCase converte KebabCase para SnakeCase
  [Theory]
  [InlineData(null, null)]
  [InlineData("", "")]
  [InlineData("snake-", "snake")]
  [InlineData("-s-n-a-k-e-", "s_n_a_k_e")]
  [InlineData("-snake", "snake")]
  [InlineData("-snake-", "snake")]
  [InlineData("snake-case", "snake_case")]
  [InlineData("snake-s-case", "snake_s_case")]
  [InlineData("snake--case", "snake_case")]
  [InlineData("-snake--case", "snake_case")]
  [InlineData("snake--case-", "snake_case")]
  [InlineData("-snake--case-", "snake_case")]
  [InlineData("another-example-snake", "another_example_snake")]
  [InlineData("another--example-snake", "another_example_snake")]
  [InlineData("another--example--snake", "another_example_snake")]
  public void FromKebabToSnakeCase_ShouldConvertToSnakeCase(string? input, string? expected)
  {
    // Act
    var result = input!.FromKebabToSnakeCase();

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para verificar se a função ToBase64 converte uma string para Base64
  [Fact]
  public void ToBase64_ShouldConvertStringToBase64()
  {
    // Arrange
    var input = "Hello, Tooark!";
    var expected = "SGVsbG8sIFRvb2FyayE=";

    // Act
    var result = input.ToBase64();

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para verificar se a função FromBase64 converte uma string Base64 para string normal
  [Fact]
  public void FromBase64_ShouldConvertBase64ToString()
  {
    // Arrange
    var input = "SGVsbG8sIFRvb2FyayE=";
    var expected = "Hello, Tooark!";

    // Act
    var result = input.FromBase64();

    // Assert
    Assert.Equal(expected, result);
  }

  // Teste para verificar se a função ToSlug converte uma string para slug
  [Theory]
  [InlineData("Olá, eu sou o Tooark!", "ola-eu-sou-o-tooark")]
  [InlineData("Olá, mundo!!!", "ola-mundo")]
  [InlineData("   Olá   mundo   ", "ola-mundo")]
  [InlineData("-slug-", "slug")]
  [InlineData("slug---com---hifens", "slug-com-hifens")]
  [InlineData("Snake_Case_example", "snake-case-example")]
  [InlineData("Cześć, nazywam się Tooark.", "czesc-nazywam-sie-tooark")]
  [InlineData("&@$€£¥©®™ºªßæœøØđÐðłŁþ", "andatdollareuropoundyencrtmoassaeoeoodddllth")]
  [InlineData("& &", "and-and")]
  [InlineData("& Ola &", "and-ola-and")]
  [InlineData("Rock & Roll", "rock-and-roll")]
  [InlineData("C# & C++", "c-and-c")]
  [InlineData("", "")]
  [InlineData(null, null)]
  public void ToSlug_ShouldConvertStringToSlug(string? input, string? expected)
  {
    // Arrange & Act
    var result = input?.ToSlug();

    // Assert
    Assert.Equal(expected, result);
  }
}
