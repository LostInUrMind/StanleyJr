namespace Stanley.Data.Wiki;

/// <summary>
/// The wiki page <c>/wiki-admin remove</c> deleted, so the confirmation can name what went.
/// </summary>
/// <remarks>
/// <see cref="WikiPageRepository.RemoveAsync"/> returns null when nothing matched, the same
/// shape every other lookup in the solution uses for "no match". Removal has no third outcome,
/// so it needs no result enum.
/// </remarks>
public sealed record RemovedWikiPage(string Key, string Url);
