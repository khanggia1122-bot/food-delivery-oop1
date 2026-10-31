using FoodDelivery.Dispatch;
using FoodDelivery.Domain;
using FoodDelivery.Pricing;
using FoodDelivery.Payment;

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
        PaymentFlowTests();
        PaymentMethodTests();
        ExtensibilityTests();

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
        new PaymentProcessor().Pay(order, new CashPayment(order.Total));
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

    private static Order OrderConfirmed(Restaurant r, int id)
    {
        Order order = NewOrder(r, id);
        order.Confirm();
        return order;
    }

    private static void PaymentFlowTests()
    {
        Console.WriteLine("=== TEST 5: Payment process (Strategy + Observer) ===");
        Restaurant r = NewRestaurant();
        var output = new StringWriter();
        var processor = new PaymentProcessor();
        new ReceiptPrinter(output).Subscribe(processor);

        var wallet = new EWalletPayment("Khang", 500000);
        Order a = OrderConfirmed(r, 1);
        PaymentRecord record = processor.Pay(a, wallet);
        Check(a.PaymentStatus == PaymentStatus.Paid && a.Payment == record && record.Amount == 129000, "Wallet pays the order total (129000 VND)");
        Check(wallet.Balance == 500000 - 129000, "Wallet balance is reduced by the total");
        Check(output.ToString().Contains("Order #1 paid 129000"), "Observer: the receipt printer was notified once payment completed");
        Throws<InvalidOperationException>(() => processor.Pay(a, wallet), "Paying the same order twice is rejected");

        Order placed = NewOrder(r, 2);
        Throws<InvalidOperationException>(() => processor.Pay(placed, new EWalletPayment("K", 500000)), "Prepaid method is rejected while the order is only Placed");
        Throws<InvalidOperationException>(() => processor.Pay(OrderConfirmed(r, 3), new CashPayment(200000)), "Cash on delivery is rejected before delivery starts");

        var poor = new EWalletPayment("Poor", 100000);
        Order c = OrderConfirmed(r, 4);
        int linesBefore = output.ToString().Split('\n').Length;
        Throws<PaymentDeclinedException>(() => processor.Pay(c, poor), "Insufficient balance is declined");
        Check(c.PaymentStatus == PaymentStatus.Unpaid && poor.Balance == 100000, "Declined payment changes nothing: order unpaid, balance untouched");
        Check(output.ToString().Split('\n').Length == linesBefore, "No receipt is printed for a declined payment");

        Order d = OrderInPreparing(r, 5);
        d.Dispatch(new DeliveryDriver("An", new Location(1, 1)));
        Throws<InvalidOperationException>(d.Deliver, "Delivering an unpaid order is rejected");
        Check(d.State == OrderState.OutForDelivery, "Order stays OutForDelivery after the rejected delivery");
        var cash = new CashPayment(150000);
        processor.Pay(d, cash);
        Check(cash.ChangeDue == 150000 - 129000, "Cash payment at the door computes the change (21000)");
        d.Deliver();
        Check(d.State == OrderState.Delivered, "Paid order can be delivered");

        Order e = OrderConfirmed(r, 6);
        var wallet2 = new EWalletPayment("Lan", 300000);
        processor.Pay(e, wallet2);
        processor.CancelWithRefund(e);
        Check(e.State == OrderState.Cancelled && e.PaymentStatus == PaymentStatus.Refunded && wallet2.Balance == 300000, "Cancel while Confirmed refunds the wallet in full");
        Check(output.ToString().Contains("Order #6 refunded 129000"), "Observer: refund notification was sent");

        Order f = OrderConfirmed(r, 7);
        var wallet3 = new EWalletPayment("Minh", 300000);
        processor.Pay(f, wallet3);
        f.StartPreparing();
        Throws<InvalidOrderTransitionException>(() => processor.CancelWithRefund(f), "Cancel with refund is rejected once cooking has started");
        Check(f.PaymentStatus == PaymentStatus.Paid && wallet3.Balance == 300000 - 129000, "Rejected cancel does not refund: payment is still Paid");

        Order g = NewOrder(r, 8);
        processor.CancelWithRefund(g);
        Check(g.State == OrderState.Cancelled && g.PaymentStatus == PaymentStatus.Unpaid, "Cancelling an unpaid order needs no refund");
    }

    private static void PaymentMethodTests()
    {
        Console.WriteLine("=== TEST 6: Payment methods behind IPaymentMethod ===");
        IPaymentMethod[] methods =
        {
            new EWalletPayment("Khang", 5000),
            new CardPayment("4111 1111 1111 1111", 5000),
            new CashPayment(5000)
        };
        bool allApproved = true;
        foreach (IPaymentMethod m in methods)
            if (!m.Charge(1000).Success) allApproved = false;
        Check(allApproved, "Wallet, card and cash are all charged through the same interface");
        Check(methods[0].Timing == PaymentTiming.Prepaid && methods[2].Timing == PaymentTiming.OnDelivery, "Each method reports its own timing");

        var card = new CardPayment("4111111111111111", 200000);
        PaymentResult ok = card.Charge(150000);
        Check(ok.Success && card.AvailableCredit == 50000, "Card charge uses up credit");
        Check(!card.Charge(60000).Success, "Card over its credit limit is declined");
        Check(card.Refund(ok.TransactionId, 150000).Success && card.AvailableCredit == 200000, "Card refund restores the credit");
        Check(!card.Refund(ok.TransactionId, 150000).Success, "The same transaction cannot be refunded twice");
        Check(!new CardPayment("4111111111111112", 200000).Charge(1000).Success, "Card number failing the Luhn check is declined");
        Check(!new CardPayment("4111111111111111", 200000).Charge(0).Success, "Zero amount is declined by the base class");

        var wallet = new EWalletPayment("Khang", 100000);
        PaymentResult w = wallet.Charge(40000);
        Check(!wallet.Refund(w.TransactionId, 39999).Success, "Partial refund amount is declined");
        Check(!wallet.Refund("WALLET-9999", 40000).Success, "Unknown transaction is declined");
        Check(wallet.Refund(w.TransactionId, 40000).Success && wallet.Balance == 100000, "Correct refund restores the wallet balance");
    }

    private sealed class VoucherPayment : IPaymentMethod
    {
        public string Name => "Voucher";
        public PaymentTiming Timing => PaymentTiming.Prepaid;
        public int Refunds { get; private set; }

        public PaymentResult Charge(long amount) =>
            amount <= 200000 ? PaymentResult.Approved("VOUCHER-1") : PaymentResult.Declined("Voucher limit is 200000");

        public PaymentResult Refund(string transactionId, long amount)
        {
            Refunds++;
            return PaymentResult.Approved(transactionId, "Voucher restored");
        }
    }

    private static void ExtensibilityTests()
    {
        Console.WriteLine("=== TEST 7: A new payment method needs no change to PaymentProcessor ===");
        Restaurant r = NewRestaurant();
        var processor = new PaymentProcessor();
        var voucher = new VoucherPayment();

        Order order = OrderConfirmed(r, 1);
        PaymentRecord rec = processor.Pay(order, voucher);
        Check(rec.MethodName == "Voucher" && order.PaymentStatus == PaymentStatus.Paid, "Processor accepts a brand-new IPaymentMethod implementation");
        processor.CancelWithRefund(order);
        Check(voucher.Refunds == 1 && order.PaymentStatus == PaymentStatus.Refunded, "Refund is routed to the same method that was charged");
    }
}
