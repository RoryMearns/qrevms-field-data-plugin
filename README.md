# QRevMS Field Data Plugin

An Aquarius Time-Series field data plugin that imports ADCP discharge summary XML measurements from
[QRevMS](https://www.genesishydrotech.com/qrevms).

## Want to install this plugin?

Download the [latest release here](https://github.com/RoryMearns/qrevms-field-data-plugin/releases/releases).

Install the `.plugin` file using the System Config page on your Aquarius Time-Series server.

## Requirements for building from source

- .NET Framework 4.7.2 targeting pack
- .NET SDK (any recent version) to run `dotnet build`/`dotnet test`

## Configuring the plugin

QRevMS XML is always metric internally - the plugin validates this and rejects anything else.
To store discharge activities in Imperial units instead, set a Group/Key/Value text setting on
the Settings page of the System Config app:

- **Group**: `FieldDataPluginConfig-QRevMS`
- **Key**: `Config`
- **Value**: the entire contents of a `Config.json` document (see reference file:
 `src/QRevMS/Configuration/Config.json`). Blank or omitted defaults to metric.

```json
{
  "ImperialUnits": false
}
```

## Building the plugin

```
dotnet build src/QRevMS.sln
```

Release builds automatically package `QRevMS.dll` into `QRevMS.plugin` at
`src/QRevMS/deploy/Release/QRevMS.plugin`.

## Testing the plugin

Run the unit test suite:

```
dotnet test tests/QRevMS.Tests/QRevMS.Tests.csproj
```

Or test against a real file with `PluginTester.exe`, included with the `Aquarius.FieldDataFramework`
NuGet package under `<nuget-packages-folder>/aquarius.fielddataframework/<version>/tools/`. After
building, run it from `src/QRevMS/bin/Debug/net472/`:

```
PluginTester.exe /Plugin=QRevMS.dll /Json=AppendedResults.json ^
  /Data=<path-to-file>.xml /Location=<location-identifier> /UtcOffset=<hh:mm:ss>
```

Sample QRevMS files are provided under `data/`.

## Known limitations

- `DeploymentMethod` is always `Unspecified`
- `VelocityObservationMethod` is left unset - no value in the enum correctly describes an ADCP
  (see the `TODO` in `DischargeSectionMapper.cs`)
- `IceCoveredData`'s thickness fields are left at zero
