namespace YBot.Models;

public record Dice
{
    protected Dice() { }

    protected virtual Dice Face { get; private init; } = null!;

    protected virtual Dice Count { get; private init; } = null!;

    public static Dice New(int face, int count = 1)
    {
        return face switch
        {
            < 1               => throw new ArgumentOutOfRangeException(nameof(face)),
            1 when count == 1 => ConstDice.ConstOne,   // 1D1 always 1
            1                 => new ConstDice(count), // xD1 always x
            _                 => new Dice { Face = new ConstDice(face), Count = new ConstDice(count) }
        };
    }

    public int Roll()
    {
        if (this is ConstDice constDice) { return constDice.Value; }

        // Roll the dice
        int face   = Face;
        int count  = Count;
        var result = 0;
        for (var i = 0; i < count; i++) { result += Random.Shared.Next(0, face) + 1; }

        return result;
    }

    public static implicit operator int(Dice dice) { return dice.Roll(); }

    public override string ToString() { return $"{Count}D{Face}"; }
}