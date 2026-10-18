namespace TeleportDelivery.Pricing;

/// <summary>
/// Shadow prices of fleet capacity: how much profit one more kilogram or one more cubic metre
/// of space would bring on this trip. They convert both limits (kg, m³) into one currency (CZK).
/// </summary>
/// <param name="PerKg">Value of 1 kg of capacity in CZK (0 when weight is not a bottleneck).</param>
/// <param name="PerM3">Value of 1 m³ of capacity in CZK (0 when volume is not a bottleneck).</param>
/// <param name="UpperBound">Profit that no loading plan can exceed (CZK).</param>
public readonly record struct CapacityPrices(double PerKg, double PerM3, double UpperBound);
