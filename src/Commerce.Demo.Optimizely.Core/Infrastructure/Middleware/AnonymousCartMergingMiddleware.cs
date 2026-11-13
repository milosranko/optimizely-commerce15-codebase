using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Anonymous;
using Mediachase.Commerce.Markets;
using Microsoft.AspNetCore.Http;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.Core.Infrastructure.Middleware;

public class AnonymousCartMergingMiddleware
{
    private readonly RequestDelegate _next;

    public AnonymousCartMergingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity != null && context.User.Identity.IsAuthenticated && !string.IsNullOrEmpty(context.User.Identity.Name))
        {
            var anonymousId = context.Features.Get<IAnonymousIdFeature>()?.AnonymousId;

            if (!string.IsNullOrWhiteSpace(anonymousId))
            {
                var orderRepository = context.RequestServices.GetInstance<IOrderRepository>();
                var marketService = context.RequestServices.GetInstance<IMarketService>();

                var market = marketService.GetMarket(MarketId.Default);
                var cart = orderRepository.LoadCart<ICart>(new Guid(anonymousId), CartNames.ShoppingCart, market.MarketId);

                if (cart != null && cart.GetAllLineItems().ToList().Count > 0)
                {
                    var currentMarket = context.RequestServices.GetInstance<ICurrentMarket>();
                    cart.MarketId = currentMarket.GetCurrentMarket().MarketId;
                    orderRepository.Save(cart);

                    var profileMigrator = context.RequestServices.GetInstance<IProfileMigrator>();
                    profileMigrator.MigrateCarts(new Guid(anonymousId));
                }
            }
        }

        await _next(context);
    }
}
