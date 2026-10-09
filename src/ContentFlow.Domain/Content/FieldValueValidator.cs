using System.Globalization;
using ContentFlow.Domain.Shared;

namespace ContentFlow.Domain.Content;

/// <summary>
/// The coerced, type-safe outcome of parsing a raw field value.
/// Exactly one typed column is set for a concrete value; all columns are
/// <see langword="null"/> when the raw input is blank (no value).
/// </summary>
/// <param name="DataType">The field data type the value was coerced to.</param>
/// <param name="TextValue">Text value (Text/LongText fields).</param>
/// <param name="NumberValue">Numeric value (Number fields).</param>
/// <param name="BooleanValue">Boolean value (Boolean fields).</param>
/// <param name="DateTimeValue">Date/time value (DateTime fields).</param>
public sealed record ParsedFieldValue(
    FieldDataType DataType,
    string? TextValue,
    decimal? NumberValue,
    bool? BooleanValue,
    DateTimeOffset? DateTimeValue)
{
    /// <summary>Creates an empty (no value) result for <paramref name="dataType"/>.</summary>
    /// <param name="dataType">The field data type.</param>
    /// <returns>An empty parsed value.</returns>
    public static ParsedFieldValue Empty(FieldDataType dataType) =>
        new(dataType, null, null, null, null);
}

/// <summary>
/// Domain service that coerces raw field input to typed values (relational-first, ADR-002).
/// Pure function: never mutates entities. Blank input coerces to <see cref="ParsedFieldValue.Empty(FieldDataType)"/>
/// (no value); required-field enforcement stays with the caller
/// (<see cref="ContentItem.SetFieldValue(FieldDefinition, string?)"/>).
/// </summary>
public static class FieldValueValidator
{
    /// <summary>
    /// Coerces <paramref name="rawValue"/> to the typed columns for <paramref name="dataType"/>.
    /// </summary>
    /// <param name="dataType">The field's data type.</param>
    /// <param name="rawValue">The raw input (may be <see langword="null"/>).</param>
    /// <param name="maxLength">Maximum length (only meaningful for Text/LongText).</param>
    /// <param name="fieldKey">Field key used in error messages.</param>
    /// <returns>A successful result with the coerced value, or a failure with a stable <see cref="ContentErrors"/> code.</returns>
    public static Result<ParsedFieldValue> Coerce(
        FieldDataType dataType,
        string? rawValue,
        int? maxLength = null,
        string? fieldKey = null)
    {
        var key = string.IsNullOrWhiteSpace(fieldKey) ? "field" : fieldKey;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Result<ParsedFieldValue>.Success(ParsedFieldValue.Empty(dataType));
        }

        switch (dataType)
        {
            case FieldDataType.Text:
            case FieldDataType.LongText:
                if (maxLength.HasValue && rawValue.Length > maxLength.Value)
                {
                    return Result<ParsedFieldValue>.Fail(ContentErrors.MaxLengthError(key, maxLength.Value));
                }

                return Result<ParsedFieldValue>.Success(new ParsedFieldValue(dataType, rawValue, null, null, null));

            case FieldDataType.Number:
                if (decimal.TryParse(rawValue.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                {
                    return Result<ParsedFieldValue>.Success(new ParsedFieldValue(dataType, null, number, null, null));
                }

                return Result<ParsedFieldValue>.Fail(ContentErrors.TypeMismatchError(key, dataType, rawValue));

            case FieldDataType.Boolean:
                var normalized = rawValue.Trim().ToLowerInvariant();
                if (normalized is "true" or "1")
                {
                    return Result<ParsedFieldValue>.Success(new ParsedFieldValue(dataType, null, null, true, null));
                }

                if (normalized is "false" or "0")
                {
                    return Result<ParsedFieldValue>.Success(new ParsedFieldValue(dataType, null, null, false, null));
                }

                return Result<ParsedFieldValue>.Fail(ContentErrors.TypeMismatchError(key, dataType, rawValue));

            case FieldDataType.DateTime:
                if (DateTimeOffset.TryParse(
                        rawValue.Trim(),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var dateTime))
                {
                    return Result<ParsedFieldValue>.Success(new ParsedFieldValue(dataType, null, null, null, dateTime));
                }

                return Result<ParsedFieldValue>.Fail(ContentErrors.TypeMismatchError(key, dataType, rawValue));

            default:
                return Result<ParsedFieldValue>.Fail(
                    ContentErrors.TypeMismatchError($"Unknown field data type '{dataType}' for field '{key}'."));
        }
    }
}
