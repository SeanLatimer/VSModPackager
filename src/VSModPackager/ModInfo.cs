namespace VSModPackager;

using System.Collections.Generic;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class ModInfo
{
    [JsonConverter(typeof(StringEnumConverter))]
    public ModType? Type { get; set; }

    public string? ModId { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? NetworkVersion { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string[]? Authors { get; set; }
    public string[]? Contributors { get; set; }
    public int? TextureSize { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public Side? Side { get; set; }

    public bool? RequiredOnClient { get; set; }
    public bool? RequiredOnServer { get; set; }
    public Dictionary<string, string>? Dependencies { get; set; }

    internal bool LogMissing(TaskLoggingHelper log)
    {
        var anyNull = Type == null || string.IsNullOrWhiteSpace(Name);
        if (Name == null)
        {
            log.LogError("Mod name is required in your modinfo.");
        }

        if (Type == null)
        {
            log.LogError("Mod type is required in your modinfo.");
        }

        return anyNull;
    }

    internal void SetProperties(string assemblyName, string assemblyVersion)
    {
        if (string.IsNullOrWhiteSpace(ModId))
        {
            ModId = assemblyName;
        }

        Version = assemblyVersion;
    }
}

public enum ModType
{
    Theme = 0,
    Content = 1,
    Code = 2
}

public enum Side
{
    Universal = 0,
    Client = 1,
    Server = 2
}
