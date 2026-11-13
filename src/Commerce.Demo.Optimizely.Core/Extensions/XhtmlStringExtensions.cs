using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.Web.Routing;
using System.Text;
using System.Text.RegularExpressions;

namespace Commerce.Demo.Optimizely.Core.Extensions;

public static class XhtmlStringExtensions
{
    /// <summary>
    /// Strip xhtml strings of all html elements
    /// </summary>
    /// <param name="xhtml"></param>
    /// <returns>A stripped string</returns>
    public static string StripXhtml(this XhtmlString xhtml)
    {
        if (xhtml == null) return string.Empty;

        return Regex.Replace(xhtml.ToString(), @"<(.|\n)*?>", string.Empty);
    }

    /// <summary>
    /// Strip xhtml of all html elements and return selected lengt of string
    /// </summary>
    /// <returns>A stripped and cropped string</returns>
    public static string StripXhtml(this XhtmlString xhtml, int length)
    {
        return xhtml.StripXhtml(length, false);
    }

    /// <summary>
    /// Strip xhtml of all html elements and return selected lengt of string
    /// </summary>
    /// <param name="xhtml"></param>
    /// <param name="length"></param>
    /// <param name="smartcrop">Crop string at blank space</param>
    /// <returns>A stripped and cropped string</returns>
    public static string StripXhtml(this XhtmlString xhtml, int length, bool smartcrop)
    {
        var plaintext = xhtml.StripXhtml();

        // Crop and return string            
        return plaintext.CropString(length, smartcrop);
    }

    public static string ToFriendlyUrlHtmlString(this XhtmlString instance)
    {
        if (instance is null || instance.IsEmpty)
            return string.Empty;

        var stringBuilder = new StringBuilder();

        foreach (IStringFragment stringFragment in instance.Fragments)
        {
            if (stringFragment is UrlFragment urlFragment)
            {
                if (TryGetExternalUrl(urlFragment.InternalFormat, out string externalUrl))
                {
                    stringBuilder.Append(externalUrl);
                    continue;
                }
            }

            stringBuilder.Append(stringFragment.GetViewFormat());
        }

        return stringBuilder.ToString();
    }

    private static bool TryGetExternalUrl(string internalFormat, out string externalUrl)
    {
        try
        {
            externalUrl = UrlResolver.Current.GetUrl(internalFormat);
        }
        catch (Exception)
        {
            externalUrl = string.Empty;
        }

        return !string.IsNullOrEmpty(externalUrl);
    }
}
