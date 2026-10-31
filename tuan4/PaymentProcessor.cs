using FoodDelivery.Domain;

namespace FoodDelivery.Payment;

public sealed class PaymentDeclinedException : Exception
{
    public string MethodName { get; }

    public PaymentDeclinedException(string methodName, string reason)
        : base($"{methodName} declined: {reason}")
    {
        MethodName = methodName;
    }
}

public sealed class PaymentEventArgs : EventArgs
{
    public Order Order { get; }
    public PaymentRecord Record { get; }

    public PaymentEventArgs(Order order, PaymentRecord record)
    {
        Order = order;
        Record = record;
    }
}

public sealed class PaymentProcessor
{
    private readonly Dictionary<int, IPaymentMethod> methodsByOrder = new();

    public event EventHandler<PaymentEventArgs>? PaymentCompleted;
    public event EventHandler<PaymentEventArgs>? PaymentRefunded;

    public PaymentRecord Pay(Order order, IPaymentMethod method)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(method);

        if (order.PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("Order is already paid");

        OrderState required = method.Timing == PaymentTiming.Prepaid
            ? OrderState.Confirmed
            : OrderState.OutForDelivery;
        if (order.State != required)
            throw new InvalidOperationException(
                $"{method.Name} can only be used when the order is {required} (now {order.State})");

        PaymentResult result = method.Charge(order.Total);
        if (!result.Success)
            throw new PaymentDeclinedException(method.Name, result.Message);

        var record = new PaymentRecord(method.Name, result.TransactionId, order.Total);
        order.MarkPaid(record);
        methodsByOrder[order.Id] = method;
        PaymentCompleted?.Invoke(this, new PaymentEventArgs(order, record));
        return record;
    }

    public void CancelWithRefund(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        order.EnsureCanCancel();

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            PaymentRecord record = order.Payment!;
            IPaymentMethod method = methodsByOrder[order.Id];
            PaymentResult result = method.Refund(record.TransactionId, record.Amount);
            if (!result.Success)
                throw new PaymentDeclinedException(method.Name, result.Message);
            order.MarkRefunded();
            order.Cancel();
            PaymentRefunded?.Invoke(this, new PaymentEventArgs(order, record));
            return;
        }

        order.Cancel();
    }
}
