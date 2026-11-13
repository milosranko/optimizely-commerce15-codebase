using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Interfaces;

public interface IAccordion : IContentData
{
    IList<DropDownLabelXhtmlProperty>? PrimaryAccordion { get; set; }
    IList<LabelXhtmlProperty>? SecondaryAccordion { get; set; }
}
