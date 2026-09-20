namespace Tooark.Tests.Moq.Entities.Category;

public class Category
{
  public Guid Id { get; set; }
  public string Type { get; set; } = null!;
  public string? Description { get; set; }
  public DateTime CreatedAt { get; set; }

  public object Object { get; set; } = null!;
  public bool Boolean { get; set; } = false;
  public char Char { get; set; } = 'T';
  public sbyte SByte { get; set; } = 0;
  public byte Byte { get; set; } = 0;
  public short Int_16 { get; set; } = 0;
  public ushort UInt_16 { get; set; } = 0;
  public int Int_32 { get; set; } = 0;
  public uint UInt_32 { get; set; } = 0;
  public long Int_64 { get; set; } = 0;
  public ulong UInt_64 { get; set; } = 0;
  public float Float { get; set; } = 0;
  public double Double { get; set; } = 0;
  public decimal Decimal { get; set; } = 0;


  public string? Field;

  public SubCategory? SubCategory { get; set; } = null!;
  public ICollection<ListSubCategory> ListSubCategory { get; set; } = null!;
}
