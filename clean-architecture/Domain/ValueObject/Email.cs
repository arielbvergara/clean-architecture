using System.Text.RegularExpressions;

namespace Domain.ValueObject;

public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    
    public string Value { get; }
    
    private Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));
        
        var normalizedEmail = value.Trim().ToLowerInvariant();
        
        if (normalizedEmail.Length > 254)
            throw new ArgumentException("Email cannot exceed 254 characters", nameof(value));
            
        if (!EmailRegex.IsMatch(normalizedEmail))
            throw new ArgumentException("Email format is invalid", nameof(value));
            
        Value = normalizedEmail;
    }
    
    public static Email Create(string value) => new(value);
}