namespace FoodDelivery.Domain;

public sealed class BasicItem : IMenuItem
{
    public string Name { get; }
    public long Price { get; }

    public BasicItem(string name, long price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must not be empty", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must not be negative");
        Name = name;
        Price = price;
    }
}

public abstract class ItemDecorator : IMenuItem
{
    protected IMenuItem Inner { get; }

    protected ItemDecorator(IMenuItem inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public abstract string Name { get; }
    public abstract long Price { get; }
}

public sealed class ToppingDecorator : ItemDecorator
{
    private readonly string toppingName;
    private readonly long extraPrice;

    public ToppingDecorator(IMenuItem inner, string toppingName, long extraPrice) : base(inner)
    {
        if (extraPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(extraPrice), "Topping price must not be negative");
        this.toppingName = toppingName;
        this.extraPrice = extraPrice;
    }

    public override string Name => $"{Inner.Name} + {toppingName}";
    public override long Price => Inner.Price + extraPrice;
}

public sealed class SizeUpgradeDecorator : ItemDecorator
{
    private readonly string sizeName;
    private readonly int percent;

    public SizeUpgradeDecorator(IMenuItem inner, string sizeName, int percent) : base(inner)
    {
        if (percent <= 0)
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be positive");
        this.sizeName = sizeName;
        this.percent = percent;
    }

    public override string Name => $"{Inner.Name} ({sizeName})";
    public override long Price => Inner.Price * percent / 100;
}
