using Google.Cloud.Firestore;

namespace Infrastructure.Data.Firestore;

[FirestoreData]
public sealed class UserDocument
{
    [FirestoreProperty]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ExternalAuthId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Role { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? UpdatedAt { get; set; }
        = null;

    [FirestoreProperty]
    public bool IsDeleted { get; set; }
        = false;

    [FirestoreProperty]
    public DateTime? DeletedAt { get; set; }
        = null;
}
