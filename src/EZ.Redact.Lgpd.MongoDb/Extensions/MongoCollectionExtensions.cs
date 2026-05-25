using EZ.Redact.Lgpd.MongoDb.Collections;
using MongoDB.Driver;

namespace EZ.Redact.Lgpd.MongoDb;

public static class MongoCollectionExtensions
{
    public static IMongoCollection<T> UseRedaction<T>(this IMongoCollection<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var service = MongoDbRedactionContext.Service
            ?? throw new InvalidOperationException(
                "AddMongoDbRedaction() must be called during service registration. " +
                "Ensure builder.Services.AddLGPDRedaction().AddMongoDbRedaction() is invoked.");

        return new LgpdMongoCollection<T>(collection, service);
    }
}
