using FoodDelivery.Dispatch;
using FoodDelivery.Domain;
using FoodDelivery.Pricing;

namespace FoodDelivery;

public static class Tests
{
    private static int passed;
    private static int failed;

    public static int RunAll()
    {
        ItemTests();
        LifecycleTests();
        ValidationTests();
        PolymorphismTests();

        Console.WriteLine();
        Console.WriteLine($"Result: {passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string name)
    {
        if (condition) passed++; else failed++;
        Console.WriteLine($"  [{(condition ? "PASS" : "FAIL")}] {name}");
    }

    private static void Throws<T>(Action action, string name) where T : Exception
    {
        try
        {
            action();
            Check(false, name);
        }
        catch (T)
        {
            Check(true, name);
        }
        catch (Exception e)
        {
            Check(false, $"{name} (threw {e.GetType().Name} instead)");
        }
    }

    private static Restaurant NewRestaurant()
    {
        var r = new Restaurant("Pho 24", new Location(0, 0));
        r.AddMenuItem("Pho Bo", 50000);
        r.AddMenuItem("Tra Sua", 30000);
        return r;
    }

    private static Order NewOrder(Restaurant r, int id = 1, IFeePolicy? policy = null)
    {
        var order = new Order(id, r, new Location(3, 4), policy ?? new DistanceFeePolicy(10000, 4000));
        order.AddItem(new ToppingDecorator(r.CreateItem(0), "Trung", 10000));
        order.AddItem(new SizeUpgradeDecorator(r.CreateItem(1), "Large", 130));
        return order;
    }

    private static void SettlePayment(Order order)
    {
    }

    private static Order OrderInPreparing(Restaurant r, int id = 1)
    {
        Order order = NewOrder(r, id);
        order.Confirm();
        order.StartPreparing();
        return order;
    }

    private static void ItemTests()
    {
        Console.WriteLine("=== TEST 1: Menu items and decorators ===");
        Restaurant r = NewRestaurant();
        IMenuItem plain = r.CreateItem(0);
        IMenuItem withEgg = new ToppingDecorator(r.CreateItem(0), "Trung", 10000);
        IMenuItem stacked = new SizeUpgradeDecorator(
            new ToppingDecorator(new ToppingDecorator(r.CreateItem(0), "Trung", 10000), "Bo vien", 15000), "Large", 130);

        Check(plain.Name == "Pho Bo" && plain.Price == 50000, "Plain item keeps its name and price");
        Check(withEgg.Name == "Pho Bo + Trung" && withEgg.Price == 60000, "Topping adds name and price");
        Check(stacked.Name == "Pho Bo + Trung + Bo vien (Large)" && stacked.Price == 97500, "Three stacked decorators: (50000+10000+15000) x 130% = 97500");
        Check(!ReferenceEquals(r.CreateItem(0), r.Menu[0]), "CreateItem returns a copy, not the menu object");
        Throws<ArgumentOutOfRangeException>(() => r.CreateItem(5), "Invalid menu index is rejected");
        Throws<ArgumentOutOfRangeException>(() => new BasicItem("Free?", -1), "Negative price is rejected");
        Throws<ArgumentNullException>(() => new ToppingDecorator(null!, "x", 1), "Decorator without an inner item is rejected");

        var full = new Restaurant("Big", new Location(0, 0));
        for (int i = 0; i < Restaurant.MaxMenuItems; i++) full.AddMenuItem("Item", 1000);
        Throws<InvalidOperationException>(() => full.AddMenuItem("Extra", 1000), "Menu with 10 items is full");
    }

    private static void LifecycleTests()
    {
        Console.WriteLine("=== TEST 2: Happy path through the state machine ===");
        Restaurant r = NewRestaurant();
        Order order = NewOrder(r);
        var drivers = new List<DeliveryDriver> { new("An", new Location(1, 1)), new("Binh", new Location(6, 8)) };
        IDriverAssigner assigner = new NearestDriverAssigner();

        Check(order.Subtotal == 99000 && order.DeliveryFee == 30000 && order.Total == 129000, "Subtotal 99000 + fee 30000 = 129000 VND");
        order.Confirm();
        order.StartPreparing();
        DeliveryDriver driver = assigner.Assign(order, drivers);
        Check(driver.Name == "An" && order.State == OrderState.OutForDelivery, "Nearest driver (An) is assigned and order is OutForDelivery");
        SettlePayment(order);
        order.Deliver();
        Check(order.State == OrderState.Delivered, "Order reaches Delivered");
        Check(driver.IsAvailable && driver.Position == new Location(3, 4), "Driver is free again and now stands at the customer's address");
    }

    private static void ValidationTests()
    {
        Console.WriteLine("=== TEST 3: Invalid operations are rejected ===");
        Restaurant r = NewRestaurant();

        Order a = NewOrder(r);
        Throws<InvalidOrderTransitionException>(a.StartPreparing, "Placed -> Preparing is rejected");
        a.Confirm();
        Throws<InvalidOrderTransitionException>(a.Confirm, "Confirming twice is rejected");
        Throws<InvalidOperationException>(() => a.AddItem(r.CreateItem(0)), "Adding an item after confirmation is rejected");
        a.Cancel();
        Check(a.State == OrderState.Cancelled, "Cancel is allowed while Confirmed");
        Throws<InvalidOrderTransitionException>(a.Confirm, "A cancelled order cannot be confirmed again");

        Order b = OrderInPreparing(r, 2);
        Throws<InvalidOrderTransitionException>(b.Cancel, "Cancel while Preparing is rejected");
        Check(b.State == OrderState.Preparing, "State is unchanged after a rejected cancel");

        var busy = new DeliveryDriver("An", new Location(1, 1));
        busy.Reserve();
        Throws<InvalidOperationException>(() => b.Dispatch(busy), "Dispatching a busy driver (double-booking) is rejected");
        Check(b.State == OrderState.Preparing && b.Driver is null, "Failed dispatch leaves the order untouched (no half-done state)");

        var drivers = new List<DeliveryDriver> { busy };
        Throws<InvalidOperationException>(() => new NearestDriverAssigner().Assign(b, drivers), "No available driver is reported");
        Check(b.State == OrderState.Preparing, "Order is still Preparing after a failed assignment");

        var free = new DeliveryDriver("Binh", new Location(2, 2));
        b.Dispatch(free);
        Throws<InvalidOrderTransitionException>(b.Cancel, "Cancel while OutForDelivery is rejected");
        SettlePayment(b);
        b.Deliver();
        Throws<InvalidOrderTransitionException>(b.Cancel, "Cancel after Delivered is rejected");

        Throws<InvalidOperationException>(() => new Order(3, r, new Location(1, 1), new DistanceFeePolicy(1, 1)).Confirm(), "Confirming an empty order is rejected");

        var big = new Order(4, r, new Location(1, 1), new DistanceFeePolicy(1, 1));
        for (int i = 0; i < Order.MaxItems; i++) big.AddItem(r.CreateItem(0));
        Throws<InvalidOperationException>(() => big.AddItem(r.CreateItem(0)), "The 21st item in an order is rejected");
    }

    private sealed class FarthestDriverAssigner : IDriverAssigner
    {
        public DeliveryDriver Assign(Order order, IReadOnlyList<DeliveryDriver> drivers)
        {
            DeliveryDriver far = drivers[0];
            foreach (DeliveryDriver d in drivers)
                if (order.Restaurant.Location.DistanceTo(d.Position) > order.Restaurant.Location.DistanceTo(far.Position))
                    far = d;
            order.Dispatch(far);
            return far;
        }
    }

    private static void PolymorphismTests()
    {
        Console.WriteLine("=== TEST 4: Polymorphism through interfaces ===");
        Restaurant r = NewRestaurant();
        Order order = NewOrder(r);

        IFeePolicy normal = new DistanceFeePolicy(10000, 4000);
        IFeePolicy surge = new SurgeFeePolicy(normal, 150);
        long normalTotal = order.Total;
        order.SetFeePolicy(surge);
        Check(normalTotal == 129000 && order.Total == 144000, "Swapping IFeePolicy changes the total: 129000 -> 144000 VND");
        Check(order.FeePolicyName.StartsWith("SurgeFee"), "Order reports the active policy name");
        Throws<ArgumentOutOfRangeException>(() => new SurgeFeePolicy(normal, 0), "Surge percent 0 is rejected");
        Throws<ArgumentOutOfRangeException>(() => new DistanceFeePolicy(-1, 0), "Negative fee is rejected");
        Check(new DistanceFeePolicy(10000, 4000).CalculateFee(0.3) == 11200, "Distance fee rounds correctly (0.3 km -> 11200)");

        IReadOnlyList<DeliveryDriver> NewDrivers() => new List<DeliveryDriver>
        {
            new("An", new Location(1, 1)), new("Binh", new Location(6, 8))
        };
        Order o1 = OrderInPreparing(r, 5);
        Order o2 = OrderInPreparing(r, 6);
        IDriverAssigner nearest = new NearestDriverAssigner();
        IDriverAssigner farthest = new FarthestDriverAssigner();
        Check(nearest.Assign(o1, NewDrivers()).Name == "An" && farthest.Assign(o2, NewDrivers()).Name == "Binh",
              "Same call through IDriverAssigner, two implementations, two different drivers");

        var tie = new List<DeliveryDriver> { new("A", new Location(3, 4)), new("B", new Location(-3, -4)) };
        Check(nearest.Assign(OrderInPreparing(r, 7), tie).Name == "A", "Equal distance: the first driver in the list wins");

        IMenuItem[] mixed = { r.CreateItem(0), new ToppingDecorator(r.CreateItem(0), "Trung", 10000) };
        long sum = 0;
        foreach (IMenuItem item in mixed) sum += item.Price;
        Check(sum == 110000, "Plain and decorated items are used the same way through IMenuItem");
    }

}
