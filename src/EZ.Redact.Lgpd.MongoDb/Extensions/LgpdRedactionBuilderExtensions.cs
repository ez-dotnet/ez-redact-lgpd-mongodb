using EZ.Redact.Lgpd.Core;
using Microsoft.Extensions.DependencyInjection;

namespace EZ.Redact.Lgpd.MongoDb;

internal static class MongoDbRedactionContext
{
    internal static ILGPDRedactService? Service { get; set; }
}

public static class LgpdRedactionBuilderExtensions
{
    public static ILGPDRedactionBuilder AddMongoDbRedaction(this ILGPDRedactionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<ILGPDRedactService>();
        MongoDbRedactionContext.Service = service;

        return builder;
    }
}
