namespace FoodDelivery;

public static class Program
{
    public static int Main()
    {
        Demo.Run();
        Console.WriteLine();
        return Tests.RunAll();
    }
}
