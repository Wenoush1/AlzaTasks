namespace TeleportDelivery.Model;

public sealed class Fleet
{
    public Fleet(IEnumerable<TruckCapacity> trucks)
    {
        Trucks = [.. trucks];
        if (Trucks.Count == 0) 
            throw new ArgumentException("A fleet needs at least one truck.", nameof(trucks));

        TotalWeightKg = Trucks.Sum(t => t.MaxWeightKg);
        TotalVolumeM3 = Trucks.Sum(t => t.MaxVolumeM3);
    }
    
    public static Fleet Default { get; } = Uniform(truckCount: 120, maxWeightKg: 5_500, maxVolumeM3: 7);

    public IReadOnlyList<TruckCapacity> Trucks { get; }

    public double TotalWeightKg { get; }
    public double TotalVolumeM3 { get; }
    
    private static Fleet Uniform(int truckCount, double maxWeightKg, double maxVolumeM3) =>
        new(Enumerable.Repeat(new TruckCapacity(maxWeightKg, maxVolumeM3), truckCount));
}
