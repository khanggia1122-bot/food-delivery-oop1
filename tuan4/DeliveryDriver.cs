namespace FoodDelivery.Domain;

public sealed class DeliveryDriver
{
    public string Name { get; }
    public Location Position { get; private set; }
    public bool IsAvailable { get; private set; } = true;

    public DeliveryDriver(string name, Location position)
    {
        Name = name;
        Position = position;
    }

    public void Reserve()
    {
        if (!IsAvailable)
            throw new InvalidOperationException($"Driver {Name} is already busy");
        IsAvailable = false;
    }

    public void Release(Location newPosition)
    {
        IsAvailable = true;
        Position = newPosition;
    }
}
