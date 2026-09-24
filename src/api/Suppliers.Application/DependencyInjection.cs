using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Suppliers.Application.Suppliers.Create;
using Suppliers.Application.Suppliers.Extract;
using Suppliers.Application.Suppliers.GetById;
using Suppliers.Application.Suppliers.List;
using Suppliers.Application.Suppliers.Media;

namespace Suppliers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<CreateSupplierHandler>();
        services.AddScoped<GetSupplierByIdHandler>();
        services.AddScoped<ListSuppliersHandler>();
        services.AddScoped<ExtractSupplierDraftHandler>();
        services.AddScoped<RequestMediaUploadHandler>();
        services.AddScoped<ConfirmMediaUploadHandler>();
        services.AddScoped<DeleteSupplierMediaHandler>();
        services.AddScoped<GetMediaDownloadUrlHandler>();

        return services;
    }
}
