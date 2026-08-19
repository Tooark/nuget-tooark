namespace Tooark.Tests.Moq.Entities.Language;

public class MLanguage
{
  public string LanguageCode { get; set; } = null!;
  public string Name { get; set; } = null!;
  public string Title { get; set; } = null!;
  public string Description { get; set; } = null!;
  public string Keywords { get; set; } = null!;
  public string Other { get; set; } = null!;
}

public class MLanguageOnlyLanguageCode
{
  public string LanguageCode { get; set; } = null!;
}

public class MLanguageOnlyName
{
  public string Name { get; set; } = null!;
}

public class MLanguageNonStringProperty
{
  public string LanguageCode { get; set; } = null!;
  public int Name { get; set; }
  public string? Title { get; set; }
}
