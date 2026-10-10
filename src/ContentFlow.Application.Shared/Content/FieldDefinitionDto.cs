using ContentFlow.Domain.Shared;

namespace ContentFlow.Application.Shared.Content;
/// <summary>
/// Read model for a field definition.
/// </summary>
/// <param name="Id">The field definition identifier.</param>
/// <param name="ContentTypeId">The owning content type identifier.</param>
/// <param name="Name">The human-readable field name.</param>
/// <param name="Key">The machine-friendly field key, unique within the content type.</param>
/// <param name="DataType">The logical data type.</param>
/// <param name="IsRequired">Whether a value is required.</param>
/// <param name="SortOrder">The display/sort order within the content type.</param>
/// <param name="DefaultValue">The optional default value (as text).</param>
/// <param name="MaxLength">The optional maximum length for text-based fields.</param>
public sealed record FieldDefinitionDto(
    Guid Id,
    Guid ContentTypeId,
    string Name,
    string Key,
    FieldDataType DataType,
    bool IsRequired,
    int SortOrder,
    string? DefaultValue,
    int? MaxLength);
