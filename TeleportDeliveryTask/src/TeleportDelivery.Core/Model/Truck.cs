namespace TeleportDelivery.Model;

public sealed class Truck(TruckCapacity spec)
{
    public double MaxWeightKg { get; } = spec.MaxWeightKg;
    public double MaxVolumeM3 { get; } = spec.MaxVolumeM3;

    public double WeightKg { get; private set; }
    public double VolumeM3 { get; private set; }
    public double Profit { get; private set; }
    public int PackageCount { get; private set; }

    public double FreeWeightKg => MaxWeightKg - WeightKg;
    public double FreeVolumeM3 => MaxVolumeM3 - VolumeM3;

    public bool Fits(in Package p) => p.WeightKg <= FreeWeightKg && p.VolumeM3 <= FreeVolumeM3;

    public void Add(in Package p)
    {
        WeightKg += p.WeightKg;
        VolumeM3 += p.VolumeM3;
        Profit += p.Profit;
        PackageCount++;
    }
}
