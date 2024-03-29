﻿namespace Core.Email.Abstractions;

[Serializable]
public class CoreEmailStatus
{
    public Guid Id { get; set; }
    public string? ProviderMessageId { get; set; }
    public bool IsSuccess { get; set; }
    public string Error { get; set; } = string.Empty;
}