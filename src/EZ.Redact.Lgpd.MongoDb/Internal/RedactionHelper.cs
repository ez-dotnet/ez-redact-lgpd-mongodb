using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Core.Taxonomies;
using Microsoft.Extensions.Compliance.Classification;

namespace EZ.Redact.Lgpd.MongoDb.Internal;

internal sealed record RedactableProperty(DadoPessoal DadoPessoal, Func<object, object?> Getter, Action<object, object?> Setter);

internal static class RedactionHelper
{
    private static readonly ConcurrentDictionary<Type, RedactableProperty[]> _cache = new();

    public static void Redact(object document, ILGPDRedactService service)
    {
        if (document is null)
            return;

        var properties = _cache.GetOrAdd(document.GetType(), ResolveRedactableProperties);
        if (properties.Length == 0)
            return;

        foreach (var prop in properties)
        {
            if (prop.Getter(document) is string str)
            {
                var redacted = service.Redact(prop.DadoPessoal, str);
                if (redacted != str)
                    prop.Setter(document, redacted);
            }
        }
    }

    private static RedactableProperty[] ResolveRedactableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string))
            .Select(p => (Property: p, Attribute: p.GetCustomAttribute<DataClassificationAttribute>()))
            .Where(x => x.Attribute is not null && x.Attribute.Classification.TaxonomyName == "LGPD")
            .Select(x =>
            {
                var dadoPessoal = LGPDTaxonomy.ToDadoPessoal(x.Attribute!.Classification);
                return new RedactableProperty(dadoPessoal, CreateGetter(x.Property), CreateSetter(x.Property));
            })
            .ToArray();
    }

    private static Func<object, object?> CreateGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var expr = Expression.Lambda<Func<object, object?>>(
            Expression.Convert(
                Expression.Property(
                    Expression.Convert(instance, property.DeclaringType!),
                    property),
                typeof(object)),
            instance);
        return expr.Compile();
    }

    private static Action<object, object?> CreateSetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var expr = Expression.Lambda<Action<object, object?>>(
            Expression.Assign(
                Expression.Property(
                    Expression.Convert(instance, property.DeclaringType!),
                    property),
                Expression.Convert(value, property.PropertyType)),
            instance,
            value);
        return expr.Compile();
    }
}
