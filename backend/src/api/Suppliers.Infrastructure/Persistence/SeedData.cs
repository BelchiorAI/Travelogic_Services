using Microsoft.EntityFrameworkCore;
using Suppliers.Domain.Suppliers;

namespace Suppliers.Infrastructure.Persistence;

/// <summary>Realistic South African suppliers, inserted only when the table is empty.</summary>
internal static class SeedData
{
    public static void Seed(DbContext context)
    {
        var suppliers = context.Set<Supplier>();
        if (suppliers.Any())
            return;

        suppliers.AddRange(Build(DateTimeOffset.UtcNow));
        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var suppliers = context.Set<Supplier>();
        if (await suppliers.AnyAsync(cancellationToken))
            return;

        suppliers.AddRange(Build(DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<Supplier> Build(DateTimeOffset now)
    {
        var lodge = Supplier.Create(
            "Marula Bush Lodge", SupplierType.Accommodation, "Hazyview", "South Africa", now,
            description: "Private game lodge on the western boundary of the Kruger National Park.",
            contactName: "Thandi Nkosi", email: "reservations@marulabush.example", phone: "+27 13 555 0101",
            website: "https://marulabush.example", addressLine: "R536, Sabie Road");
        lodge.AddService("Luxury Safari Suite", ServiceType.Accommodation, 6800m, "ZAR", PricingUnit.PerPersonPerNight,
            description: "Full board, includes two game drives per day.", capacity: 2);
        lodge.AddService("Sunrise Game Drive", ServiceType.Activity, 950m, "ZAR", PricingUnit.PerPerson,
            durationMinutes: 180, capacity: 9);
        lodge.AddService("Guided Bush Walk", ServiceType.Activity, 650m, "ZAR", PricingUnit.PerPerson,
            durationMinutes: 150, capacity: 8);
        yield return lodge;

        var hotel = Supplier.Create(
            "Harbourview Waterfront Hotel", SupplierType.Accommodation, "Cape Town", "South Africa", now,
            description: "Four-star hotel at the V&A Waterfront with views of Table Mountain.",
            contactName: "Pieter van der Merwe", email: "groups@harbourview.example", phone: "+27 21 555 0202",
            addressLine: "12 Dock Road, V&A Waterfront");
        hotel.AddService("Deluxe Mountain-View Room", ServiceType.Accommodation, 3200m, "ZAR", PricingUnit.PerRoomPerNight,
            description: "Bed and breakfast, sleeps two.", capacity: 2);
        hotel.AddService("Family Suite", ServiceType.Accommodation, 5400m, "ZAR", PricingUnit.PerRoomPerNight,
            capacity: 4);
        yield return hotel;

        var sharkCage = Supplier.Create(
            "Gansbaai Shark Cage Adventures", SupplierType.Activity, "Gansbaai", "South Africa", now,
            description: "Great white shark cage diving in Shark Alley, with marine biologist guides.",
            contactName: "Lisa Botha", email: "dive@gansbaaishark.example", phone: "+27 28 555 0303",
            website: "https://gansbaaishark.example");
        sharkCage.AddService("Shark Cage Dive", ServiceType.Activity, 2800m, "ZAR", PricingUnit.PerPerson,
            description: "Includes breakfast, wetsuit and a light lunch.", durationMinutes: 300, capacity: 30);
        sharkCage.AddService("Cape Town Return Transfer", ServiceType.Transfer, 750m, "ZAR", PricingUnit.PerPerson,
            durationMinutes: 150);
        yield return sharkCage;

        var transfers = Supplier.Create(
            "Cape Airport Shuttle Co", SupplierType.Transport, "Cape Town", "South Africa", now,
            description: "Scheduled and private transfers from Cape Town International Airport.",
            contactName: "Sipho Dlamini", email: "ops@capeshuttle.example", phone: "+27 21 555 0404");
        transfers.AddService("Private Sedan Transfer (CPT to City Bowl)", ServiceType.Transfer, 650m, "ZAR", PricingUnit.PerVehicle,
            durationMinutes: 30, capacity: 3);
        transfers.AddService("Minibus Transfer (CPT to Winelands)", ServiceType.Transfer, 1800m, "ZAR", PricingUnit.PerVehicle,
            durationMinutes: 60, capacity: 12);
        yield return transfers;

        var wineEstate = Supplier.Create(
            "Jonkershoek Wine Estate", SupplierType.Restaurant, "Stellenbosch", "South Africa", now,
            description: "Historic wine farm with a cellar-door tasting room and a farm-to-table restaurant.",
            contactName: "Annelie du Toit", email: "events@jonkershoek.example", phone: "+27 21 555 0505",
            website: "https://jonkershoek.example");
        wineEstate.AddService("Premium Wine Tasting", ServiceType.Activity, 350m, "ZAR", PricingUnit.PerPerson,
            durationMinutes: 60, capacity: 20);
        wineEstate.AddService("Three-Course Harvest Lunch", ServiceType.Meal, 695m, "ZAR", PricingUnit.PerPerson,
            capacity: 60);
        yield return wineEstate;

        var adventure = Supplier.Create(
            "Tsitsikamma Edge Adventures", SupplierType.Activity, "Storms River", "South Africa", now,
            description: "Garden Route adventure operator: canopy tours, kloofing and the Bloukrans bungee.",
            contactName: "Jason Adams", email: "bookings@tsitsiedge.example", phone: "+27 42 555 0606");
        adventure.AddService("Forest Canopy Tour", ServiceType.Tour, 695m, "ZAR", PricingUnit.PerPerson,
            durationMinutes: 150, capacity: 12);
        adventure.AddService("Kloofing Expedition", ServiceType.Tour, 850m, "ZAR", PricingUnit.PerPerson,
            durationMinutes: 240, capacity: 10);
        adventure.AddService("Private Group Day Package", ServiceType.Tour, 9500m, "ZAR", PricingUnit.PerGroup,
            description: "Canopy tour, lunch and kloofing for up to 10 people.", durationMinutes: 480, capacity: 10);
        yield return adventure;
    }
}
