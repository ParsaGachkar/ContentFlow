namespace ContentFlow.Domain.Content;

/// <summary>
/// Logical data type used by a <see cref="FieldDefinition"/> to describe how a field value is stored.
/// </summary>
public enum FieldDataType
{
    /// <summary>Short, single-line text.</summary>
    Text = 0,

    /// <summary>Long / multi-line text.</summary>
    LongText = 1,

    /// <summary>Numeric value.</summary>
    Number = 2,

    /// <summary>Boolean (true/false) value.</summary>
    Boolean = 3,

    /// <summary>Date and time value.</summary>
    DateTime = 4,
}
