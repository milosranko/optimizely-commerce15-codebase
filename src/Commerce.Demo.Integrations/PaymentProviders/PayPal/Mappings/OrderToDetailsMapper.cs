using Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;
using PaypalServerSdk.Standard.Models;
using System.Globalization;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Mappings;

public static class OrderToDetailsMapper
{
    public static PayPalOrderDetails Map(Order order)
    {
        var payerGivenName = order.Payer?.Name?.GivenName;
        var payerSurname = order.Payer?.Name?.Surname;
        var payerName = string.Join(" ", new[] { payerGivenName, payerSurname }.Where(s => !string.IsNullOrWhiteSpace(s)));

        object? firstPurchaseUnit = order.PurchaseUnits?.FirstOrDefault();

        var amountBreakdown = MapAmountBreakdown(firstPurchaseUnit);
        var shippingAddress = MapShippingAddress(firstPurchaseUnit);
        var payments = MapPayments(firstPurchaseUnit);

        // High-level sketch; fill in fields that are present in the SDK Order structure
        return new PayPalOrderDetails
        {
            OrderId = order.Id ?? "",
            Status = OrderStatusMapper.Map(order.Status?.ToString()),

            PayerInfo = new PayPalPayerInfo
            {
                PayerId = order.Payer?.PayerId,
                Email = order.Payer?.EmailAddress,
                PayerName = string.IsNullOrWhiteSpace(payerName) ? null : payerName,

                // In Orders v2 the shipping address is typically under purchase_units[0].shipping.address
                Address = shippingAddress
            },

            Amount = amountBreakdown,
            Payments = payments
        };
    }

    private static PayPalAmountBreakdown? MapAmountBreakdown(object? purchaseUnit)
    {
        // Orders v2 typically returns purchase_units[0].amount with optional breakdown.
        var amountObj = GetProp(purchaseUnit, "Amount");
        if (amountObj == null)
            return null;

        var currencyCode = GetStringProp(amountObj, "CurrencyCode") ?? "EUR";
        var orderTotalStr = GetStringProp(amountObj, "Value", "MValue");
        var orderTotalMoney = ParseMoney(currencyCode, orderTotalStr);

        var breakdownObj = GetProp(amountObj, "Breakdown");

        // PayPal naming in Orders v2 breakdown is commonly: item_total, shipping, handling, tax_total, discount
        // SDK property names vary; use reflection + multiple candidates.
        var itemTotal = ParseMoney(currencyCode, GetStringProp(GetProp(breakdownObj, "ItemTotal", "ItemTotal"), "Value", "MValue"));
        var shippingTotal = ParseMoney(currencyCode, GetStringProp(GetProp(breakdownObj, "Shipping", "ShippingTotal"), "Value", "MValue"));
        var handlingTotal = ParseMoney(currencyCode, GetStringProp(GetProp(breakdownObj, "Handling", "HandlingTotal"), "Value", "MValue"));
        var taxTotal = ParseMoney(currencyCode, GetStringProp(GetProp(breakdownObj, "TaxTotal", "Tax", "TaxTotal"), "Value", "MValue"));
        var discountTotal = ParseMoney(currencyCode, GetStringProp(GetProp(breakdownObj, "Discount", "DiscountTotal"), "Value", "MValue"));

        return new PayPalAmountBreakdown
        {
            OrderTotal = orderTotalMoney ?? new PayPalMoney { CurrencyCode = currencyCode, Value = 0m },
            ItemTotal = itemTotal,
            ShippingTotal = shippingTotal,
            HandlingTotal = handlingTotal,
            TaxTotal = taxTotal,
            DiscountTotal = discountTotal
        };
    }

    private static PayPalAddress? MapShippingAddress(object? purchaseUnit)
    {
        var shippingObj = GetProp(purchaseUnit, "Shipping");
        if (shippingObj == null)
            return null;

        var nameObj = GetProp(shippingObj, "Name");
        var fullName = GetStringProp(nameObj, "FullName", "Name");

        var addressObj = GetProp(shippingObj, "Address");
        if (addressObj == null)
            return string.IsNullOrWhiteSpace(fullName) ? null : new PayPalAddress { Name = fullName };

        return new PayPalAddress
        {
            Name = fullName,
            Street1 = GetStringProp(addressObj, "AddressLine1", "Street1", "Line1"),
            Street2 = GetStringProp(addressObj, "AddressLine2", "Street2", "Line2"),
            City = GetStringProp(addressObj, "AdminArea2", "City", "CityName"),
            StateOrProvince = GetStringProp(addressObj, "AdminArea1", "State", "StateOrProvince"),
            PostalCode = GetStringProp(addressObj, "PostalCode"),
            CountryCode = GetStringProp(addressObj, "CountryCode", "Country"),
            Phone = GetStringProp(addressObj, "Phone", "PhoneNumber")
        };
    }

    private static List<PayPalPaymentInfo> MapPayments(object? purchaseUnit)
    {
        // Capture information is typically at purchase_units[0].payments.captures[]
        var paymentsObj = GetProp(purchaseUnit, "Payments");
        var capturesObj = GetProp(paymentsObj, "Captures");
        if (capturesObj is not System.Collections.IEnumerable captures)
            return new List<PayPalPaymentInfo>();

        var result = new List<PayPalPaymentInfo>();
        foreach (var capture in captures)
        {
            if (capture == null)
                continue;

            result.Add(new PayPalPaymentInfo
            {
                TransactionId = GetStringProp(capture, "Id"),
                PaymentStatus = GetStringProp(capture, "Status"),
                CaptureStatus = CaptureStatusMapper.Map(GetStringProp(capture, "Status")),
                PaymentDate = GetStringProp(capture, "CreateTime", "UpdateTime"),
                PaymentError = null
            });
        }

        return result;
    }

    private static object? GetProp(object? obj, params string[] names)
    {
        if (obj == null)
            return null;

        var type = obj.GetType();
        foreach (var name in names)
        {
            var prop = type.GetProperty(name);
            if (prop == null)
                continue;

            return prop.GetValue(obj);
        }

        return null;
    }

    private static string? GetStringProp(object? obj, params string[] names)
    {
        var value = GetProp(obj, names);
        return value?.ToString();
    }

    private static PayPalMoney? ParseMoney(string currencyCode, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return null;

        return new PayPalMoney { CurrencyCode = currencyCode, Value = amount };
    }
}