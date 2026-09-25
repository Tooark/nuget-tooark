using System.Text;
using Tooark.Extensions;
using Tooark.Tests.Moq.Entities.Category;

namespace Tooark.Tests.Extensions;

public class EnumerableExtensionsTests
{
  // Criando uma lista de categorias para testar os métodos de extensão
  private readonly List<Category> _categories;

  // Construtor para inicializar a lista de categorias
  public EnumerableExtensionsTests()
  {
    _categories =
    [
      TestDataGenerator.CreateCategory(1),
      TestDataGenerator.CreateCategory(2),
      TestDataGenerator.CreateCategory(3),
      TestDataGenerator.CreateCategory(4),
      TestDataGenerator.CreateCategory(5)
    ];
  }

  // Classe para gerar dados de teste
  public static class TestDataGenerator
  {
    public static Category CreateCategory(int sequence)
    {
      bool includeOptional = sequence % 2 == 1;
      int valueChar = sequence * (includeOptional ? -1 : 1) + 70;
      string letter = new(Encoding.Default.GetChars([(byte)valueChar]));
      DateTime date = new(2025, 1, 1);

      var category = new Category
      {
        Id = new Guid("00000000-0000-0000-0000-00000000000" + sequence),
        Type = "Type " + letter,
        Description = includeOptional ? "Description " + letter : null,
        CreatedAt = date.AddDays(sequence),
        SubCategory = includeOptional ? CreateSubCategory(sequence) : null,
        ListSubCategory = includeOptional ? new List<ListSubCategory> { CreateListSubCategory(sequence) } : null!,

        Object = includeOptional,
        Boolean = includeOptional,
        Char = letter[0],
        SByte = (sbyte)sequence,
        Byte = (byte)sequence,
        Int_16 = (short)sequence,
        UInt_16 = (ushort)sequence,
        Int_32 = sequence,
        UInt_32 = (uint)sequence,
        Int_64 = sequence,
        UInt_64 = (ulong)sequence,
        Float = sequence,
        Double = sequence,
        Decimal = sequence,
      };

      return category;
    }

    public static SubCategory CreateSubCategory(int sequence)
    {
      bool includeOptional = sequence % 2 == 1;
      int valueChar = sequence * (includeOptional ? -1 : 1) + 72;
      string letter = new(Encoding.Default.GetChars([(byte)valueChar]));

      var subCategory = new SubCategory
      {
        Id = sequence * 100 + sequence,
        Name = "Name " + letter,
        Type = includeOptional ? sequence : null,
        DeepCategory = includeOptional ? CreateDeepCategory(sequence) : null,
        CreatedAt = DateTime.UtcNow.AddDays(sequence),
      };

      return subCategory;
    }

    public static DeepCategory CreateDeepCategory(int sequence)
    {
      bool includeOptional = sequence % 2 == 1;
      int valueChar = sequence * (includeOptional ? -1 : 1) + 74;
      string letter = new(Encoding.Default.GetChars([(byte)valueChar]));

      var deepCategory = new DeepCategory
      {
        Id = sequence * 10000 + sequence,
        Name = "Name " + letter,
        CreatedAt = DateTime.UtcNow.AddDays(sequence),
      };

      return deepCategory;
    }

    public static ListSubCategory CreateListSubCategory(int sequence)
    {
      bool includeOptional = sequence % 2 == 1;
      int valueChar = sequence * (includeOptional ? -1 : 1) + 76;
      string letter = new(Encoding.Default.GetChars([(byte)valueChar]));

      var listSubCategory = new ListSubCategory
      {
        Id = sequence * 1000 + sequence,
        Name = includeOptional ? "Name " + letter : null,
        Type = includeOptional ? 1 : 0,
        DeepCategory = includeOptional ? CreateDeepCategory(sequence) : null,
        CreatedAt = DateTime.UtcNow.AddDays(sequence)
      };

      return listSubCategory;
    }
  }

  // Teste para ordenar por Guid
  [Fact]
  public void OrderByProperty_SortsByGuidProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Id")
      .Select(x => x.Id)
      .ToList();

    // Assert
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 1), result[0]);
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 2), result[1]);
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 3), result[2]);
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 4), result[3]);
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 5), result[4]);
  }

  // Teste para ordenar por Guid de forma descendente
  [Fact]
  public void OrderByPropertyDescending_SortsByGuidProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByPropertyDescending("Id")
      .Select(x => x.Id)
      .ToList();

    // Assert
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 5), result[0]);
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 4), result[1]);
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 3), result[2]);
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 2), result[3]);
    Assert.Equal(new Guid("00000000-0000-0000-0000-00000000000" + 1), result[4]);
  }

  // Teste para ordenar por string
  [Fact]
  public void OrderByProperty_SortsByStringProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Type")
      .Select(x => x.Type)
      .ToList();

    // Assert
    Assert.Equal("Type A", result[0]);
    Assert.Equal("Type C", result[1]);
    Assert.Equal("Type E", result[2]);
    Assert.Equal("Type H", result[3]);
    Assert.Equal("Type J", result[4]);
  }

  // Teste para ordenar por data
  [Fact]
  public void OrderByProperty_SortsByDateTimeProperty()
  {
    // Arrange
    DateTime date = new(2025, 1, 1);

    // Act
    var result = _categories
      .OrderByProperty("CreatedAt")
      .Select(x => x.CreatedAt)
      .ToList();

    // Assert
    Assert.Equal(date.AddDays(1), result[0]);
    Assert.Equal(date.AddDays(2), result[1]);
    Assert.Equal(date.AddDays(3), result[2]);
    Assert.Equal(date.AddDays(4), result[3]);
    Assert.Equal(date.AddDays(5), result[4]);
  }

  // Teste para ordenar por uma propriedade de uma classe
  [Fact]
  public void OrderByProperty_SortsByClassProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("SubCategory.Name")
      .Select(x => x.SubCategory?.Name)
      .ToList();

    // Assert
    Assert.Null(result[0]);
    Assert.Null(result[1]);
    Assert.Equal("Name C", result[2]);
    Assert.Equal("Name E", result[3]);
    Assert.Equal("Name G", result[4]);
  }

  // Teste para ordenar por uma propriedade de uma classe dentro de outra classe
  [Fact]
  public void OrderByProperty_SortsByClassDeepProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("SubCategory.DeepCategory.Name")
      .Select(x => x.SubCategory?.DeepCategory?.Name)
      .ToList();

    // Assert
    Assert.Null(result[0]);
    Assert.Null(result[1]);
    Assert.Equal("Name E", result[2]);
    Assert.Equal("Name G", result[3]);
    Assert.Equal("Name I", result[4]);
  }

  // Teste para ordenar por uma propriedade dentro de uma lista
  [Fact]
  public void OrderByProperty_SortsByListProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("ListSubCategory.Name")
      .Select(x => x.ListSubCategory?.FirstOrDefault()?.Name)
      .ToList();

    // Assert
    Assert.Null(result[0]);
    Assert.Null(result[1]);
    Assert.Equal("Name G", result[2]);
    Assert.Equal("Name I", result[3]);
    Assert.Equal("Name K", result[4]);
  }

  // Teste para ordenar por uma propriedade de uma classe dentro de uma lista
  [Fact]
  public void OrderByProperty_SortsByListDeepProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("ListSubCategory.DeepCategory.Name")
      .Select(x => x.ListSubCategory?.FirstOrDefault()?.DeepCategory?.Name)
      .ToList();

    // Assert
    Assert.Null(result[0]);
    Assert.Null(result[1]);
    Assert.Equal("Name E", result[2]);
    Assert.Equal("Name G", result[3]);
    Assert.Equal("Name I", result[4]);
  }

  // Teste para ordenar por uma propriedade passando null
  [Fact]
  public void OrderByProperty_SortsByNullProperty()
  {
    // Arrange
    var categories = new List<Category> { new() { Id = new Guid("00000000-0000-0000-0000-000000000001") } };

    // Act
    var result = _categories
      .OrderByProperty(null!)
      .Select(x => x.Id)
      .ToList();

    // Assert
    Assert.Equal(categories.First().Id, result[0]);
  }

  // Teste para ordenar por uma propriedade passando vazio
  [Fact]
  public void OrderByProperty_SortsByEmptyProperty()
  {
    // Arrange
    var categories = new List<Category> { new() { Id = new Guid("00000000-0000-0000-0000-000000000001") } };

    // Act
    var result = _categories
      .OrderByProperty(string.Empty)
      .Select(x => x.Id)
      .ToList();

    // Assert
    Assert.Equal(categories.First().Id, result[0]);
  }

  // Teste para ordenar por uma propriedade passando propriedade inexistente
  [Fact]
  public void OrderByProperty_SortsByUnknownProperty()
  {
    // Arrange
    var categories = new List<Category> { new() { Id = new Guid("00000000-0000-0000-0000-000000000001") } };

    // Act
    var result = _categories
      .OrderByProperty("Unknown")
      .Select(x => x.Id)
      .ToList();

    // Assert
    Assert.Equal(categories.First().Id, result[0]);
  }

  // Teste para ordenar por booleano
  [Fact]
  public void OrderByProperty_SortsByBoolProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Boolean")
      .Select(x => x.Boolean)
      .ToList();

    // Assert
    Assert.False(result[0]);
    Assert.False(result[1]);
    Assert.True(result[2]);
    Assert.True(result[3]);
    Assert.True(result[4]);
  }

  // Teste para ordenar por caractere
  [Fact]
  public void OrderByProperty_SortsByCharProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Char")
      .Select(x => x.Char)
      .ToList();

    // Assert
    Assert.Equal(65, result[0]);
    Assert.Equal(67, result[1]);
    Assert.Equal(69, result[2]);
    Assert.Equal(72, result[3]);
    Assert.Equal(74, result[4]);
  }

  // Teste para ordenar por Sbyte
  [Fact]
  public void OrderByProperty_SortsBySByteProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("SByte")
      .Select(x => x.SByte)
      .ToList();

    // Assert
    Assert.Equal(1, result[0]);
    Assert.Equal(2, result[1]);
    Assert.Equal(3, result[2]);
    Assert.Equal(4, result[3]);
    Assert.Equal(5, result[4]);
  }

  // Teste para ordenar por byte
  [Fact]
  public void OrderByProperty_SortsByByteProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Byte")
      .Select(x => x.Byte)
      .ToList();

    // Assert
    Assert.Equal(1, result[0]);
    Assert.Equal(2, result[1]);
    Assert.Equal(3, result[2]);
    Assert.Equal(4, result[3]);
    Assert.Equal(5, result[4]);
  }

  // Teste para ordenar por inteiro de 16 bits
  [Fact]
  public void OrderByProperty_SortsByInt16Property()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Int_16")
      .Select(x => x.Int_16)
      .ToList();

    // Assert
    Assert.Equal(1, result[0]);
    Assert.Equal(2, result[1]);
    Assert.Equal(3, result[2]);
    Assert.Equal(4, result[3]);
    Assert.Equal(5, result[4]);
  }

  // Teste para ordenar por inteiro sem sinal de 16 bits
  [Fact]
  public void OrderByProperty_SortsByUInt16Property()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("UInt_16")
      .Select(x => x.UInt_16)
      .ToList();

    // Assert
    Assert.Equal((ushort)1, result[0]);
    Assert.Equal((ushort)2, result[1]);
    Assert.Equal((ushort)3, result[2]);
    Assert.Equal((ushort)4, result[3]);
    Assert.Equal((ushort)5, result[4]);
  }

  // Teste para ordenar por inteiro de 32 bits
  [Fact]
  public void OrderByProperty_SortsByInt32Property()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Int_32")
      .Select(x => x.Int_32)
      .ToList();

    // Assert
    Assert.Equal(1, result[0]);
    Assert.Equal(2, result[1]);
    Assert.Equal(3, result[2]);
    Assert.Equal(4, result[3]);
    Assert.Equal(5, result[4]);
  }

  // Teste para ordenar por inteiro sem sinal de 32 bits
  [Fact]
  public void OrderByProperty_SortsByUInt32Property()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("UInt_32")
      .Select(x => x.UInt_32)
      .ToList();

    // Assert
    Assert.Equal((uint)1, result[0]);
    Assert.Equal((uint)2, result[1]);
    Assert.Equal((uint)3, result[2]);
    Assert.Equal((uint)4, result[3]);
    Assert.Equal((uint)5, result[4]);
  }

  // Teste para ordenar por inteiro de 64 bits | long
  [Fact]
  public void OrderByProperty_SortsByInt64Property()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Int_64")
      .Select(x => x.Int_64)
      .ToList();

    // Assert
    Assert.Equal(1, result[0]);
    Assert.Equal(2, result[1]);
    Assert.Equal(3, result[2]);
    Assert.Equal(4, result[3]);
    Assert.Equal(5, result[4]);
  }

  // Teste para ordenar por inteiro sem sinal de 64 bits | long
  [Fact]
  public void OrderByProperty_SortsByUInt64Property()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("UInt_64")
      .Select(x => x.UInt_64)
      .ToList();

    // Assert
    Assert.Equal((ulong)1, result[0]);
    Assert.Equal((ulong)2, result[1]);
    Assert.Equal((ulong)3, result[2]);
    Assert.Equal((ulong)4, result[3]);
    Assert.Equal((ulong)5, result[4]);
  }

  // Teste para ordenar por float
  [Fact]
  public void OrderByProperty_SortsByFloatProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Float")
      .Select(x => x.Float)
      .ToList();

    // Assert
    Assert.Equal(1, result[0]);
    Assert.Equal(2, result[1]);
    Assert.Equal(3, result[2]);
    Assert.Equal(4, result[3]);
    Assert.Equal(5, result[4]);
  }

  // Teste para ordenar por double
  [Fact]
  public void OrderByProperty_SortsByDoubleProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Double")
      .Select(x => x.Double)
      .ToList();

    // Assert
    Assert.Equal(1, result[0]);
    Assert.Equal(2, result[1]);
    Assert.Equal(3, result[2]);
    Assert.Equal(4, result[3]);
    Assert.Equal(5, result[4]);
  }

  // Teste para ordenar por decimal
  [Fact]
  public void OrderByProperty_SortsByDecimalProperty()
  {
    // Arrange & Act
    var result = _categories
      .OrderByProperty("Decimal")
      .Select(x => x.Decimal)
      .ToList();

    // Assert
    Assert.Equal(1, result[0]);
    Assert.Equal(2, result[1]);
    Assert.Equal(3, result[2]);
    Assert.Equal(4, result[3]);
    Assert.Equal(5, result[4]);
  }
}
