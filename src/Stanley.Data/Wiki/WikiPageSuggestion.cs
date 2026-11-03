namespace Stanley.Data.Wiki;

/// <summary>
/// One autocomplete choice that is a wiki page: offered by <c>/wiki</c> beside the fighters,
/// and on its own by <c>/wiki-admin edit</c> and <c>/wiki-admin remove</c>.
/// </summary>
/// <param name="Key">The key as an editor typed it, for the choice's label.</param>
/// <param name="KeySlug">
/// What is sent back when the choice is picked. The slug rather than the key, so the command
/// matches on <c>key_slug</c> exactly however the label is punctuated.
/// </param>
/// <param name="Url">
/// The page's address. Shown to editors, so two keys pointing at different pages can be told
/// apart in the list.
/// </param>
public sealed record WikiPageSuggestion(string Key, string KeySlug, string Url);
