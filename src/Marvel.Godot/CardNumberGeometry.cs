using Godot;

namespace Marvel.Godot;

/// <summary>Places independent printed marks around the measured numeral, never around its cell.</summary>
internal sealed class CardNumberGeometry
{
    internal Rect2 Number { get; }
    internal Rect2 Underline { get; }
    internal Rect2 Special { get; }
    internal Rect2 PerPlayer { get; }
    internal IReadOnlyList<Rect2> Consequences { get; }

    internal CardNumberGeometry(Rect2 number, float baseline, CardNumberMarks marks, float scale, bool full)
    {
        Number = number;
        float specialSize = (full ? 10 : 6.5f) * scale;
        float damageSize = (full ? 9 : 6) * scale;
        float gap = (full ? 2 : 1) * scale;
        float right = number.End.X + 0.5f * scale;
        float top = number.Position.Y;
        Special = marks.SpecialStar ? new Rect2(right, top, specialSize, specialSize) : default;
        PerPlayer = marks.PerPlayer ? new Rect2(right + (marks.SpecialStar ? specialSize + gap : 0),
            top, specialSize, specialSize) : default;
        Underline = new Rect2(number.Position.X - scale, baseline + scale,
            number.Size.X + 2 * scale, (full ? 2 : 1.5f) * scale);
        float groupWidth = marks.Consequences * damageSize + Math.Max(0, marks.Consequences - 1) * gap;
        float groupLeft = number.GetCenter().X - groupWidth / 2;
        Consequences = Enumerable.Range(0, marks.Consequences)
            .Select(index => new Rect2(groupLeft + index * (damageSize + gap),
                baseline + (full ? 18 : 4) * scale, damageSize, damageSize)).ToArray();
    }
}
