using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Cms.Api.Delivery.Controllers.Content;

[ApiVersion("2.0")]
public class ByRouteContentApiController : ContentApiItemControllerBase
{
    private readonly IApiContentPathResolver _apiContentPathResolver;
    private readonly IRequestRedirectService _requestRedirectService;
    private readonly IRequestPreviewService _requestPreviewService;
    private readonly IRequestMemberAccessService _requestMemberAccessService;
    private const string PreviewContentRequestPathPrefix = $"/{Constants.DeliveryApi.Routing.PreviewContentPathPrefix}";

    public ByRouteContentApiController(
        IApiPublishedContentCache apiPublishedContentCache,
        IApiContentResponseBuilder apiContentResponseBuilder,
        IRequestRedirectService requestRedirectService,
        IRequestPreviewService requestPreviewService,
        IRequestMemberAccessService requestMemberAccessService,
        IApiContentPathResolver apiContentPathResolver)
        : base(apiPublishedContentCache, apiContentResponseBuilder)
    {
        _requestRedirectService = requestRedirectService;
        _requestPreviewService = requestPreviewService;
        _requestMemberAccessService = requestMemberAccessService;
        _apiContentPathResolver = apiContentPathResolver;
    }

    /// <summary>
    ///     Gets a content item by route.
    /// </summary>
    /// <param name="path">The path to the content item.</param>
    /// <remarks>
    ///     Optional URL segment for the root content item
    ///     can be added through the "start-item" header.
    /// </remarks>
    /// <returns>The content item or not found result.</returns>
    [HttpGet("item/{*path}")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(IApiContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ByRouteV20(string path = "")
        => await HandleRequest(path);

    private async Task<IActionResult> HandleRequest(string path)
    {
        path = DecodePath(path);
        path = path.Length == 0 ? "/" : path;

        IPublishedContent? contentItem = GetContent(path);
        if (contentItem is not null)
        {
            IActionResult? deniedAccessResult = await HandleMemberAccessAsync(contentItem, _requestMemberAccessService);
            if (deniedAccessResult is not null)
            {
                return deniedAccessResult;
            }

            return Ok(ApiContentResponseBuilder.Build(contentItem));
        }

        IApiContentRoute? redirectRoute = _requestRedirectService.GetRedirectRoute(path);
        return redirectRoute != null
            ? RedirectTo(redirectRoute)
            : NotFound();
    }

    private IPublishedContent? GetContent(string path)
        => path.StartsWith(PreviewContentRequestPathPrefix)
            ? GetPreviewContent(path)
            : GetPublishedContent(path);

    private IPublishedContent? GetPublishedContent(string path)
        => _apiContentPathResolver.ResolveContentPath(path);

    private IPublishedContent? GetPreviewContent(string path)
    {
        if (_requestPreviewService.IsPreview() is false)
        {
            return null;
        }

        if (Guid.TryParse(path.AsSpan(PreviewContentRequestPathPrefix.Length).TrimEnd("/"), out Guid contentId) is false)
        {
            return null;
        }

        IPublishedContent? contentItem = ApiPublishedContentCache.GetById(contentId);
        return contentItem;
    }

    private IActionResult RedirectTo(IApiContentRoute redirectRoute)
    {
        Response.Headers.Add("Location-Start-Item-Path", redirectRoute.StartItem.Path);
        Response.Headers.Add("Location-Start-Item-Id", redirectRoute.StartItem.Id.ToString("D"));
        return RedirectPermanent(redirectRoute.Path);
    }
}
