namespace Restaurant.Domain.Dishes;

public sealed record DishName
{
    private const int MinLength = 2;
    private const int MaxLength = 80;

    private DishName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static DishName Create(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is < MinLength or > MaxLength)
        {
            throw DishErrors.InvalidName();
        }

        return new DishName(trimmed);
    }
}
