using System.Security.Claims;
using ContentFlow.Application.Shared.Authorization;
using ContentFlow.Application.Shared.Content;
using ContentFlow.Domain.Auth;
using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;
using FluentValidation;

namespace ContentFlow.Application.Content;
/// <summary>
/// Adds a field definition to an existing content type (issue #7, ADR-002).
/// Requires the <c>content.write</c> permission. Duplicate keys surface the domain
/// <c>content.duplicate_key</c> error via <see cref="ContentType.AddField(FieldDefinition)"/>.
/// </summary>
/// <param name="ContentTypeId">Owning content type identifier.</param>
/// <param name="Name">Human-readable field name (required, non-blank).</param>
/// <param name="Key">Machine-friendly field key, unique within the content type.</param>
/// <param name="DataType">Logical data type of the field.</param>
/// <param name="IsRequired">Whether a value is required.</param>
/// <param name="SortOrder">Display/sort order within the content type.</param>
/// <param name="DefaultValue">Optional default value as text.</param>
/// <param name="MaxLength">Optional maximum length (Text/LongText only).</param>
public sealed record AddFieldDefinitionCommand(
    Guid ContentTypeId,
    string Name,
    string Key,
    FieldDataType DataType,
    bool IsRequired = false,
    int SortOrder = 0,
    string? DefaultValue = null,
    int? MaxLength = null);
