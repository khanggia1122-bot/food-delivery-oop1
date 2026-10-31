using FoodDelivery.Dispatch;
using FoodDelivery.Domain;
using FoodDelivery.Pricing;

namespace FoodDelivery;

public static class Demo
{
    public static void Run()
    {
        Console.WriteLine("=== DEMO: one order end to end ===");

        var restaurant = new Restaurant("Pho 24", new Location(0, 0));
        restaurant.AddMenuItem("Pho Bo", 50000);
        restaurant.AddMenuItem("Tra Sua", 30000);

        IFeePolicy fee = new DistanceFeePolicy(10000, 4000);
        var drivers = new List<DeliveryDriver>
        {
            new("An", new Location(1, 1)),
            new("Binh", new Location(6, 8))
        };
        IDriverAssigner assigner = new NearestDriverAssigner();

        var order = new Order(1, restaurant, new Location(3, 4), fee);
        order.AddItem(new ToppingDecorator(restaurant.CreateItem(0), "Trung", 10000));
        order.AddItem(new SizeUpgradeDecorator(restaurant.CreateItem(1), "Large", 130));
        Print(order);


        order.Confirm();
        order.StartPreparing();
        DeliveryDriver driver = assigner.Assign(order, drivers);
        Console.WriteLine($"  assigned driver: {driver.Name}, state: {order.State}");
        order.Deliver();
        Console.WriteLine($"  state: {order.State}, {driver.Name} available again: {driver.IsAvailable}");
    }

    private static void Print(Order order)
    {
        Console.WriteLine($"Order #{order.Id} | state: {order.State}");
        foreach (IMenuItem item in order.Items)
            Console.WriteLine($"  - {item.Name}: {item.Price} VND");
        Console.WriteLine($"  Subtotal {order.Subtotal} + fee {order.DeliveryFee} ({order.FeePolicyName}, {order.DistanceKm:0.##} km) = {order.Total} VND");
    }
}
