using Commerce.Demo.Business.Features.CookieConsentAudit.Enums;
using Commerce.Demo.Business.Features.CookieConsentAudit.Interfaces;
using Commerce.Demo.Business.Features.CookieConsentAudit.Models;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Enums;
using Commerce.Demo.Web.Api.Requests;
using Commerce.Demo.Web.Api.Responses;
using Commerce.Demo.Web.Api.Responses.Base;
using Commerce.Demo.Web.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Commerce.Demo.Web.Api;

public static class Endpoints
{
    private const int MaxActionLength = 20;
    private const int MaxCategoriesLength = 256;
    private const int MaxCookieNameLength = 128;
    private const int MaxPolicyVersionLength = 64;

    public static IEndpointRouteBuilder AddApiEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api");
        group.MapGet("/subpage", Get)
            .Produces<ResponseList<SubPage>>();
        group.MapGet("/subpage/{id}", GetById)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ResponseSingle<SubPage>>();
        group.MapPost("/post", Post)
            .Produces(StatusCodes.Status201Created);
        //.RequireAuthorization();

        group.MapPost("/consent/audit", RecordConsent)
            .RequireRateLimiting("consent-audit")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status429TooManyRequests);

        return builder;
    }

    private static IResult Post([FromBody] PostRequest request, HttpContext context)
    {
        return Results.Created(request.Id.ToString(), request);
    }

    private static IResult Get(IContentLoader contentLoader, IApiService apiService, HttpContext context)
    {
        var subPages = contentLoader
            .GetChildren<PageData>(ContentReference.StartPage, CultureInfo.GetCultureInfo("sr"))
            .Select(x => new SubPage { Name = x.Name });
        var response = new ResponseList<SubPage>
        {
            Results = subPages
        };

        return Results.Ok(response);
    }

    private static IResult GetById(int id, IContentLoader contentLoader, HttpContext context)
    {
        var subPage = contentLoader
            .GetChildren<PageData>(ContentReference.StartPage, CultureInfo.GetCultureInfo("sr"))
            .FirstOrDefault();
        var response = new ResponseSingle<SubPage>
        {
            Result = new SubPage { Name = subPage?.Name }
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> RecordConsent(
        [FromBody] RecordConsentRequest request,
        ICookieConsentAuditService auditService,
        HttpContext context,
        ILogger<RecordConsentRequest> logger)
    {
        if (!IsValidConsentRequest(request))
            return Results.BadRequest("Invalid consent request.");

        try
        {
            var ipAddress = AnonymizeIp(GetClientIpAddress(context));
            var userAgent = context.Request.Headers.UserAgent.ToString();
            string? visitorId = null;
            var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

            if (isAuthenticated)
            {
                visitorId = context.User.Identity?.Name;
            }
            else
            {
                visitorId = context.Request.Cookies
                    .FirstOrDefault(c => c.Key.Contains("Anonymous", StringComparison.OrdinalIgnoreCase)).Value;
            }

            await auditService.RecordConsent(request, visitorId, isAuthenticated, ipAddress, userAgent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record cookie consent audit");
        }

        return Results.Ok();
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out StringValues forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ip))
                return ip.Split(',', StringSplitOptions.TrimEntries).First();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static bool IsValidConsentRequest(RecordConsentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Action) || string.IsNullOrWhiteSpace(request.CategoriesSelected))
            return false;

        if (request.Action.Length > MaxActionLength ||
            request.CategoriesSelected.Length > MaxCategoriesLength ||
            request.CookieName?.Length > MaxCookieNameLength ||
            request.PolicyVersion?.Length > MaxPolicyVersionLength)
            return false;

        if (!Enum.TryParse<ConsentAction>(request.Action, ignoreCase: true, out _))
            return false;

        // Categories must be "false" (deny) or a pipe-delimited list of known cookie usage categories.
        foreach (var token in request.CategoriesSelected.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.Equals("false", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Enum.TryParse<SnippetUsage>(token, ignoreCase: true, out _))
                return false;
        }

        return true;
    }

    // Anonymize the client IP before storage (GDPR data minimization): drop the last IPv4 octet / last 80 bits of IPv6.
    private static string? AnonymizeIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip) || !IPAddress.TryParse(ip, out var address))
            return null;

        var bytes = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            bytes[3] = 0;
        }
        else if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            for (var i = 6; i < bytes.Length; i++)
                bytes[i] = 0;
        }

        return new IPAddress(bytes).ToString();
    }
}
