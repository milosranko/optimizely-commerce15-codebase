namespace Commerce.Demo.Web.Api.Responses.Base;

public abstract record ResponseBase
{
    public IEnumerable<string>? Errors { get; init; }
}
