namespace FoodDelivery.Domain;

public readonly record struct Location(double X, double Y)
{
    public double DistanceTo(Location other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
