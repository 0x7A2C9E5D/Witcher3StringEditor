# Witcher3StringEditor

A powerful tool for editing string resources in *The Witcher 3: Wild Hunt*, enabling modification of in-game text such
as dialogues, quest descriptions, and UI elements.

## Features

- **String Editing**: Add, modify, or delete entries in Witcher 3 string files.
- **Automatic Backups**: Files are auto-backed up on save (stored in `%AppData%\Witcher3StringEditor\Backup`), with
  management options via the backup dialog.
- **File Compatibility & Handling**: Support for `.w3strings` (Witcher 3 native), `.csv`, and `.xlsx` formats, with
  drag-and-drop functionality for easy access.
- **Built-in `.w3strings` Codec**: `.w3strings` files are decoded and encoded in-process — no external encoder tool is
  needed. Containers of the UTF-8 generation the current game ships (`v164`) are fully supported, and the older
  generation (`v162`/`v163`) is decoded as well, including the community containers that store UTF-8 while declaring
  one of those versions: those are read as UTF-8, a warning is written to the log, and saving such a container writes
  the version its contents belong to, so re-saving one fixes its declaration. A container that declares more text than
  the file holds is read up to what is there, with a warning, and the entries whose text is missing are dropped. Saves
  are otherwise written as `v162` or `v164`, whichever is selected in the save dialog.
- **Recent Files**: Quick access to recently opened files through the "Recent" dialog.
- **Localization**: The interface is available in ten languages (English, German, French, Hungarian, Japanese, Polish,
  Brazilian Portuguese, Thai, Simplified Chinese and Traditional Chinese). The system language picks one on first run,
  and the settings can change it.
- **Translation Helper**: Built-in tool for localizing entries, one at a time or in batch, through Microsoft, Google or
  Yandex (chosen in the settings).
- **Term Dictionaries**: Reuse your own word lists while translating (Microsoft Translator with an English source
  language): `.txt` dictionaries live in `%AppData%\Witcher3StringEditor\Dictionaries` and are managed from the
  dictionary dialog.
- **Update Checks**: Automatic check for a newer release on the Nexus Mods page.
- **Game Integration**: Launch *The Witcher 3* directly (requires `witcher3.exe` path setup).
- **Search & Pagination**: Efficient entry discovery and handling of large files with paginated display.

## Installation

1. Download the latest release from the [Nexus Mods page](https://www.nexusmods.com/witcher3/mods/10032).
2. Extract the zip file to your desired location.
3. Run `Witcher3StringEditor.exe` to launch.
4. To upgrade later, replace the extracted files. Your settings, logs, backups and dictionaries are kept, because they
   live in `%AppData%\Witcher3StringEditor` instead of the application folder.

## Required External Dependencies (For End Users)

- **.NET 10 Desktop Runtime**  
  Required for the application to run. Download the matching architecture (x64/x86) from
  the [Microsoft Official Page](https://dotnet.microsoft.com/download/dotnet/10.0) (select "Desktop Runtime" for your
  system).

## First-Run Setup

`.w3strings` handling needs no setup — the codec ships with the application. Optionally set the path to `witcher3.exe`
in the settings to launch the game directly.

## Usage

### Basic Operations

- **Open Files**: Use "Open", the "Recent" menu, or drag-and-drop (`.w3strings`/`.csv`/`.xlsx`). A prompt warns of
  unsaved changes.
- **Edit Entries**: Select entries to modify; use "Add" or "Delete" to manage.
- **Save Changes**: Click "Save" to persist edits (auto-backups created).
- **Manage Backups**: Access the backup dialog to view, restore, or delete backups (restoring overwrites with
  confirmation).

### Bulk Translation with CSV or Excel

Entries can be exported to `.csv` or `.xlsx` from the save dialog and opened again later, which is how a translation
round usually runs:

- **CSV**: every line is `id|key(hex)|key(str)|text`, introduced by `;` comment lines that are skipped on import. A text
  may contain the `|` separator itself — everything after the third field counts as the text — and it is read back
  exactly as written, spaces included. A text that spans several lines cannot survive a CSV round trip, so use Excel
  for those.
- **Excel**: the workbook keeps the columns `StrId`, `KeyHex`, `KeyName`, `OldText` and `Text`. Only `Text` becomes the
  entry text on import; `OldText` is there for the translator to compare against and is ignored.
- Saving the result back to `.w3strings` is what puts the texts into the game. The container version and the payload
  encoding are chosen in the save dialog.

### Advanced Features

- **Translation Tool**: Select an entry and click "Translate". Note: Mode changes interrupt translations; overwrites
  require confirmation.
- **Settings**: Customize the interface language, game path, translator, page size, preferred save format, and the
  `.w3strings` container version written on save.
- **Log Viewer**: Check operation history with timestamps.
- **Nexus Mods Integration**: Click "Nexus Mods" to visit the [mod page](https://www.nexusmods.com/witcher3/mods/10032)
  for updates.

## Screenshots

![Main Window](https://staticdelivery.nexusmods.com/mods/952/images/10032/10032-1755524172-1319856400.png)  
*Main interface with string entries, search, and pagination*

![Backup Dialog](https://staticdelivery.nexusmods.com/mods/952/images/10032/10032-1739770257-1910199133.png)  
*Backup management with restore/delete options*

![Translation Tool](https://staticdelivery.nexusmods.com/mods/952/images/10032/10032-1755524172-877884616.png)  
*Built-in translation helper with source/target language support*

## Building from Source

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Put a Syncfusion licence key in `Witcher3StringEditor\Syncfusion.lic`. The file is not part of the repository while
   the build embeds it as a resource and the application registers it at startup, so a fresh clone cannot build
   without it.
3. Build with `dotnet build Witcher3StringEditor\Witcher3StringEditor.csproj -c Release`, or produce a
   framework-dependent folder with `dotnet publish Witcher3StringEditor\Witcher3StringEditor.csproj -c Release`.

## License

Licensed under the MIT License – see the [LICENSE](LICENSE) file for details. The application bundles third-party
components (among them Syncfusion UI and XlsIO, iNKORE UI, GTranslate and Serilog) that stay under their own licences.

## Support

For issues, feature requests, or questions, visit the [Nexus Mods page](https://www.nexusmods.com/witcher3/mods/10032).