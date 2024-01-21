using System.Text.Json.Serialization;

namespace Core.Email.Abstractions;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CoreEmailNotificationType
{
    Unknown,
    Bounce,
    Complaint,
    Delivery
}