using System.Text.Json;
using Amazon;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

namespace Core.Email.Provider.SES;

internal class SimpleEmailServiceProvider : ICoreEmailProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Options _options = new();
    private readonly ICoreEmailPersistence? _persistence;

    private readonly AmazonSimpleEmailServiceV2Client _ses;


    public SimpleEmailServiceProvider(IConfiguration configuration, [ServiceKey] string key,
        IServiceProvider serviceProvider)
    {
        configuration.Bind($"Email:{key}", _options);
        _ses = new AmazonSimpleEmailServiceV2Client(_options.AccessKey, _options.SecretAccessKey,
            RegionEndpoint.GetBySystemName(_options.Region ?? "eu-central-1"));

        _persistence = serviceProvider.GetService<ICoreEmailPersistence>();
    }

    public string Name => "SES";

    public long MaxSize => 40 * 1024 * 1024;

    public async Task<List<CoreEmailStatus>> SendBatchAsync(List<CoreEmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var list = new List<CoreEmailStatus>();

        foreach (var message in messages)
            try
            {
                var m = new MimeMessage();
                m.From.Add(new MailboxAddress("", message.From));

                if (!string.IsNullOrEmpty(message.ReplyTo))
                    m.ReplyTo.Add(new MailboxAddress(string.Empty, message.ReplyTo));

                foreach (var to in message.To)
                    m.To.Add(new MailboxAddress(string.Empty, to));

                foreach (var cc in message.Cc)
                    m.Cc.Add(new MailboxAddress(string.Empty, cc));

                foreach (var bcc in message.Bcc)
                    m.Bcc.Add(new MailboxAddress(string.Empty, bcc));

                m.Subject = message.Subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = message.HtmlBody,
                    TextBody = message.TextBody
                };

                foreach (var attachment in message.Attachments)
                    bodyBuilder.Attachments.Add(attachment.Name, attachment.Content,
                        ContentType.Parse(attachment.ContentType));

                m.Body = bodyBuilder.ToMessageBody();

                using var stream = new MemoryStream();
                await m.WriteToAsync(stream, cancellationToken);
                stream.Position = 0;

                var res = await _ses.SendEmailAsync(new SendEmailRequest
                {
                    FromEmailAddress = message.From,
                    Destination = new Destination
                    {
                        ToAddresses = message.To,
                        CcAddresses = message.Cc,
                        BccAddresses = message.Bcc
                    },
                    Content = new EmailContent
                    {
                        Raw = new RawMessage
                        {
                            Data = stream
                        }
                    },
                    ReplyToAddresses = string.IsNullOrEmpty(message.ReplyTo) ? new List<string>() : [message.ReplyTo]
                }, cancellationToken).ConfigureAwait(false);

                list.Add(new CoreEmailStatus
                {
                    Id = message.Id,
                    ProviderMessageId = res.MessageId,
                    IsSuccess = (int)res.HttpStatusCode >= 200 && (int)res.HttpStatusCode < 300,
                    Error = string.Empty // TODO: ?
                });
            }
            catch (Exception e)
            {
                list.Add(new CoreEmailStatus
                {
                    Id = message.Id,
                    IsSuccess = false,
                    Error = e.Message
                });
            }

        return list;
    }

    public async Task GetNotificationsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.QueueUrl) || _persistence == null)
            return;

        var sqs = new AmazonSQSClient(_options.AccessKey, _options.SecretAccessKey,
            RegionEndpoint.GetBySystemName(_options.Region ?? "eu-central-1"));

        while (!cancellationToken.IsCancellationRequested)
            try
            {
                var messages = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    MaxNumberOfMessages = 10,
                    VisibilityTimeout = 60,
                    QueueUrl = _options.QueueUrl,
                    WaitTimeSeconds = 30
                }, cancellationToken).ConfigureAwait(false);

                await _persistence.StoreNotificationBatchAsync(messages.Messages.Select(x =>
                {
                    var body = JsonSerializer.Deserialize<Notification>(x.Body, JsonOptions);

                    return new CoreEmailNotification
                    {
                        ProviderMessageId = body?.Mail?.MessageId ?? string.Empty,
                        Type = body?.NotificationType switch
                        {
                            "Bounce" => CoreEmailNotificationType.Bounce,
                            "Complaint" => CoreEmailNotificationType.Complaint,
                            "Delivery" => CoreEmailNotificationType.Delivery,
                            _ => CoreEmailNotificationType.Unknown
                        },
                        Recipients = body?.NotificationType switch
                        {
                            "Bounce" => body.Bounce?.BouncedRecipients.Select(y => y.EmailAddress).ToList(),
                            "Complaint" => body.Complaint?.ComplainedRecipients.Select(y => y.EmailAddress).ToList(),
                            "Delivery" => body.Delivery?.Recipients.ToList(),
                            _ => null
                        } ?? [],
                        Timestamp = body?.NotificationType switch
                        {
                            "Bounce" => body.Bounce?.Timestamp ?? DateTimeOffset.UtcNow,
                            "Complaint" => body.Complaint?.Timestamp ?? DateTimeOffset.UtcNow,
                            "Delivery" => body.Delivery?.Timestamp ?? DateTimeOffset.UtcNow,
                            _ => DateTimeOffset.UtcNow
                        }
                    };
                }).ToList(), CancellationToken.None);

                await sqs.DeleteMessageBatchAsync(new DeleteMessageBatchRequest
                {
                    QueueUrl = _options.QueueUrl,
                    Entries = messages.Messages
                        .Select(x => new DeleteMessageBatchRequestEntry(x.MessageId, x.ReceiptHandle))
                        .ToList()
                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // TODO:
            }
    }

    [Serializable]
    private class Options
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string? Region { get; set; }
        public string? QueueUrl { get; set; }
    }

    [Serializable]
    private class Notification
    {
        public string NotificationType { get; set; } = string.Empty;
        public BounceNotification? Bounce { get; set; }
        public ComplaintNotification? Complaint { get; set; }
        public DeliveryNotification? Delivery { get; set; }
        public MailNotification? Mail { get; set; }
    }

    [Serializable]
    private class MailNotification
    {
        public string MessageId { get; set; } = string.Empty;

        public DateTimeOffset Timestamp { get; set; }
    }

    [Serializable]
    private class BounceEmail
    {
        public string EmailAddress { get; set; } = string.Empty;
    }

    [Serializable]
    private class BounceNotification
    {
        public string BounceType { get; set; } = string.Empty;
        public string BounceSubType { get; set; } = string.Empty;

        public List<BounceEmail> BouncedRecipients { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; }
    }

    [Serializable]
    private class ComplaintNotification
    {
        public string ComplaintFeedbackType { get; set; } = string.Empty;
        public List<BounceEmail> ComplainedRecipients { get; set; } = new();
        public DateTimeOffset ArrivalDate { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    [Serializable]
    private class DeliveryNotification
    {
        public List<string> Recipients { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; }
    }
}