using Microsoft.EntityFrameworkCore;
using Stanley.Data;
using Stanley.Data.Views;
using Stanley.Importer;

// A one-off developer tool, not part of the deployed bot: it loads v1's fighters.json into the
// database once (FR-21, migration plan phase 3). Running it again changes nothing.
//
//   dotnet run --project tools/Stanley.Importer -- .old/fighters.json
//
// The connection string comes from STANLEY_DB_CONNECTION, or the second argument.

string path = args.Length > 0 ? args[0] : Path.Combine(".old", "fighters.json");

string connectionString =
    (args.Length > 1 ? args[1] : null)
    ?? Environment.GetEnvironmentVariable("STANLEY_DB_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=stanley;Username=stanley;Password=stanley";

if (!File.Exists(path))
{
    Console.Error.WriteLine($"No fighter file at '{Path.GetFullPath(path)}'.");
    return 1;
}

await using StanleyDbContext db = StanleyDbContextOptions.Create(connectionString);

Console.WriteLine("Applying any pending migrations...");
await db.Database.MigrateAsync();

Console.WriteLine($"Importing '{path}'...");
ImportSummary summary = await new FighterImporter(db).ImportAsync(path, CancellationToken.None);

Console.WriteLine();
Console.WriteLine($"  fighters in file     {summary.FightersInFile}");
Console.WriteLine($"  characters created   {summary.CharactersCreated}");
Console.WriteLine($"  fighters created     {summary.VariantsCreated}");
Console.WriteLine($"  abilities created    {summary.AbilitiesCreated}");
Console.WriteLine($"  abilities removed    {summary.AbilitiesRemoved}");
Console.WriteLine($"  rows written         {summary.RowsWritten}");

// The totals a reviewer checks against the database design, section 5.3. "Catalogue" counts
// every row; "collectible" leaves out the Raid Bosses, which is what the views return and what
// a command shows a player.
int characters = await db.Characters.CountAsync();
int catalogue = await db.Variants.CountAsync();
int raidBosses = await db.Variants.CountAsync(v => v.IsRaidBoss);
int abilities = await db.SignatureAbilities.CountAsync();
int aliases = await db.VariantAliases.CountAsync();
List<TierVariantCount> tierCounts = await db.TierVariantCounts.OrderBy(t => t.Id).ToListAsync();
List<ElementVariantCount> elementCounts =
    await db.ElementVariantCounts.OrderBy(e => e.Id).ToListAsync();

Console.WriteLine();
Console.WriteLine("Database now holds:");
Console.WriteLine($"  characters           {characters}");
Console.WriteLine($"  fighters             {catalogue} ({catalogue - raidBosses} collectible, "
    + $"{raidBosses} raid boss(es))");
Console.WriteLine($"  signature abilities  {abilities}");
Console.WriteLine($"  aliases              {aliases}");
Console.WriteLine("  collectible by tier  "
    + string.Join(", ", tierCounts.Select(t => $"{t.Name} {t.TotalVariants}")));
Console.WriteLine("  collectible by elem. "
    + string.Join(", ", elementCounts.Select(e => $"{e.Name} {e.TotalVariants}")));

string[] raidBossNames = await db.Variants
    .Where(v => v.IsRaidBoss)
    .OrderBy(v => v.Name)
    .Select(v => v.Name)
    .ToArrayAsync();

Console.WriteLine($"  raid bosses          {string.Join(", ", raidBossNames)}");

return 0;
