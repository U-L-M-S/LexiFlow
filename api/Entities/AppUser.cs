using System;
using System.Collections.Generic;

namespace LexiFlow.Api.Entities;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
