using Microsoft.EntityFrameworkCore;
using Tooark.Entities;
using Tooark.Entities.EntityFrameworkCore.Extensions;
using Tooark.Entities.EntityFrameworkCore.Interfaces;
using Tooark.Enums;

namespace Tooark.Tests.Entities.EntityFrameworkCore;

// Usuário atual que os testes trocam entre uma gravação e outra
public sealed class TestCurrentUser(Guid? id) : ICurrentUser
{
  public Guid? Id { get; set; } = id;
}

// Entidade só com os campos de criação
public class Note : InitialEntity
{
  public string Text { get; set; } = string.Empty;
}

// Arquivo, que também só tem os campos de criação
public class Attachment : FileEntity;

// Entidade com criação e atualização, sem versão e sem exclusão lógica
public class Article : DetailedEntity
{
  public Article() { }

  public Article(Guid createdBy) : base(createdBy) { }

  public string Title { get; set; } = string.Empty;

  public List<Comment> Comments { get; set; } = [];
}

// Dependente com exclusão lógica de um principal que é apagado de verdade
public class Comment : SoftDeletableEntity
{
  public Guid ArticleId { get; set; }

  public string Text { get; set; } = string.Empty;
}

// Entidade com versão
public class Document : VersionedEntity
{
  public Document() { }

  public Document(Guid createdBy) : base(createdBy) { }

  public string Title { get; set; } = string.Empty;
}

// Entidade com exclusão lógica
public class Category : SoftDeletableEntity
{
  public Category() { }

  public Category(Guid createdBy) : base(createdBy) { }

  public string Name { get; set; } = string.Empty;

  public bool Active { get; set; } = true;
}

// Entidade com auditoria completa, principal dos testes de cascata
public class Order : AuditableEntity
{
  public string Number { get; set; } = string.Empty;

  public Address? Address { get; set; }

  public List<Phone> Phones { get; set; } = [];

  public List<OrderItem> Items { get; set; } = [];

  public List<OrderLog> Logs { get; set; } = [];

  public List<Shipment> Shipments { get; set; } = [];
}

// Parte do pedido que divide a tabela com ele
public class Address
{
  public string Street { get; set; } = string.Empty;

  public string City { get; set; } = string.Empty;
}

// Parte do pedido com tabela própria
public class Phone
{
  public string Number { get; set; } = string.Empty;
}

// Dependente obrigatório com exclusão lógica
public class OrderItem : SoftDeletableEntity
{
  public Guid OrderId { get; set; }

  public string Name { get; set; } = string.Empty;
}

// Dependente obrigatório sem exclusão lógica
public class OrderLog : InitialEntity
{
  public Guid OrderId { get; set; }

  public string Text { get; set; } = string.Empty;
}

// Dependente opcional sem exclusão lógica
public class Shipment : DetailedEntity
{
  public Guid? OrderId { get; set; }

  public string Code { get; set; } = string.Empty;
}

// Raiz de uma hierarquia em uma única tabela, com exclusão lógica
public abstract class Vehicle : SoftDeletableEntity
{
  public string Plate { get; set; } = string.Empty;
}

public class Car : Vehicle
{
  public int Doors { get; set; }
}

public class Truck : Vehicle
{
  public int Capacity { get; set; }
}

// Entidade que não é do Tooark
public class Setting
{
  public int Id { get; set; }

  public string Value { get; set; } = string.Empty;
}

// Contexto principal dos testes, com as convenções aplicadas por último
public class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
  public DbSet<Note> Notes => Set<Note>();

  public DbSet<Attachment> Attachments => Set<Attachment>();

  public DbSet<Article> Articles => Set<Article>();

  public DbSet<Comment> Comments => Set<Comment>();

  public DbSet<Document> Documents => Set<Document>();

  public DbSet<Category> Categories => Set<Category>();

  public DbSet<Order> Orders => Set<Order>();

  public DbSet<OrderItem> OrderItems => Set<OrderItem>();

  public DbSet<OrderLog> OrderLogs => Set<OrderLog>();

  public DbSet<Shipment> Shipments => Set<Shipment>();

  public DbSet<Vehicle> Vehicles => Set<Vehicle>();

  public DbSet<Car> Cars => Set<Car>();

  public DbSet<Truck> Trucks => Set<Truck>();

  public DbSet<Setting> Settings => Set<Setting>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    // O EFileType é uma classe, e sem o conversor o Entity Framework a trataria como outra entidade
    modelBuilder.Entity<Attachment>().Property(attachment => attachment.Type).HasConversion(type => type.ToInt(), id => (EFileType)id);

    modelBuilder.Entity<Article>().HasMany(article => article.Comments).WithOne().HasForeignKey(comment => comment.ArticleId);

    modelBuilder.Entity<Order>(order =>
    {
      order.OwnsOne(o => o.Address);

      // Chave própria de uma coluna: o SQLite não gera valor para a chave composta padrão das coleções owned
      order.OwnsMany(o => o.Phones, phone =>
      {
        phone.WithOwner().HasForeignKey("OrderId");
        phone.Property<int>("Id");
        phone.HasKey("Id");
      });

      order.HasMany(o => o.Items).WithOne().HasForeignKey(item => item.OrderId);
      order.HasMany(o => o.Logs).WithOne().HasForeignKey(log => log.OrderId);
      order.HasMany(o => o.Shipments).WithOne().HasForeignKey(shipment => shipment.OrderId);
    });

    modelBuilder.ApplyTooarkConventions();
  }
}
