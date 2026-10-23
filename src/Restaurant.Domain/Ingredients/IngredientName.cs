namespace Restaurant.Domain.Ingredients;

public sealed record IngredientName
{
    private const int MinLength = 2;
    private const int MaxLength = 60;

    private IngredientName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static IngredientName Create(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is < MinLength or > MaxLength)
        {
            throw IngredientErrors.InvalidName();
        }

        return new IngredientName(trimmed);
    }
}
