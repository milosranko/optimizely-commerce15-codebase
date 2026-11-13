using Commerce.Demo.Optimizely.Core.Extensions;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.Core.Attributes.Validations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class AllowUniqueBlockTypesAttribute : ValidationAttribute
{
    private Type[] _allowedTypes;
    private string _typeName = string.Empty;

    public AllowUniqueBlockTypesAttribute(params Type[] allowedTypes)
    {
        _allowedTypes = allowedTypes;
    }

    public override bool IsValid(object? value)
    {
        return ValidateContentArea(value as ContentArea);
    }

    public override string FormatErrorMessage(string name)
    {
        return string.IsNullOrEmpty(_typeName) ?
            LocalizationService.Current.GetString("/validations/allowuniqueblocktypesonly") :
            string.Format(LocalizationService.Current.GetString("/validations/allowuniqueblocktypes"), _typeName);
    }

    private bool ValidateContentArea(ContentArea? contentArea)
    {
        if (contentArea?.Items == null || !contentArea.Items.Any() || contentArea.Items.Count == 1)
            return true;

        return HasDuplicates(contentArea.Items);
    }

    private bool HasDuplicates(IList<ContentAreaItem> contentAreaItems)
    {
        if (_allowedTypes == null || _allowedTypes.Length == 0)
        {
            var blockTypes = new HashSet<int>();

            foreach (var item in contentAreaItems)
            {
                if (blockTypes.Contains(item.LoadContent().ContentTypeID))
                {
                    return false;
                }

                blockTypes.Add(item.LoadContent().ContentTypeID);
            }
        }
        else
        {
            foreach (var type in _allowedTypes)
            {
                var blockItems = contentAreaItems.Select(x => x.ContentLink.GetBlock<BlockData>());
                if (blockItems.Count(x => x != null && x.GetType().IsSubclassOf(type)) > 1)
                {
                    _typeName = type.Name;
                    return false;
                }
            }
        }

        return true;
    }
}
