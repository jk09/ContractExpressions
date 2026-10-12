using ContractCheck.Samples.Stack;

namespace ContractCheck.Samples.PropertyTests;

/// <summary>
/// Properties of <see cref="INonNullStack{T}"/> and <see cref="INonNullStackWithUpdates{T}"/>; the single-call ones port the
/// ContractExpressions4 demo properties (Demo/Stack/StackDemoTests.cs, Demo/NonNullStackWithUpdate/NonNullStackWithUpdateDemoTests.cs).
/// </summary>
[Properties(Arbitrary = [typeof(Generators)])]
public class NonNullStackProperties
{
    [Property]
    public Property Push_keeps_the_contracts(string value) =>
        DbcPropertyTest.Check(() => Dbc.Make<INonNullStack<string>>(new NonNullStack<string>(0)), stack => stack.Push(value));

    [Property]
    public Property Pop_after_a_push_keeps_the_contracts(NonEmptyString value) =>
        DbcPropertyTest.Check(
            () =>
            {
                INonNullStack<string> stack = Dbc.Make<INonNullStack<string>>(new NonNullStack<string>(0));
                stack.Push(value.Get);
                return stack;
            },
            stack => stack.Pop());

    [Property]
    public Property UpdateAt_keeps_the_contracts(NonEmptyString value) =>
        DbcPropertyTest.Check(
            () =>
            {
                INonNullStackWithUpdates<string> stack = Dbc.Make<INonNullStackWithUpdates<string>>(new NonNullStackWithUpdates<string>(1));
                stack.Push("seed");
                return stack;
            },
            stack => stack.UpdateAt(0, value.Get));

    /// <summary>Random calls of every member, generated from the interface (no step records): rejected calls are skipped.</summary>
    [Property]
    public Property Any_calls_keep_the_contracts() =>
        DbcPropertyTest.CheckCalls(() => Dbc.Make<INonNullStackWithUpdates<string>>(new NonNullStackWithUpdates<string>(0)));

    /// <summary>Null values, pops of an empty stack and out-of-range updates are rejected by preconditions and skipped.</summary>
    [Property]
    public Property Pushes_pops_and_updates_keep_the_contracts(StackStep[] steps) =>
        DbcPropertyTest.CheckSequence(
            () => Dbc.Make<INonNullStackWithUpdates<string>>(new NonNullStackWithUpdates<string>(0)),
            steps.Select(step => (Action<INonNullStackWithUpdates<string>>)step.Apply));
}
