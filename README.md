# VSModPackager

---

[![GitHub license](https://badgen.net/github/license/SeanLatimer/VSModPackager)](https://github.com/SeanLatimer/VSModPackager/blob/main/LICENSE)
[![NuGet stable version](https://badgen.net/nuget/v/VSModPackager)](https://nuget.org/packages/VSModPackager)

This is an MSBuild task that simplifies packaging Vintage Story Mods.

Install the NuGet package `VSModPackager`. When you build in release mode, your mod will be packaged and placed in the
output directory with the name `{AssemblyName}-{ProductVersion}.zip` by default.

## Table of Contents

---

## Configuration

---

If you need to override the default task configuration, you can do so by creating a file called `VSModPackager.targets`
in your project and use the following as a template

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
    <Target Name="VSModPackager" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
        <VSModPackager ProjectDir="$(ProjectDir)"
                       OutputPath="$(OutputPath)"
                       ZipOutputPath="$(BaseOutputPath)$(Configuration)"
                       AssemblyName="$(AssemblyName)"/>
    </Target>
</Project>
```

## Mod Info Generation

---

VSModPackager reduces the amount of keys you need to include in your mod info, filling in the rest from sane defaults or
your assembly. You can of course specify everything manually.

```json
{
    "modid": "ModId",
    "name": "ModName",
    "description": "ModDesc",
    "type": "Code"
}
```

# YAML Mod Info

---

In addition, VSModPackager also allows you to use YAML, a more friendly format, for your mod info. YAML uses snake_case
for keys instead of PascalCase like JSON. All features of the packager work with YAML mod info, just use the `.yaml`
extension instead of `.json`.

```yaml
modid: "ModId"
name: "ModTemplate"
description: "ModTemplate"
type: "code"
```

## Zip file generation

---

VSModPackager will also create a release ready zip of your mod in `$(BaseOutputPath)$(Configuration)` by default. If you
do not override the default config, it will use `{AssemblyName}-{ProductVersion}.zip` as the file name.

Note that the entire contents of the `$(OutputPath)` will be included in the zip by default. You will need to disable
copying Vintage Story references or use `Exclude` or `Include` to change this.

## Task Attributes

---

| Attribute        | Description                                                                                                                                                                   | Required | Default                             |
|------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|-------------------------------------|
| `ProjectDir`     | This is the path where your csproj is located. You must set this to `$(ProjectDir)`.                                                                                          | **Yes**  | _None_ - set to `$(ProjectDir)`     |
| `OutputPath`     | This is the path that your assemblies are output to after build. You must set this to `$(OutputPath).`                                                                        | **Yes**  | _None_ - set to `$(OutputPath)`     |
| `AssemblyName`   | This is the name of the assembly that Vintage Story will load. You must set this to `$(AssemblyName).`                                                                        | **Yes**  | _None_ - set to `$(AssemblyName)`   |
| `ZipOutputPath`  | This is the path that the packaged zip will be placed.                                                                                                                        | No       | `$(BaseOutputPath)$(Configuration)` |
| `ZipName`        | This is the name of the output zip file.                                                                                                                                      | No       | `$(AssemblyName)`                   |
| `ModInfoType`    | You can choose between `auto`, `json`, and `yaml` for your manifest file. `auto` will use `json` first, then fall back on `yaml`.                                             | No       | `auto`                              |
| `Version`        | This is the version that will be placed inside the generated `modinfo.json`. By default, we load the assembly Product Version for this.                                       | No       | Assembly Product Version            |
| `VersionZipName` | Determines whether we append `Version` to `ZipName`.                                                                                                                          | No       | `true`                              |
| `Exclude`        | Files to exclude from the zip. Mutually exclusive with `Include`. Files should be separated by a semicolon (`;`) and be relative to `OutputPath`. Files do not need to exist. | No       | _None_                              |
| `Include`        | Files to include in the zip. Mutually exclusive with `Exclude`. Files should be separated by a semicolon (`;`) and be relative to `OutputPath`. Files must exist.             | No       | _None_                              |
