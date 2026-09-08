# Driveclub PS4 Save Editor

Offline Windows save editor for decrypted **Driveclub PS4 v1.28 `profile.sav`** files.

The editor is title-ID neutral: it does not require CUSA00003 and accepts compatible regional saves that use the verified v1.28 profile format.

## Features

- Modern Windows Forms interface
- Driver Fame and calculated Driver Rank 1–120
- Garage editing with readable vehicle names and vehicle level controls
- Vehicle Fame/progression preservation for untouched entries
- Livery and customisation editing
- Elite progression reference data
- Accolade and challenge counters
- Editable Trophy Prep values
- Friendly statistics and an Advanced raw-data view
- Automatic backups when overwriting a save
- Driveclub checksum regeneration and structural validation
- Optional PayPal Donate button

## Save safety

The editor validates the Driveclub v1.28 save structure before writing and rebuilds variable-size components instead of relying on blind fixed offsets.

Verified format checks include:

- save version `65519`
- expected nine-component entity sequence
- component boundaries
- final `0x0000FFFF` marker
- first 8 bytes of MD5 over all data after the 12-byte save header

Untouched Garage progression is preserved exactly. If normal and Advanced pages attempt conflicting changes to the same backing value, saving is rejected instead of silently choosing one.

> **Always keep backups of your original save.** Although the editor creates backups when overwriting, testing modifications on a copy is recommended.

## Supported saves

The editor expects a **decrypted Driveclub v1.28 `profile.sav`**.

“All regions” means saves using the compatible v1.28 profile format. A physical save from every regional title ID has not been individually tested, so incompatible formats are rejected rather than guessed.

## Main sections

### Player

- Player Fame
- calculated Driver Rank
- target-rank Fame tools
- feature/menu flags
- recent car and bike selection

### Garage

- readable car and bike names
- vehicle Level 1–15 editing
- current vehicle Fame display
- Max Selected / Max All

### Customisation

- customisation Slot 1 / Slot 2
- livery designs
- four paint channels
- race number, badge and number font
- stickers / decals
- club shield, flourish and symbol layers
- mark confirmed new/unseen customisation items as seen

### Progression

- Elite progression reference
- Fame requirements
- Tour-star requirements (reference only)
- target times
- named tracks and vehicles
- editable accolade/challenge counters

### Trophy Prep

Mapped values can be edited directly with quick actions such as:

- Set 1 Below
- Set Requirement
- Restore Original
- Apply Value

Some counters have confirmed save locations but unknown in-game unit conversions; those remain raw numeric values rather than using an invented conversion.

### Advanced

Raw access to profile strings, profile floats, StatsStore, Rank, Fame, and StoreCatalog data.

## Build

### Requirements

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build with the included script

Run:

```text
build.bat
```

The executable will be created at:

```text
DriveclubSaveEditor\bin\Release\net8.0-windows\Driveclub PS4 Save Editor.exe
```

You can also build the project directly:

```powershell
dotnet build .\DriveclubSaveEditor\DriveclubSaveEditor.csproj -c Release
```

## Notes

- Per-event Tour/star save writing is intentionally not exposed until the persistent GameSession mapping is sufficiently verified.
- `AssetNewness` is treated as new/unseen state, not as proven livery or DLC ownership.
- The editor is intended for offline save editing; no online database is required.

## Disclaimer

Driveclub and related names/assets are property of their respective owners. This project is an unofficial fan-made save editor and is not affiliated with or endorsed by Sony Interactive Entertainment or Evolution Studios.
