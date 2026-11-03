namespace Stanley.Core.Validation;

/// <summary>
/// What Stanley accepts as a web address an editor typed.
/// </summary>
/// <remarks>
/// One rule in one place: a portrait URL, a fighter's wiki URL override and a wiki page's URL
/// are all entered by hand into a Discord option, and all three are refused for the same
/// reason when they are not an address a user's client could follow.
/// </remarks>
public static class WebAddress
{
    /// <summary>
    /// Whether <paramref name="value"/> is an absolute <c>http</c> or <c>https</c> address.
    /// </summary>
    /// <remarks>
    /// Absolute, because a relative address means nothing in a Discord message, and the scheme
    /// is checked so <c>file:</c> or <c>javascript:</c> cannot be stored as a link.
    /// </remarks>
    public static bool IsAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
