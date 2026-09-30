using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tooark.Storage.Aws.Options;
using Tooark.Storage.Interfaces;
using Tooark.Storage.Options;

namespace Tooark.Storage.Aws.Injections;

/// <summary>
/// Classe para adicionar o storage na AWS ao container de injeção de dependência.
/// </summary>
public static partial class TooarkDependencyInjection
{
  /// <summary>
  /// Adiciona o <see cref="IStorageService"/> sobre o Amazon S3, com as opções lidas da seção <c>Storage</c>.
  /// </summary>
  /// <remarks>
  /// O cliente do S3 é montado a partir das opções e registrado como <see cref="IAmazonS3"/>. Se a aplicação já
  /// registrou um <see cref="IAmazonS3"/>, o dela é usado, e região, credenciais e endereço das opções não se
  /// aplicam. As opções são validadas aqui, no registro, para que uma configuração incompleta falhe no startup.
  /// </remarks>
  /// <param name="services">Coleção de serviços.</param>
  /// <param name="configuration">Configuração da aplicação (IConfiguration).</param>
  /// <param name="configure">Ação opcional para configurar programaticamente as opções.</param>
  /// <returns>A coleção de serviços com o storage adicionado.</returns>
  /// <exception cref="Tooark.Exceptions.InternalServerErrorException">Quando as opções são inválidas.</exception>
  public static IServiceCollection AddTooarkStorageAws(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<AwsStorageOptions>? configure = null
  )
  {
    // Carrega da configuração e aplica os overrides programáticos por cima
    void Apply(AwsStorageOptions options)
    {
      configuration.GetSection(StorageOptions.Section).Bind(options);
      configure?.Invoke(options);
    }

    // Valida no startup, e não na primeira operação
    var options = new AwsStorageOptions();
    Apply(options);
    options.Validate();

    services.AddOptions<AwsStorageOptions>().Configure(Apply);

    // O cliente é thread-safe e caro de criar: uma instância atende a aplicação
    services.TryAddSingleton<IAmazonS3>(_ => CreateClient(options));
    services.TryAddSingleton<IStorageService, AwsStorageService>();

    return services;
  }

  /// <summary>
  /// Monta o cliente do S3 a partir das opções.
  /// </summary>
  /// <param name="options">As opções já validadas.</param>
  /// <returns>O cliente do S3.</returns>
  internal static AmazonS3Client CreateClient(AwsStorageOptions options)
  {
    var config = new AmazonS3Config { ForcePathStyle = options.ForcePathStyle };
    var region = options.Region?.Trim();

    if (string.IsNullOrWhiteSpace(options.ServiceUrl))
    {
      if (!string.IsNullOrEmpty(region))
      {
        config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
      }
    }
    else
    {
      // Serviço compatível: o endereço substitui o da AWS. O SDK descarta a região ao receber o endereço, então a
      // região informada vale só para a assinatura, que é o que o serviço confere
      config.ServiceURL = options.ServiceUrl.Trim();

      if (!string.IsNullOrEmpty(region))
      {
        config.AuthenticationRegion = region;
      }
    }

    // Sem chaves nas opções, a cadeia padrão da AWS resolve as credenciais
    if (string.IsNullOrWhiteSpace(options.AccessKey))
    {
      return new AmazonS3Client(config);
    }

    AWSCredentials credentials = string.IsNullOrWhiteSpace(options.SessionToken)
      ? new BasicAWSCredentials(options.AccessKey, options.SecretKey)
      : new SessionAWSCredentials(options.AccessKey, options.SecretKey, options.SessionToken);

    return new AmazonS3Client(credentials, config);
  }
}
