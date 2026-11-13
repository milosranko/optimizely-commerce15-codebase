using PaypalServerSdk.Standard;
using PaypalServerSdk.Standard.Authentication;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal;

public static class PayPalSdkClientFactory
{
    public static PaypalServerSdkClient Create(PayPalOptions opt)
    {
        var env = opt.Environment.Equals("production", StringComparison.OrdinalIgnoreCase)
            ? PaypalServerSdk.Standard.Environment.Production
            : PaypalServerSdk.Standard.Environment.Sandbox;

        return new PaypalServerSdkClient.Builder()
            .ClientCredentialsAuth(
                new ClientCredentialsAuthModel.Builder(opt.ClientId, opt.ClientSecret).Build()
            )
            .Environment(env)
            .Build();
    }
}

