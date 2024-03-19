namespace YBot.Models;

public sealed record ConstDice : Dice
{
    public static readonly ConstDice ConstOne = new(1);

    public ConstDice(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        Value = value;
    }

    protected override Dice Face => ConstOne;

    protected override Dice Count => this;

    public int Value { get; }

    public bool Equals(ConstDice? other) { return base.Equals(other); }

    public static implicit operator int(ConstDice dice) { return dice.Value; }

    public override int GetHashCode() { return base.GetHashCode(); }

    public override string ToString() { return Value.ToString(); }
}