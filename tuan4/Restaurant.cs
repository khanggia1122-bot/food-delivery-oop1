namespace FoodDelivery.Domain;

public sealed class Restaurant
{
    public const int MaxMenuItems = 10;

    private readonly List<IMenuItem> menu = new();

    public string Name { get; }
    public Location Location { get; }
    public IReadOnlyList<IMenuItem> Menu => menu;

    public Restaurant(string name, Location location)
    {
        Name = name;
        Location = location;
    }

    public void AddMenuItem(string name, long price)
    {
        if (menu.Count >= MaxMenuItems)
            throw new InvalidOperationException("Menu is full");
        menu.Add(new BasicItem(name, price));
    }

    public IMenuItem CreateItem(int index)
    {
        if (index < 0 || index >= menu.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Menu item does not exist");
        return new BasicItem(menu[index].Name, menu[index].Price);
    }
}
