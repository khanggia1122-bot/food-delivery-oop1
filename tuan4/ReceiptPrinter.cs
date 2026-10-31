namespace FoodDelivery.Payment;

public sealed class ReceiptPrinter
{
    private readonly TextWriter output;

    public ReceiptPrinter(TextWriter output)
    {
        this.output = output;
    }

    public void Subscribe(PaymentProcessor processor)
    {
        processor.PaymentCompleted += OnPaymentCompleted;
        processor.PaymentRefunded += OnPaymentRefunded;
    }

    private void OnPaymentCompleted(object? sender, PaymentEventArgs e)
    {
        output.WriteLine($"  [receipt] Order #{e.Order.Id} paid {e.Record.Amount} VND via {e.Record.MethodName} ({e.Record.TransactionId})");
    }

    private void OnPaymentRefunded(object? sender, PaymentEventArgs e)
    {
        output.WriteLine($"  [receipt] Order #{e.Order.Id} refunded {e.Record.Amount} VND via {e.Record.MethodName} ({e.Record.TransactionId})");
    }
}
