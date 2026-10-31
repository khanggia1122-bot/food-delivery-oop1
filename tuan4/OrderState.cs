namespace FoodDelivery.Domain;

public enum OrderState
{
    Placed,
    Confirmed,
    Preparing,
    OutForDelivery,
    Delivered,
    Cancelled
}

public sealed class InvalidOrderTransitionException : InvalidOperationException
{
    public OrderState From { get; }
    public OrderState To { get; }

    public InvalidOrderTransitionException(OrderState from, OrderState to)
        : base($"Invalid transition: {from} -> {to}")
    {
        From = from;
        To = to;
    }
}
