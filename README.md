# Witcher3StringEditor

A powerful tool for editing string resources in *The Witcher 3: Wild Hunt*, enabling modification of in-game text such as dialogues, quest descriptions, and UI elements.

## Features

- **String Editing**: Add, modify, or delete entries in Witcher 3 string files.
- **Automatic Backups**: Files are auto-backed up on save (stored in `%AppData%\Witcher3StringEditor\Backup`), with management options via the backup dialog.
- **File Compatibility & Handling**: Support for `.w3strings` (Witcher 3 native), `.csv`, and `.xlsx` formats, with drag-and-drop functionality for easy access.
- **Recent Files**: Quick access to recently opened files through the "Recent" dialog.
- **Localization**: Interface adapts to system language settings.
- **Translation Helper**: Built-in tool for localizing entries (batch support, 1,000-character limit per translation).
- **Update Checks**: Automatic checks for the latest version.
- **Game Integration**: Launch *The Witcher 3* directly (requires `witcher3.exe` path setup).
- **Search & Pagination**: Efficient entry discovery and handling of large files with paginated display.

## Installation

1. Download the latest release from the [Nexus Mods page](https://www.nexusmods.com/witcher3/mods/10032).
2. Extract the zip file to your desired location.
3. Run `Witcher3StringEditor.exe` to launch.

## Required External Dependencies (For End Users)

- **.NET 10 Desktop Runtime**  
  Required for the application to run. Download the matching architecture (x64/x86) from the [Microsoft Official Page](https://dotnet.microsoft.com/download/dotnet/10.0) (select "Desktop Runtime" for your system).

- **w3strings Encoder**  
  Required for `.w3strings` file encoding/decoding. Download from [Nexus Mods](https://www.nexusmods.com/witcher3/mods/1055) and specify its path during first-run setup.

## First-Run Setup

On initial launch, you’ll be prompted to:
1. Set the path to `w3strings.exe` (mandatory for `.w3strings` handling).
2. Optionally set the path to `witcher3.exe` for direct game launching.

## Usage

### Basic Operations
- **Open Files**: Use "Open", the "Recent" menu, or drag-and-drop (`.w3strings`/`.csv`/`.xlsx`). A prompt warns of unsaved changes.
- **Edit Entries**: Select entries to modify; use "Add" or "Delete" to manage.
- **Save Changes**: Click "Save" to persist edits (auto-backups created).
- **Manage Backups**: Access the backup dialog to view, restore, or delete backups (restoring overwrites with confirmation).

### Advanced Features
- **Translation Tool**: Select an entry and click "Translate". Note: Mode changes interrupt translations; overwrites require confirmation.
- **Settings**: Customize encoder path, game path, and preferred save format.
- **Log Viewer**: Check operation history with timestamps.
- **Nexus Mods Integration**: Click "Nexus Mods" to visit the [mod page](https://www.nexusmods.com/witcher3/mods/10032) for updates.

## Screenshots

![Main Window](https://staticdelivery.nexusmods.com/mods/952/images/10032/10032-1755524172-1319856400.png)  
*Main interface with string entries, search, and pagination*

![Backup Dialog](https://staticdelivery.nexusmods.com/mods/952/images/10032/10032-1739770257-1910199133.png)  
*Backup management with restore/delete options*

![Translation Tool](https://staticdelivery.nexusmods.com/mods/952/images/10032/10032-1755524172-877884616.png)  
*Built-in translation helper with source/target language support*

## License

Licensed under the MIT License – see the [LICENSE](LICENSE) file for details.

## Support

For issues, feature requests, or questions, visit the [Nexus Mods page](https://www.nexusmods.com/witcher3/mods/10032).