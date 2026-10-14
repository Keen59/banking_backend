namespace NotificationService.Application.DTOs.Notifications;

public class NotificationDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public Guid SourceId { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
