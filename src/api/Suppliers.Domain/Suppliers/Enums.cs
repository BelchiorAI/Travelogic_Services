namespace Suppliers.Domain.Suppliers;

// Names are part of the API contract: they travel as strings and are stored as strings.

public enum SupplierType
{
    Accommodation,
    Activity,
    Transport,
    Restaurant,
    Other,
}

public enum ServiceType
{
    Accommodation,
    Activity,
    Tour,
    Transfer,
    Meal,
    Other,
}

public enum PricingUnit
{
    PerPerson,
    PerPersonPerNight,
    PerRoomPerNight,
    PerVehicle,
    PerGroup,
}
