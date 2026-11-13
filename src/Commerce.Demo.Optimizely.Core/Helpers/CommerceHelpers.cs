using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;

namespace Commerce.Demo.Optimizely.Core.Helpers;

public static class CommerceHelpers
{
    public static MetaClass GetOrCreateMetaClass(string metaClassName, string? metaClassFriendlyName = null)
    {
        var metaClass = DataContext.Current.MetaModel.MetaClasses
            .Cast<MetaClass>()
            .FirstOrDefault(mc => mc.Name == metaClassName) ?? CreateMetaClass(metaClassName, metaClassFriendlyName ?? metaClassName);

        return metaClass;
    }

    public static void CreateMetaFields(MetaClass metaClass, IEnumerable<MetaField> metaFields)
    {
        ArgumentNullException.ThrowIfNull(metaFields);

        if (!metaFields.Any())
            return;

        var fieldAdded = false;

        using var builder = new MetaFieldBuilder(metaClass);

        foreach (var field in metaFields)
        {
            if (metaClass.Fields.Contains(field.Name))
                continue;

            _ = metaClass.CreateMetaField(field);
            fieldAdded = true;
        }

        if (fieldAdded)
            builder.SaveChanges();
    }

    private static MetaClass CreateMetaClass(string metaClassName, string metaClassFriendlyName)
    {
        var metaClass = default(MetaClass);
        using (var scope = DataContext.Current.MetaModel.BeginEdit())
        {
            metaClass = DataContext.Current.MetaModel.CreateMetaClass(metaClassName, metaClassFriendlyName);
            scope.SaveChanges();
        }
        return metaClass;
    }
}
