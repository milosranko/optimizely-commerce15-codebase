using EPiServer.Core;

namespace Commerce.Demo.Models.ViewModels;

public record ModalViewModel(Type Caller, string Heading, string LeadText, XhtmlString MainBody, ContentReference Image);
