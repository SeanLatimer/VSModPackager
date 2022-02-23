// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace VSModPackager;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// ReSharper disable once InconsistentNaming
/// <summary>
///     Main Task Class
/// </summary>
public class VSModPackager : Task
{
    /// <summary>
    ///     Set this to $(AssemblyName)
    /// </summary>
    [Required]
    public string AssemblyName { get; set; } = null!;

    /// <summary>
    ///     Set this to $(ProjectDir)
    /// </summary>
    [Required]
    public string ProjectDir { get; set; } = null!;

    /// <summary>
    ///     Set this to $(OutputPath)
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = null!;

    /// <summary>
    ///     This can be either "auto", "json", or "yaml"
    /// </summary>
    public string ModInfoType { get; set; } = "auto";

    public string? Version { get; set; }

    public bool MakeZip { get; set; }
    public bool VersionZipName { get; set; } = true;

    public string? Exclude { get; set; }

    public string? Include { get; set; }

    private Lazy<List<string>> ExcludeFiles => new(() => StringToList(Exclude));

    private Lazy<List<string>> IncludeFiles => new(() => StringToList(Include));

    private ModInfoKind RealModInfoType => ModInfoType switch
    {
        "auto" => ModInfoKind.Auto,
        "json" => ModInfoKind.Json,
        "yaml" => ModInfoKind.Yaml,
        _ => throw new ArgumentException("Invalid manifest type: expected either 'auto', 'json', or 'yaml'",
            nameof(ModInfoType))
    };

    private static string NormalisePath(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar);

    private static List<string> StringToList(string? s)
    {
        if (s == null)
        {
            return new List<string>();
        }

        return s.Split(';').Select(name => NormalisePath(name.Trim())).ToList();
    }

    public override bool Execute()
    {
        ProjectDir = NormalisePath(ProjectDir);
        OutputPath = NormalisePath(OutputPath);

        var modInfo = LoadModInfo();
        if (modInfo == null)
        {
            Log.LogError("Could not find mod info in project directory.");
            return false;
        }

        if (modInfo.LogMissing(Log))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            Version = LoadAssemblyVersion().ProductVersion;
        }

        modInfo.SetProperties(AssemblyName, Version!);

        SaveModInfo(modInfo);

        return !MakeZip || CreateZip();
    }

    private FileVersionInfo LoadAssemblyVersion()
    {
        var assemblyPath = Path.Combine(OutputPath, $"{AssemblyName}.dll");
        var fullPath = Path.GetFullPath(assemblyPath);

        return FileVersionInfo.GetVersionInfo(fullPath);
    }

    private void SaveModInfo(ModInfo modInfo)
    {
        var path = Path.Combine(OutputPath, "modinfo.json");
        var jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        });

        using var jsonFile = File.Open(path, FileMode.Create);
        using var jsonStream = new StreamWriter(jsonFile) { NewLine = "\n" };
        jsonSerializer.Serialize(jsonStream, modInfo);
    }

    public bool CreateZip()
    {
        var zipOutput = Path.Combine(OutputPath, AssemblyName);
        if (Directory.Exists(zipOutput))
        {
            Directory.Delete(zipOutput, true);
        }

        var includeLen = IncludeFiles.Value.Count;
        var excludeLen = ExcludeFiles.Value.Count;
        if (includeLen > 0 && excludeLen > 0)
        {
            Log.LogError("Specify either Include or Exclude on your VSModPackager task, not both.");
            return false;
        }

        string[] fileNames;
        if (includeLen == 0 && excludeLen == 0)
        {
            fileNames = Directory.EnumerateFiles(OutputPath, "*", SearchOption.AllDirectories)
                .Select(file => NormalisePath(file.Substring(OutputPath.Length + 1))).ToArray();
        }
        else if (includeLen > 0)
        {
            fileNames = IncludeFiles.Value.ToArray();
        }
        else
        {
            fileNames = Directory.EnumerateFiles(OutputPath, "*", SearchOption.AllDirectories)
                .Select(file => NormalisePath(file.Substring(OutputPath.Length + 1)))
                .Where(file => !ExcludeFiles.Value.Contains(file)).ToArray();
        }

        var zipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using (var zipFile = File.Create(zipPath))
        {
            using var zip = new ZipArchive(zipFile, ZipArchiveMode.Create);
            foreach (var file in fileNames)
            {
                var filePath = Path.Combine(OutputPath, file);
                _ = zip.CreateEntryFromFile(filePath, file);
            }
        }

        _ = Directory.CreateDirectory(zipOutput);
        File.Copy(
            Path.Combine(OutputPath, "modinfo.json"),
            Path.Combine(zipOutput, "modinfo.json")
        );

        var sb = new StringBuilder($"{AssemblyName}");
        if (VersionZipName)
        {
            _ = sb.Append('-').Append(Version);
        }

        _ = sb.Append(".zip");
        File.Move(
            zipPath,
            Path.Combine(zipOutput, sb.ToString())
        );

        return true;
    }

    private ModInfo? LoadModInfo()
    {
        // ReSharper disable once IdentifierTypo
        var exts = RealModInfoType switch
        {
            ModInfoKind.Auto => new[] { "json", "yaml" },
            ModInfoKind.Json => new[] { "json" },
            ModInfoKind.Yaml => new[] { "yaml" },
            _ => throw new ArgumentOutOfRangeException(nameof(RealModInfoType),
                $"extension doesn't exist for {RealModInfoType}")
        };

        foreach (var ext in exts)
        {
            var modInfoPath = Path.Combine(ProjectDir, $"modinfo.{ext}");
            if (!File.Exists(modInfoPath))
            {
                continue;
            }

            using var modInfoFile = File.Open(modInfoPath, FileMode.Open);
            using var modInfoReader = new StreamReader(modInfoFile);

            return ext switch
            {
                "json" => LoadJsonModInfo(modInfoReader, Log),
                "yaml" => LoadYamlModInfo(modInfoReader, Log),
#pragma warning disable CA2201
                _ => throw new Exception("unreachable")
#pragma warning restore CA2201
            };
        }

        return null;
    }

    private static ModInfo LoadYamlModInfo(TextReader reader, TaskLoggingHelper? log = null)
    {
        log?.LogMessage(MessageImportance.Normal, "Found yaml mod info");
        var yamlDeserializer = new DeserializerBuilder().IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        return yamlDeserializer.Deserialize<ModInfo>(reader);
    }

    private static ModInfo LoadJsonModInfo(TextReader reader, TaskLoggingHelper? log = null)
    {
        log?.LogMessage(MessageImportance.Normal, "Found json mod info");
        return JsonSerializer.CreateDefault().Deserialize<ModInfo>(new JsonTextReader(reader))!;
    }
}
