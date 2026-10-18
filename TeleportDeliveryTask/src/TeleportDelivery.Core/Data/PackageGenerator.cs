using TeleportDelivery.Model;

namespace TeleportDelivery.Data;

/// <summary>Synthetic packages</summary>
public static class PackageGenerator
{
    public static Package[] Generate(int count, ScenarioType scenarioType, int seed)
    {
        var random = new Random(seed);
        var packages = new Package[count];
        for (int i = 0; i < count; i++)
        {
            // AlzaBox compartments limit the parcel size, so every parcel is tiny compared to a truck.
            double volume = Math.Clamp(LogNormal(random, median: 0.004, sigma: 0.9), 0.0003, 0.12);   // m³
            double density = scenarioType switch                                                          // kg/m³
            {
                ScenarioType.Typical => LogNormal(random, median: 170, sigma: 0.6),
                ScenarioType.Heavy => LogNormal(random, median: 1_000, sigma: 0.35),
                ScenarioType.Mixed => random.NextDouble() < 0.6
                    ? LogNormal(random, median: 1_400, sigma: 0.3)
                    : LogNormal(random, median: 90, sigma: 0.5),
                _ => throw new ArgumentOutOfRangeException(nameof(scenarioType)),
            };
            double weight = Math.Clamp(volume * density, 0.05, 40);                                  // kg

            // Bigger parcels earn more in absolute terms, but less per m³.
            double profit = LogNormal(random, median: 80, sigma: 0.8) * Math.Pow(volume / 0.004, 0.4); // CZK

            packages[i] = new Package(i, Math.Round(weight, 3), Math.Round(volume, 5), Math.Round(profit, 2));
        }
        return packages;
    }

    // Skewed random values (many small, few big) so the packages are realistic.
    private static double LogNormal(Random random, double median, double sigma) =>
        median * Math.Exp(sigma * StandardNormal(random));

    private static double StandardNormal(Random random)   // Box–Muller transform
    {
        double u1 = 1 - random.NextDouble(), u2 = random.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }
}
