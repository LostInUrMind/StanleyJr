namespace Stanley.Data.Entities;

/// <summary>
/// An entity whose <c>updated_at</c> column is maintained by the application
/// (database design section 1.4). <see cref="StanleyDbContext"/> sets it on every save.
/// </summary>
public interface ITimestamped
{
    DateTime CreatedAt { get; set; }

    DateTime UpdatedAt { get; set; }
}
