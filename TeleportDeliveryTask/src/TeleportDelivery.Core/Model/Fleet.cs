namespace TeleportDelivery.Model;

public sealed class Fleet
{
    public Fleet(IEnumerable<TruckCapacity> vans)
    {
        Vans = [.. vans];
        if (Vans.Count == 0) 
            throw new ArgumentException("A fleet needs at least one van.", nameof(vans));
    }
    
    public static Fleet Default { get; } = Uniform(vanCount: 120, maxWeightKg: 5_500, maxVolumeM3: 7);

    public IReadOnlyList<TruckCapacity> Vans { get; }
    
    private static Fleet Uniform(int vanCount, double maxWeightKg, double maxVolumeM3) =>
        new(Enumerable.Repeat(new TruckCapacity(maxWeightKg, maxVolumeM3), vanCount));
}
