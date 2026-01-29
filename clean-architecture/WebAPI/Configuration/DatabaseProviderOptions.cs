namespace WebAPI.Configuration;

public static class DatabaseProviderNames
{
    public const string InMemory = "InMemory";
    public const string Postgres = "Postgres";
    public const string Firestore = "Firestore";
}

public sealed class DatabaseProviderOptions
{
    public const string SectionName = "Database";

    public string? Provider { get; init; }

    public string? ConnectionString { get; init; }

    public string? FirestoreProjectId { get; init; }
}
