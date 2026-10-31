using FoodDelivery.Pricing;

namespace FoodDelivery.Domain;

public enum PaymentStatus
{
    Unpaid,
    Paid,
    Refunded
}

public sealed record PaymentRecord(string MethodName, string TransactionId, long Amount);

public sealed class Order
{
    public const int MaxItems = 20;

    private readonly List<IMenuItem> items = new();
    private IFeePolicy feePolicy;

    public int Id { get; }
    public Restaurant Restaurant { get; }
    public Location Destination { get; }
    public OrderState State { get; private set; } = OrderState.Placed;
    public DeliveryDriver? Driver { get; private set; }
    public IReadOnlyList<IMenuItem> Items => items;
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Unpaid;
    public PaymentRecord? Payment { get; private set; }

    public Order(int id, Restaurant restaurant, Location destination, IFeePolicy feePolicy)
    {
        Id = id;
        Restaurant = restaurant ?? throw new ArgumentNullException(nameof(restaurant));
        Destination = destination;
        this.feePolicy = feePolicy ?? throw new ArgumentNullException(nameof(feePolicy));
    }

    public long Subtotal => items.Sum(item => item.Price);
    public double DistanceKm => Restaurant.Location.DistanceTo(Destination);
    public long DeliveryFee => feePolicy.CalculateFee(DistanceKm);
    public long Total => Subtotal + DeliveryFee;
    public string FeePolicyName => feePolicy.Name;
    public bool CanCancel => CanMove(State, OrderState.Cancelled);

    public void SetFeePolicy(IFeePolicy policy)
    {
        feePolicy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public void AddItem(IMenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (State != OrderState.Placed)
            throw new InvalidOperationException($"Cannot add items when order is {State}");
        if (items.Count >= MaxItems)
            throw new InvalidOperationException("Order is full");
        items.Add(item);
    }

    public void Confirm()
    {
        if (items.Count == 0)
            throw new InvalidOperationException("Cannot confirm an empty order");
        MoveTo(OrderState.Confirmed);
    }

    public void StartPreparing() => MoveTo(OrderState.Preparing);

    public void Cancel() => MoveTo(OrderState.Cancelled);

    public void EnsureCanCancel() => CheckMove(OrderState.Cancelled);

    public void Dispatch(DeliveryDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        CheckMove(OrderState.OutForDelivery);
        driver.Reserve();
        State = OrderState.OutForDelivery;
        Driver = driver;
    }

    public void Deliver()
    {
        CheckMove(OrderState.Delivered);
        if (PaymentStatus != PaymentStatus.Paid)
            throw new InvalidOperationException("Order must be paid before delivery");
        State = OrderState.Delivered;
        Driver!.Release(Destination);
    }

    internal void MarkPaid(PaymentRecord record)
    {
        Payment = record;
        PaymentStatus = PaymentStatus.Paid;
    }

    internal void MarkRefunded()
    {
        PaymentStatus = PaymentStatus.Refunded;
    }

    private static bool CanMove(OrderState from, OrderState to) => from switch
    {
        OrderState.Placed => to is OrderState.Confirmed or OrderState.Cancelled,
        OrderState.Confirmed => to is OrderState.Preparing or OrderState.Cancelled,
        OrderState.Preparing => to is OrderState.OutForDelivery,
        OrderState.OutForDelivery => to is OrderState.Delivered,
        _ => false
    };

    private void CheckMove(OrderState next)
    {
        if (!CanMove(State, next))
            throw new InvalidOrderTransitionException(State, next);
    }

    private void MoveTo(OrderState next)
    {
        CheckMove(next);
        State = next;
    }
}
