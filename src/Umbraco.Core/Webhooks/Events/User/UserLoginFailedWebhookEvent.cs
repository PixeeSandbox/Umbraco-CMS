using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Cms.Core.Webhooks.Events;

[WebhookEvent("User Login Failed")]
public class UserLoginFailedWebhookEvent : WebhookEventBase<UserLoginFailedNotification>
{
    public UserLoginFailedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebhookService webHookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor)
        : base(webhookFiringService, webHookService, webhookSettings, serverRoleAccessor)
    {
    }

    public override string Alias => Constants.WebhookEvents.Aliases.UserLoginFailed;

    public override object? ConvertNotificationToRequestPayload(UserLoginFailedNotification notification)
        => new DefaultPayloadModel
        {
            Id = notification.AffectedUserId is not null &&
                 Guid.TryParse(notification.AffectedUserId, out Guid affectedUserGuid)
                ? affectedUserGuid
                : Guid.Empty,
        };
}
