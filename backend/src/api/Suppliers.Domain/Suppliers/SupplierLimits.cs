namespace Suppliers.Domain.Suppliers;

/// <summary>Business limits shared by the domain, validators and persistence mappings.</summary>
public static class SupplierLimits
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int ContactMaxLength = 200;
    public const int PhoneMaxLength = 50;
    public const int WebsiteMaxLength = 500;
    public const int AddressLineMaxLength = 300;
    public const int CityMaxLength = 100;
    public const int CountryMaxLength = 100;
    public const int CurrencyLength = 3;
    public const int MaxServicesPerSupplier = 50;

    public const int MaxMediaPerSupplier = 20;
    public const int FileNameMaxLength = 255;
    public const long MaxImageBytes = 10L * 1024 * 1024;
    public const long MaxVideoBytes = 100L * 1024 * 1024;

    public static long MaxMediaBytes(MediaKind kind) => kind == MediaKind.Video ? MaxVideoBytes : MaxImageBytes;
}
