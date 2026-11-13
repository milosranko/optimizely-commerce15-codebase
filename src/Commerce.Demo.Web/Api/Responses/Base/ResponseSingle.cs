namespace Commerce.Demo.Web.Api.Responses.Base;

public record ResponseSingle<T> : ResponseBase where T : class
{
    public T? Result { get; init; }
}
