namespace WMS.Domain.Constants;

public static class LocationTypes
{
    public const string Zone = "Zone";
    public const string Aisle = "Aisle";
    public const string Rack = "Rack";
    public const string Bin = "Bin";

    public static bool IsValid(string? type) => type is Zone or Aisle or Rack or Bin;

    public static string? RequiredParentType(string type) => type switch
    {
        Zone => null,
        Aisle => Zone,
        Rack => Aisle,
        Bin => Rack,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
