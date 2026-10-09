// Field-value coercion tests (issue #7, ADR-007).
//
// Covers all five FieldDataType coercions via FieldValueValidator.Coerce, plus the
// ContentItem.SetFieldValue guards (required / MaxLength / unknown-field) and storage
// semantics: upsert by FieldDefinitionId and sibling-column clearing (ApplyParsed
// assigns every typed column, so a parsed value carries at most one). Written against
// the Wave-1 shapes; blank raw input coerces to Empty (no value) and required-field
// enforcement stays with SetFieldValue.

using ContentFlow.Domain.Content;
using ContentFlow.Domain.Shared;

namespace ContentFlow.Domain.Tests.Content;

public sealed class FieldValueCoercionTests
{
    private static ContentType NewType() => new("Articles", "articles");

    private static FieldDefinition NewField(
        Guid typeId,
        string key,
        FieldDataType dataType,
        bool isRequired = false,
        int? maxLength = null,
        string name = "Field") =>
        new(typeId, name, key, dataType, isRequired, maxLength: maxLength);

    private static ContentItem NewItem(Guid typeId) => new(typeId, "hello-world");

    // --- Coerce: text types ---

    [Theory]
    [InlineData(FieldDataType.Text)]
    [InlineData(FieldDataType.LongText)]
    public void Coerce_TextTypes_PreservesRaw(FieldDataType dataType)
    {
        var result = FieldValueValidator.Coerce(dataType, "Hello, world!");

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello, world!", result.Value!.TextValue);
        Assert.Null(result.Value.NumberValue);
        Assert.Null(result.Value.BooleanValue);
        Assert.Null(result.Value.DateTimeValue);
    }

    [Theory]
    [InlineData(FieldDataType.Text)]
    [InlineData(FieldDataType.LongText)]
    [InlineData(FieldDataType.Number)]
    [InlineData(FieldDataType.Boolean)]
    [InlineData(FieldDataType.DateTime)]
    public void Coerce_Blank_ReturnsEmpty(FieldDataType dataType)
    {
        foreach (var raw in new string?[] { null, "", "   " })
        {
            var result = FieldValueValidator.Coerce(dataType, raw);

            Assert.True(result.IsSuccess);
            var parsed = result.Value!;
            Assert.Null(parsed.TextValue);
            Assert.Null(parsed.NumberValue);
            Assert.Null(parsed.BooleanValue);
            Assert.Null(parsed.DateTimeValue);
        }
    }

    [Fact]
    public void Coerce_Text_ExceedingMaxLength_FailsWithMaxLength()
    {
        var result = FieldValueValidator.Coerce(FieldDataType.Text, "toolong", maxLength: 3, fieldKey: "title");

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.MaxLength, result.Error!.Code);
    }

    [Fact]
    public void Coerce_Text_WithinMaxLength_Succeeds()
    {
        var result = FieldValueValidator.Coerce(FieldDataType.Text, "abc", maxLength: 3, fieldKey: "title");

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value!.TextValue);
    }

    // --- Coerce: number ---

    [Theory]
    [InlineData("42", 42)]
    [InlineData("  3.5 ", 3.5)]
    [InlineData("-1.25", -1.25)]
    public void Coerce_Number_Valid_ParsesInvariant(string raw, double expected)
    {
        var result = FieldValueValidator.Coerce(FieldDataType.Number, raw, fieldKey: "count");

        Assert.True(result.IsSuccess);
        Assert.Equal((decimal)expected, result.Value!.NumberValue);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12px")]
    [InlineData("one")]
    public void Coerce_Number_Invalid_FailsWithTypeMismatch(string raw)
    {
        var result = FieldValueValidator.Coerce(FieldDataType.Number, raw, fieldKey: "count");

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.TypeMismatch, result.Error!.Code);
    }

    // --- Coerce: boolean ---

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData(" true ", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    [InlineData(" 0 ", false)]
    public void Coerce_Boolean_Valid(string raw, bool expected)
    {
        var result = FieldValueValidator.Coerce(FieldDataType.Boolean, raw, fieldKey: "flag");

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.BooleanValue);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("2")]
    [InlineData("maybe")]
    [InlineData("on")]
    public void Coerce_Boolean_Invalid_FailsWithTypeMismatch(string raw)
    {
        var result = FieldValueValidator.Coerce(FieldDataType.Boolean, raw, fieldKey: "flag");

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.TypeMismatch, result.Error!.Code);
    }

    // --- Coerce: datetime ---

    [Fact]
    public void Coerce_DateTime_Valid_Roundtrips()
    {
        var result = FieldValueValidator.Coerce(FieldDataType.DateTime, "2026-10-09T12:00:00+00:00", fieldKey: "at");

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero), result.Value!.DateTimeValue);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("32.13.2026")]
    public void Coerce_DateTime_Invalid_FailsWithTypeMismatch(string raw)
    {
        var result = FieldValueValidator.Coerce(FieldDataType.DateTime, raw, fieldKey: "at");

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.TypeMismatch, result.Error!.Code);
    }

    [Fact]
    public void Coerce_UnknownDataType_FailsWithTypeMismatch()
    {
        var result = FieldValueValidator.Coerce((FieldDataType)999, "x", fieldKey: "mystery");

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.TypeMismatch, result.Error!.Code);
    }

    // --- SetFieldValue guards ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetFieldValue_RequiredBlank_FailsWithFieldRequired(string? raw)
    {
        var type = NewType();
        var field = NewField(type.Id, "title", FieldDataType.Text, isRequired: true);
        var item = NewItem(type.Id);

        var result = item.SetFieldValue(field, raw);

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.FieldRequired, result.Error!.Code);
        Assert.Empty(item.FieldValues);
    }

    [Fact]
    public void SetFieldValue_OptionalBlank_SucceedsWithEmptyValue()
    {
        var type = NewType();
        var field = NewField(type.Id, "subtitle", FieldDataType.Text);
        var item = NewItem(type.Id);

        var result = item.SetFieldValue(field, null);

        Assert.True(result.IsSuccess);
        var value = Assert.Single(item.FieldValues);
        Assert.Null(value.TextValue);
        Assert.Null(value.NumberValue);
        Assert.Null(value.BooleanValue);
        Assert.Null(value.DateTimeValue);
    }

    [Fact]
    public void SetFieldValue_ForeignField_FailsWithFieldUnknown()
    {
        var type = NewType();
        var foreign = NewField(Guid.NewGuid(), "title", FieldDataType.Text);
        var item = NewItem(type.Id);

        var result = item.SetFieldValue(foreign, "hi");

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.FieldUnknown, result.Error!.Code);
        Assert.Empty(item.FieldValues);
    }

    [Fact]
    public void SetFieldValue_NullField_ThrowsArgumentNullException()
    {
        var item = NewItem(Guid.NewGuid());

        Assert.Throws<ArgumentNullException>(() => item.SetFieldValue(null!, "hi"));
    }

    [Fact]
    public void SetFieldValue_CoercionFailure_PropagatesTypeMismatchWithoutStoring()
    {
        var type = NewType();
        var field = NewField(type.Id, "count", FieldDataType.Number);
        var item = NewItem(type.Id);

        var result = item.SetFieldValue(field, "abc");

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.TypeMismatch, result.Error!.Code);
        Assert.Empty(item.FieldValues);
    }

    [Fact]
    public void SetFieldValue_ExceedingMaxLength_FailsWithMaxLength()
    {
        var type = NewType();
        var field = NewField(type.Id, "title", FieldDataType.Text, maxLength: 3);
        var item = NewItem(type.Id);

        var result = item.SetFieldValue(field, "toolong");

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.MaxLength, result.Error!.Code);
        Assert.Empty(item.FieldValues);
    }

    // --- Storage semantics: upsert + sibling clearing ---

    [Fact]
    public void SetFieldValue_Twice_UpsertsSameValue()
    {
        var type = NewType();
        var field = NewField(type.Id, "title", FieldDataType.Text);
        var item = NewItem(type.Id);
        Assert.True(item.SetFieldValue(field, "first").IsSuccess);

        var result = item.SetFieldValue(field, "second");

        Assert.True(result.IsSuccess);
        var value = Assert.Single(item.FieldValues);
        Assert.Equal("second", value.TextValue);
    }

    [Fact]
    public void SetFieldValue_ClearingOptional_ReplacesValueWithEmpty()
    {
        var type = NewType();
        var field = NewField(type.Id, "count", FieldDataType.Number);
        var item = NewItem(type.Id);
        Assert.True(item.SetFieldValue(field, "42").IsSuccess);

        var result = item.SetFieldValue(field, null);

        Assert.True(result.IsSuccess);
        var value = Assert.Single(item.FieldValues);
        Assert.Null(value.NumberValue);
    }

    [Fact]
    public void ApplyParsed_ClearsSiblingColumns()
    {
        var value = new ContentFieldValue(Guid.NewGuid(), Guid.NewGuid());
        value.ApplyParsed(new ParsedFieldValue(FieldDataType.Number, null, 42m, null, null));
        Assert.Equal(42m, value.NumberValue);

        value.ApplyParsed(new ParsedFieldValue(FieldDataType.Text, "hi", null, null, null));

        Assert.Equal("hi", value.TextValue);
        Assert.Null(value.NumberValue);
        Assert.Null(value.BooleanValue);
        Assert.Null(value.DateTimeValue);
    }

    [Fact]
    public void ApplyParsed_Null_ThrowsArgumentNullException()
    {
        var value = new ContentFieldValue(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentNullException>(() => value.ApplyParsed(null!));
    }

    // --- End-to-end: every datatype through SetFieldValue ---

    [Fact]
    public void SetFieldValue_AllDataTypes_PersistTypedColumns()
    {
        var type = NewType();
        var text = NewField(type.Id, "title", FieldDataType.Text);
        var longText = NewField(type.Id, "body", FieldDataType.LongText);
        var number = NewField(type.Id, "count", FieldDataType.Number);
        var boolean = NewField(type.Id, "flag", FieldDataType.Boolean);
        var dateTime = NewField(type.Id, "at", FieldDataType.DateTime);
        var item = NewItem(type.Id);

        Assert.True(item.SetFieldValue(text, "hello").IsSuccess);
        Assert.True(item.SetFieldValue(longText, "multi\nline").IsSuccess);
        Assert.True(item.SetFieldValue(number, "2.5").IsSuccess);
        Assert.True(item.SetFieldValue(boolean, "true").IsSuccess);
        Assert.True(item.SetFieldValue(dateTime, "2026-10-09T12:00:00+00:00").IsSuccess);

        Assert.Equal(5, item.FieldValues.Count);
        Assert.Equal("hello", item.FieldValues.Single(v => v.FieldDefinitionId == text.Id).TextValue);
        Assert.Equal("multi\nline", item.FieldValues.Single(v => v.FieldDefinitionId == longText.Id).TextValue);
        Assert.Equal(2.5m, item.FieldValues.Single(v => v.FieldDefinitionId == number.Id).NumberValue);
        Assert.True(item.FieldValues.Single(v => v.FieldDefinitionId == boolean.Id).BooleanValue);
        Assert.Equal(
            new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero),
            item.FieldValues.Single(v => v.FieldDefinitionId == dateTime.Id).DateTimeValue);
    }
}

