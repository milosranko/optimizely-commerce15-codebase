namespace Commerce.Demo.Business.Features.BackgroundJobs.Models;

public record SendOrderPayload(string OrderNumber, int OrderGroupId);
