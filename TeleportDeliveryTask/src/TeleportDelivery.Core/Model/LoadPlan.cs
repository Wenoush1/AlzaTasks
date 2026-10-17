namespace TeleportDelivery.Model;

public sealed class LoadPlan
{
    public const int NotLoaded = -1;
    
    public required int[] VanOfPackage { get; init; }
    public required Truck[] Vans { get; init; }

    public double TotalProfit => Vans.Sum(v => v.Profit);
    public int LoadedCount => Vans.Sum(v => v.PackageCount);
}
