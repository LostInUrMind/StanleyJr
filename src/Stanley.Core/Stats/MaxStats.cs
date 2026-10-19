namespace Stanley.Core.Stats;

/// <summary>
/// A fighter's stats at maximum level. Never stored; computed per request
/// (database design section 6.2).
/// </summary>
public readonly record struct MaxStats(int Atk, int Hp, int Fs);
