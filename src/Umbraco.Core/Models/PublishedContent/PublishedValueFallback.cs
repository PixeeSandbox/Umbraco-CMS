using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Extensions;

namespace Umbraco.Cms.Core.Models.PublishedContent;

/// <summary>
///     Provides a default implementation for <see cref="IPublishedValueFallback" />.
/// </summary>
public class PublishedValueFallback : IPublishedValueFallback
{
    private readonly ILocalizationService? _localizationService;
    private readonly IVariationContextAccessor _variationContextAccessor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PublishedValueFallback" /> class.
    /// </summary>
    public PublishedValueFallback(ServiceContext serviceContext, IVariationContextAccessor variationContextAccessor)
    {
        _localizationService = serviceContext.LocalizationService;
        _variationContextAccessor = variationContextAccessor;
    }

    /// <inheritdoc />
    public bool TryGetValue(IPublishedProperty property, string? culture, string? segment, Fallback fallback, object? defaultValue, out object? value) =>
        TryGetValue<object>(property, culture, segment, fallback, defaultValue, out value);

    /// <inheritdoc />
    public bool TryGetValue<T>(IPublishedProperty property, string? culture, string? segment, Fallback fallback, T? defaultValue, out T? value)
    {
        _variationContextAccessor.ContextualizeVariation(property.PropertyType.Variations, ref culture, ref segment);

        foreach (var f in fallback)
        {
            switch (f)
            {
                case Fallback.None:
                    continue;
                case Fallback.DefaultValue:
                    value = defaultValue;
                    return true;
                case Fallback.Language:
                    if (TryGetValueWithLanguageFallback(property, culture, segment, out value))
                    {
                        return true;
                    }

                    break;
                case Fallback.DefaultLanguage:
                    if (TryGetValueWithDefaultLanguageFallback(property, culture, segment, out value))
                    {
                        return true;
                    }

                    break;
                default:
                    throw NotSupportedFallbackMethod(f, "property");
            }
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetValue(IPublishedElement content, string alias, string? culture, string? segment, Fallback fallback, object? defaultValue, out object? value) =>
        TryGetValue<object>(content, alias, culture, segment, fallback, defaultValue, out value);

    /// <inheritdoc />
    public bool TryGetValue<T>(IPublishedElement content, string alias, string? culture, string? segment, Fallback fallback, T? defaultValue, out T? value)
    {
        IPublishedPropertyType? propertyType = content.ContentType.GetPropertyType(alias);
        if (propertyType == null)
        {
            value = default;
            return false;
        }

        _variationContextAccessor.ContextualizeVariation(propertyType.Variations, ref culture, ref segment);

        foreach (var f in fallback)
        {
            switch (f)
            {
                case Fallback.None:
                    continue;
                case Fallback.DefaultValue:
                    value = defaultValue;
                    return true;
                case Fallback.Language:
                    if (TryGetValueWithLanguageFallback(content, alias, culture, segment, out value))
                    {
                        return true;
                    }

                    break;
                case Fallback.DefaultLanguage:
                    if (TryGetValueWithDefaultLanguageFallback(content, alias, culture, segment, out value))
                    {
                        return true;
                    }

                    break;
                default:
                    throw NotSupportedFallbackMethod(f, "element");
            }
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetValue(IPublishedContent content, string alias, string? culture, string? segment, Fallback fallback, object? defaultValue, out object? value, out IPublishedProperty? noValueProperty) =>
        TryGetValue<object>(content, alias, culture, segment, fallback, defaultValue, out value, out noValueProperty);

    /// <inheritdoc />
    public virtual bool TryGetValue<T>(IPublishedContent content, string alias, string? culture, string? segment, Fallback fallback, T? defaultValue, out T? value, out IPublishedProperty? noValueProperty)
    {
        noValueProperty = default;

        IPublishedPropertyType? propertyType = content.ContentType.GetPropertyType(alias);
        if (propertyType != null)
        {
            _variationContextAccessor.ContextualizeVariation(propertyType.Variations, content.Id, ref culture, ref segment);
            noValueProperty = content.GetProperty(alias);
        }

        // note: we don't support "recurse & language" which would walk up the tree,
        // looking at languages at each level - should someone need it... they'll have
        // to implement it.
        foreach (var f in fallback)
        {
            switch (f)
            {
                case Fallback.None:
                    continue;
                case Fallback.DefaultValue:
                    value = defaultValue;
                    return true;
                case Fallback.Language:
                    if (propertyType == null)
                    {
                        continue;
                    }

                    if (TryGetValueWithLanguageFallback(content, alias, culture, segment, out value))
                    {
                        return true;
                    }

                    break;
                case Fallback.Ancestors:
                    if (TryGetValueWithAncestorsFallback(content, alias, culture, segment, out value, ref noValueProperty))
                    {
                        return true;
                    }

                    break;
                case Fallback.DefaultLanguage:
                    if (TryGetValueWithDefaultLanguageFallback(content, alias, culture, segment, out value))
                    {
                        return true;
                    }

                    break;
                default:
                    throw NotSupportedFallbackMethod(f, "content");
            }
        }

        value = default;
        return false;
    }

    private NotSupportedException NotSupportedFallbackMethod(int fallback, string level) =>
        new NotSupportedException(
            $"Fallback {GetType().Name} does not support fallback code '{fallback}' at {level} level.");

    // tries to get a value, recursing the tree
    // because we recurse, content may not even have the a property with the specified alias (but only some ancestor)
    // in case no value was found, noValueProperty contains the first property that was found (which does not have a value)
    private bool TryGetValueWithAncestorsFallback<T>(IPublishedContent? content, string alias, string? culture, string? segment, out T? value, ref IPublishedProperty? noValueProperty)
    {
        IPublishedProperty? property; // if we are here, content's property has no value
        do
        {
            content = content?.Parent<IPublishedContent>(StaticServiceProvider.Instance.GetRequiredService<IDocumentNavigationQueryService>(), StaticServiceProvider.Instance.GetRequiredService<IPublishedContentStatusFilteringService>());

            IPublishedPropertyType? propertyType = content?.ContentType.GetPropertyType(alias);

            if (propertyType != null && content is not null)
            {
                culture = null;
                segment = null;
                _variationContextAccessor.ContextualizeVariation(propertyType.Variations, content.Id, ref culture, ref segment);
            }

            property = content?.GetProperty(alias);
            if (property != null && noValueProperty == null)
            {
                noValueProperty = property;
            }
        }
        while (content != null && (property == null || property.HasValue(culture, segment) == false));

        // if we found a content with the property having a value, return that property value
        if (property != null && property.HasValue(culture, segment))
        {
            value = property.Value<T>(this, culture, segment);
            return true;
        }

        value = default;
        return false;
    }

    // tries to get a value, falling back onto other languages
    private bool TryGetValueWithLanguageFallback<T>(IPublishedProperty property, string? culture, string? segment, out T? value)
        => TryGetValueWithLanguageFallback(
            (actualCulture, actualSegment)
                => property.HasValue(actualCulture, actualSegment)
                    ? property.Value<T>(this, actualCulture, actualSegment)
                    : default,
            culture,
            segment,
            out value);

    // tries to get a value, falling back onto other language
    private bool TryGetValueWithLanguageFallback<T>(IPublishedElement content, string alias, string? culture, string? segment, out T? value)
        => TryGetValueWithLanguageFallback(
            (actualCulture, actualSegment)
                => content.HasValue(alias, actualCulture, actualSegment)
                    ? content.Value<T>(this, alias, actualCulture, actualSegment)
                    : default,
            culture,
            segment,
            out value);

    private bool TryGetValueWithLanguageFallback<T>(TryGetValueForCultureAndSegment<T> getValue, string? culture, string? segment, out T? value)
    {
        value = default;

        if (culture.IsNullOrWhiteSpace())
        {
            return false;
        }

        var visited = new HashSet<string>();

        ILanguage? language = culture is not null ? _localizationService?.GetLanguageByIsoCode(culture) : null;
        if (language == null)
        {
            return false;
        }

        while (true)
        {
            if (language.FallbackIsoCode == null)
            {
                return false;
            }

            var language2IsoCode = language.FallbackIsoCode;
            if (visited.Contains(language2IsoCode))
            {
                return false;
            }

            visited.Add(language2IsoCode);

            ILanguage? language2 = _localizationService?.GetLanguageByIsoCode(language2IsoCode);
            if (language2 == null)
            {
                return false;
            }

            var culture2 = language2.IsoCode;
            T? culture2Value = getValue(culture2, segment);
            if (culture2Value != null)
            {
                value = culture2Value;
                return true;
            }

            language = language2;
        }
    }

    private bool TryGetValueWithDefaultLanguageFallback<T>(IPublishedProperty property, string? culture, string? segment, out T? value)
    {
        value = default;

        if (culture.IsNullOrWhiteSpace())
        {
            return false;
        }

        string? defaultCulture = _localizationService?.GetDefaultLanguageIsoCode();
        if (culture.InvariantEquals(defaultCulture) == false && property.HasValue(defaultCulture, segment))
        {
            value = property.Value<T>(this, defaultCulture, segment);
            return true;
        }

        return false;
    }

    private bool TryGetValueWithDefaultLanguageFallback<T>(IPublishedElement element, string alias, string? culture, string? segment, out T? value)
    {
        value = default;

        if (culture.IsNullOrWhiteSpace())
        {
            return false;
        }

        string? defaultCulture = _localizationService?.GetDefaultLanguageIsoCode();
        if (culture.InvariantEquals(defaultCulture) == false && element.HasValue(alias, defaultCulture, segment))
        {
            value = element.Value<T>(this, alias, defaultCulture, segment);
            return true;
        }

        return false;
    }

    private delegate T? TryGetValueForCultureAndSegment<out T>(string actualCulture, string? actualSegment);
}
