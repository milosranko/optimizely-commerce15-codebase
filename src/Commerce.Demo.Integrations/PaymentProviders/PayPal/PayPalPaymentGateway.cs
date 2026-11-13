using Commerce.Demo.Integrations.Abstractions.PayPal.Results;
using Commerce.Demo.Integrations.PaymentProviders.PayPal.Interfaces;
using Commerce.Demo.Integrations.PaymentProviders.PayPal.Mappings;
using Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;
using PaypalServerSdk.Standard;
using PaypalServerSdk.Standard.Models;
using System.Globalization;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal;

public sealed class PayPalPaymentGateway : IPayPalPaymentGateway
{
    private readonly PaypalServerSdkClient _client;
    private readonly PayPalOptions _opt;

    public PayPalPaymentGateway(PayPalOptions opt)
    {
        _opt = opt;
        _opt.Validate();

        _client = PayPalSdkClientFactory.Create(_opt);
    }

    public async Task<PayPalOperationResult<CreateOrderResult>> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            if (!TryValidateBreakdown(request, out var validationError))
            {
                return new PayPalOperationResult<CreateOrderResult>
                {
                    IsSuccess = false,
                    Errors = new List<PayPalError>
                    {
                        new PayPalError
                        {
                            ErrorCode = "InvalidAmountBreakdown",
                            ShortMessage = validationError,
                            LongMessage = validationError,
                            IsRetryable = false
                        }
                    }
                };
            }

            var order = await CreateSdkOrderAsync(request);

            var approvalUrl =
                order.Links?.FirstOrDefault(l => string.Equals(l.Rel, "approve", StringComparison.OrdinalIgnoreCase))?.Href
                ?? "";

            return new PayPalOperationResult<CreateOrderResult>
            {
                IsSuccess = true,
                RawStatus = order.Status?.ToString(),
                Value = new CreateOrderResult
                {
                    OrderId = order.Id ?? "",
                    ApprovalUrl = approvalUrl
                }
            };
        }
        catch (Exception ex)
        {
            return Failure<CreateOrderResult>(ex);
        }
    }

    public async Task<PayPalOperationResult<CaptureOrderResult>> CaptureOrderAsync(string orderId)
    {
        try
        {
            var order = await CaptureSdkOrderAsync(orderId);
            var captureId = ExtractFirstCaptureId(order);
            var captureStatusRaw = ExtractFirstCaptureStatus(order);

            return new PayPalOperationResult<CaptureOrderResult>
            {
                IsSuccess = true,
                RawStatus = order.Status?.ToString(),
                Value = new CaptureOrderResult
                {
                    Status = order.Status?.ToString() ?? "",
                    CaptureStatus = CaptureStatusMapper.Map(captureStatusRaw),
                    CaptureId = captureId
                }
            };
        }
        catch (Exception ex)
        {
            return Failure<CaptureOrderResult>(ex);
        }
    }

    public async Task<PayPalOperationResult<PayPalOrderDetails>> GetOrderDetailsAsync(string orderId)
    {
        try
        {
            var input = new GetOrderInput { Id = orderId };
            var resp = await _client.OrdersController.GetOrderAsync(input);
            var order = resp.Data;

            if (order == null)
            {
                return new PayPalOperationResult<PayPalOrderDetails>
                {
                    IsSuccess = false,
                    Errors = new List<PayPalError>
                    {
                        new PayPalError
                        {
                            ErrorCode = "OrderNotFound",
                            ShortMessage = "PayPal order not found.",
                            LongMessage = "GetOrderAsync returned null order.",
                            IsRetryable = false
                        }
                    }
                };
            }

            var details = OrderToDetailsMapper.Map(order);

            return new PayPalOperationResult<PayPalOrderDetails>
            {
                IsSuccess = true,
                RawStatus = order.Status?.ToString(),
                Value = details
            };
        }
        catch (Exception ex)
        {
            return Failure<PayPalOrderDetails>(ex);
        }
    }

    private async Task<Order> CreateSdkOrderAsync(CreateOrderRequest request)
    {
        // If caller provided any breakdown inputs, validate and apply them.
        // This mirrors the legacy behavior where item/shipping totals were explicitly set and relied upon.

        var orderRequest = new OrderRequest
        {
            Intent = CheckoutPaymentIntent.Capture,
            PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest
                {
                    Amount = new AmountWithBreakdown
                    {
                        CurrencyCode = request.CurrencyCode,
                        MValue = request.Total.ToString("F2", CultureInfo.InvariantCulture)
                    }
                }
            },
            ApplicationContext = new OrderApplicationContext
            {
                BrandName = request.BrandName,
                ReturnUrl = _opt.ReturnUrl,
                CancelUrl = _opt.CancelUrl,
                UserAction = OrderApplicationContextUserAction.PayNow
            }
        };

        // Apply optional breakdown fields (reflection-based to avoid tight coupling to SDK model shape).
        var purchaseUnit = orderRequest.PurchaseUnits.First();
        TryApplyAmountBreakdown(purchaseUnit.Amount, request);

        // Apply optional itemization (reflection-based to avoid tight coupling to SDK model shape).
        TryApplyItems(purchaseUnit, request);

        var input = new CreateOrderInput { Body = orderRequest };
        var resp = await _client.OrdersController.CreateOrderAsync(input);
        return resp.Data;
    }

    private async Task<Order> CaptureSdkOrderAsync(string orderId)
    {
        var input = new CaptureOrderInput { Id = orderId };
        var resp = await _client.OrdersController.CaptureOrderAsync(input);
        return resp.Data;
    }

    private static string? ExtractFirstCaptureId(Order? order)
    {
        return order?.PurchaseUnits?
            .FirstOrDefault()?
            .Payments?
            .Captures?
            .FirstOrDefault()?
            .Id;
    }

    private static string? ExtractFirstCaptureStatus(Order? order)
    {
        return order?.PurchaseUnits?
            .FirstOrDefault()?
            .Payments?
            .Captures?
            .FirstOrDefault()?
            .Status?
            .ToString();
    }

    private static PayPalOperationResult<T> Failure<T>(Exception ex)
    {
        var mapped = ExceptionToPayPalErrorsMapper.Map(ex);

        return new PayPalOperationResult<T>
        {
            IsSuccess = false,
            DebugId = mapped.DebugId,
            Errors = mapped.Errors
        };
    }

    private static bool TryValidateBreakdown(CreateOrderRequest request, out string error)
    {
        error = "";

        var hasAny = request.ItemTotal.HasValue || request.ShippingTotal.HasValue || request.HandlingTotal.HasValue ||
                     request.TaxTotal.HasValue || (request.AppliedDiscountLines != null && request.AppliedDiscountLines.Count > 0);

        if (!hasAny)
            return true;

        var shipping = request.ShippingTotal ?? 0m;
        var handling = request.HandlingTotal ?? 0m;
        var tax = request.TaxTotal ?? 0m;
        var discount = request.AppliedDiscountLines?.Sum(l => l.Amount) ?? 0m;

        // Legacy stored discount lines as positive amounts but applied as negative line items.
        // Here we treat discount as a positive reduction.
        var item = request.ItemTotal ?? (request.Total - shipping - handling - tax + discount);

        if (request.Items != null && request.Items.Count > 0)
        {
            var sumItems = Round2(request.Items.Sum(i => i.UnitAmount * i.Quantity));
            var itemRounded = Round2(item);

            if (sumItems != itemRounded)
            {
                error = $"Itemization does not reconcile with ItemTotal. ItemTotal={itemRounded:F2} but sum(items)={sumItems:F2}.";
                return false;
            }
        }

        var computed = Round2(item + shipping + handling + tax - discount);
        var total = Round2(request.Total);

        if (computed != total)
        {
            error = $"Breakdown does not reconcile. Total={total:F2} but components compute to {computed:F2} (Item={item:F2}, Shipping={shipping:F2}, Handling={handling:F2}, Tax={tax:F2}, Discount={discount:F2}).";
            return false;
        }

        // Guard: negative amounts are not meaningful here.
        if (item < 0m || shipping < 0m || handling < 0m || tax < 0m || discount < 0m)
        {
            error = "Breakdown contains a negative component (item/shipping/handling/tax/discount).";
            return false;
        }

        return true;
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static void TryApplyAmountBreakdown(AmountWithBreakdown sdkAmount, CreateOrderRequest request)
    {
        if (!TryValidateBreakdown(request, out _))
        {
            // Validation is enforced by rich method. Minimal method keeps old behavior.
            return;
        }

        var hasAny = request.ItemTotal.HasValue || request.ShippingTotal.HasValue || request.HandlingTotal.HasValue ||
                     request.TaxTotal.HasValue || (request.AppliedDiscountLines != null && request.AppliedDiscountLines.Count > 0);

        if (!hasAny)
            return;

        var shipping = request.ShippingTotal ?? 0m;
        var handling = request.HandlingTotal ?? 0m;
        var tax = request.TaxTotal ?? 0m;
        var discount = request.AppliedDiscountLines?.Sum(l => l.Amount) ?? 0m;
        var item = request.ItemTotal ?? (request.Total - shipping - handling - tax + discount);

        // Reflection-based application of breakdown to minimize SDK coupling.
        var breakdownProp = sdkAmount.GetType().GetProperty("Breakdown");
        if (breakdownProp == null)
            return;

        var breakdown = Activator.CreateInstance(breakdownProp.PropertyType);
        if (breakdown == null)
            return;

        SetMoneyProp(breakdown, "ItemTotal", request.CurrencyCode, item);
        SetMoneyProp(breakdown, "Shipping", "ShippingTotal", request.CurrencyCode, shipping);
        SetMoneyProp(breakdown, "Handling", "HandlingTotal", request.CurrencyCode, handling);
        SetMoneyProp(breakdown, "TaxTotal", "Tax", request.CurrencyCode, tax);
        SetMoneyProp(breakdown, "Discount", "DiscountTotal", request.CurrencyCode, discount);

        breakdownProp.SetValue(sdkAmount, breakdown);
    }

    private static void TryApplyItems(PurchaseUnitRequest purchaseUnit, CreateOrderRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return;

        // NOTE: We do NOT convert AppliedDiscountLines into negative line items here.
        // In Orders v2, discounts are better represented as a breakdown discount value.

        var itemsProp = purchaseUnit.GetType().GetProperty("Items");
        if (itemsProp == null)
            return;

        var listType = itemsProp.PropertyType;
        if (!listType.IsGenericType)
            return;

        var itemType = listType.GetGenericArguments()[0];

        // Create List<TItem>
        var list = (System.Collections.IList?)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
        if (list == null)
            return;

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                continue;
            if (item.Quantity <= 0)
                continue;
            if (item.UnitAmount < 0m)
                continue;

            var sdkItem = Activator.CreateInstance(itemType);
            if (sdkItem == null)
                continue;

            sdkItem.GetType().GetProperty("Name")?.SetValue(sdkItem, item.Name);
            sdkItem.GetType().GetProperty("Description")?.SetValue(sdkItem, item.Description);
            sdkItem.GetType().GetProperty("Sku")?.SetValue(sdkItem, item.Sku);
            sdkItem.GetType().GetProperty("Url")?.SetValue(sdkItem, item.Url);
            sdkItem.GetType().GetProperty("Quantity")?.SetValue(sdkItem, item.Quantity.ToString(CultureInfo.InvariantCulture));

            // category is commonly an enum in the SDK; try string first then enum parse.
            var categoryProp = sdkItem.GetType().GetProperty("Category");
            if (categoryProp != null)
            {
                if (categoryProp.PropertyType == typeof(string))
                {
                    categoryProp.SetValue(sdkItem, item.Category);
                }
                else if (categoryProp.PropertyType.IsEnum)
                {
                    try
                    {
                        var enumValue = Enum.Parse(categoryProp.PropertyType, item.Category, ignoreCase: true);
                        categoryProp.SetValue(sdkItem, enumValue);
                    }
                    catch
                    {
                        // Ignore if category cannot be parsed
                    }
                }
            }

            // unit_amount is typically a Money type with currency_code + value
            var unitAmountProp = sdkItem.GetType().GetProperty("UnitAmount");
            if (unitAmountProp != null)
            {
                var money = Activator.CreateInstance(unitAmountProp.PropertyType);
                if (money != null)
                {
                    money.GetType().GetProperty("CurrencyCode")?.SetValue(money, request.CurrencyCode);
                    money.GetType().GetProperty("MValue")?.SetValue(money, item.UnitAmount.ToString("F2", CultureInfo.InvariantCulture));
                    money.GetType().GetProperty("Value")?.SetValue(money, item.UnitAmount.ToString("F2", CultureInfo.InvariantCulture));
                    unitAmountProp.SetValue(sdkItem, money);
                }
            }

            list.Add(sdkItem);
        }

        itemsProp.SetValue(purchaseUnit, list);
    }

    private static void SetMoneyProp(object parent, string propName, string currencyCode, decimal amount)
        => SetMoneyProp(parent, new[] { propName }, currencyCode, amount);

    private static void SetMoneyProp(object parent, string propName1, string propName2, string currencyCode, decimal amount)
        => SetMoneyProp(parent, new[] { propName1, propName2 }, currencyCode, amount);

    private static void SetMoneyProp(object parent, string propName1, string propName2, string propName3, string currencyCode, decimal amount)
        => SetMoneyProp(parent, new[] { propName1, propName2, propName3 }, currencyCode, amount);

    private static void SetMoneyProp(object parent, string[] propNames, string currencyCode, decimal amount)
    {
        var parentType = parent.GetType();
        System.Reflection.PropertyInfo? prop = null;
        foreach (var n in propNames)
        {
            prop = parentType.GetProperty(n);
            if (prop != null)
                break;
        }

        if (prop == null)
            return;

        var money = Activator.CreateInstance(prop.PropertyType);
        if (money == null)
            return;

        // Common SDK money property names
        money.GetType().GetProperty("CurrencyCode")?.SetValue(money, currencyCode);
        money.GetType().GetProperty("MValue")?.SetValue(money, amount.ToString("F2", CultureInfo.InvariantCulture));
        money.GetType().GetProperty("Value")?.SetValue(money, amount.ToString("F2", CultureInfo.InvariantCulture));

        prop.SetValue(parent, money);
    }
}