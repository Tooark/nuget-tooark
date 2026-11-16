using System.ComponentModel;
using System.Globalization;

namespace Tooark.Observability.Enums;

/// <summary>
/// Conversão de texto para os tipos enumerados do pacote, usada pelo binder de configuração.
/// </summary>
/// <remarks>
/// <see cref="EProtocolOtlp"/> e <see cref="EProcessorType"/> são classes com conversão implícita a partir do
/// texto. O binder do .NET não enxerga operadores implícitos: ele pede um <see cref="TypeConverter"/> ao tipo
/// e, sem um, ignora o valor em silêncio. Era o que acontecia com <c>"Protocol": "http"</c> e com
/// <c>"ExportProcessorType": "simple"</c> no <c>appsettings.json</c>, que ficavam nos padrões.
/// </remarks>
/// <typeparam name="T">O tipo enumerado de destino.</typeparam>
/// <param name="convert">A conversão do texto, a mesma do operador implícito do tipo.</param>
internal abstract class DescriptionTypeConverter<T>(Func<string, T> convert) : TypeConverter where T : class
{
  /// <inheritdoc/>
  public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
    sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

  /// <inheritdoc/>
  public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
    value is string text ? convert(text.Trim()) : base.ConvertFrom(context, culture, value);
}

/// <summary>
/// Conversão de texto para <see cref="EProtocolOtlp"/> (<c>grpc</c> ou <c>http</c>).
/// </summary>
internal sealed class EProtocolOtlpConverter() : DescriptionTypeConverter<EProtocolOtlp>(text => text);

/// <summary>
/// Conversão de texto para <see cref="EProcessorType"/> (<c>simple</c> ou <c>batch</c>).
/// </summary>
internal sealed class EProcessorTypeConverter() : DescriptionTypeConverter<EProcessorType>(text => text);
