﻿namespace Umbraco.Cms.Core.Models;

public class ContentPropertySettings
{
    private readonly HashSet<string> _reservedFieldNames = new();

    /// <summary>
    /// Gets a set of standard names for fields that cannot be used for custom properties.
    /// </summary>
    public ISet<string> ReservedFieldNames => _reservedFieldNames;

    public bool AddReservedFieldName(string name) => _reservedFieldNames.Add(name);

    public void AddReservedFieldNames(ISet<string> names) => _reservedFieldNames.UnionWith(names);
}
