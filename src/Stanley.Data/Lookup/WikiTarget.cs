namespace Stanley.Data.Lookup;

/// <summary>
/// What <c>/wiki</c> needs about a fighter: the two columns its URL depends on (FR-07).
/// </summary>
/// <param name="Name">The display name the URL is built from.</param>
/// <param name="StoredUrl">
/// The <c>variants.wiki_url</c> override, normally empty. Set only when a fighter's page title
/// does not match its name, in which case it wins over the built URL.
/// </param>
public sealed record WikiTarget(string Name, string? StoredUrl);
