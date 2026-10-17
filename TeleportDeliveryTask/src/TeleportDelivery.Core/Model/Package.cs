namespace TeleportDelivery.Model;

public readonly record struct Package(int Id, double WeightKg, double VolumeM3, double Profit);
