record struct ModelInfo(
    string TypeFullName,
    string TypeName,
    EquatableArray<PropertyInfo> Properties,
    ActivatorPlan? Activator,
    EquatableArray<RowReaderSlot>? RowReaderSlots,
    EquatableArray<EnumRenderInfo> EnumRenders);

record struct EnumRenderInfo(
    string TypeFullName,
    EquatableArray<EnumMemberInfo> Members);

record struct EnumMemberInfo(
    string Name,
    string Display);

record struct ActivatorPlan(
    EquatableArray<ActivatorParam> CtorParams,
    EquatableArray<ActivatorAssign> InitProps,
    EquatableArray<ActivatorAssign> SetProps);

/// <summary>
/// A constructor parameter. <see cref="MemberName"/> is the column member it is filled from -
/// the one of the same name, ignoring case - or the parameter's own name where there is none.
/// </summary>
record struct ActivatorParam(
    string Name,
    string TypeFullName,
    string MemberName);

record struct ActivatorAssign(
    string Name,
    string TypeFullName);

/// <summary>
/// One slot in the model's column ordering — matches the runtime reader's
/// property enumeration. <see cref="Assignment"/> tells the row-reader emitter
/// how to land the parsed value (ctor arg, init initializer, post-construct
/// setter, or skip when the property has no writer).
/// </summary>
record struct RowReaderSlot(
    string Name,
    string TypeFullName,
    string ReaderTypeKey,
    bool IsNullable,
    SlotAssignment Assignment);

enum SlotAssignment
{
    None,
    CtorArg,
    Init,
    Setter
}

record struct ModelResult(
    ModelInfo? Model,
    DiagnosticInfo? Diagnostic);

record struct DiagnosticInfo(
    string TypeName,
    Location? Location);
