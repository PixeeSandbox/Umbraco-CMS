﻿namespace Umbraco.Cms.Api.Management.ViewModels.Webhook.Logs;

public class WebhookLogResponseModel
{
    public Guid Key { get; set; }

    public Guid WebhookKey { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public bool IsSuccessStatusCode { get; set; }

    public DateTime Date { get; set; }

    public string EventAlias { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int RetryCount { get; set; }

    public string RequestHeaders { get; set; } = string.Empty;

    public string RequestBody { get; set; } = string.Empty;

    public string ResponseHeaders { get; set; } = string.Empty;

    public string ResponseBody { get; set; } = string.Empty;

    public bool ExceptionOccured { get; set; }
}
