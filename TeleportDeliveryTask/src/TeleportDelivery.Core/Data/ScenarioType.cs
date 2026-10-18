namespace TeleportDelivery.Data;

/// <summary>Kind of day used to generate synthetic test data.</summary>
public enum ScenarioType
{
    /// <summary>Ordinary e-shop mix: parcels are light for their size, so volume is the bottleneck.</summary>
    Typical,

    /// <summary>Drinks, books, tools...: dense parcels, so weight is the bottleneck.</summary>
    Heavy,

    /// <summary>Very dense and very light parcels mixed: both limits bind at the same time.</summary>
    Mixed,
}
