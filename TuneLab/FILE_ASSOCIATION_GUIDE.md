# TuneLab File Association Guide

This document explains how TuneLab associates `.tlp` (TuneLab Project) files across different platforms.

## Icons

- **Application Icon**: `Assets/app.ico` (Windows), `Assets/app.icns` (macOS), `Assets/app.png` (Linux)
- **Project File Icon**: `Assets/file.ico` (Windows), `Assets/file.icns` (macOS), `Assets/file.png` (Linux)

## File Type Description

- **English**: "TuneLab Project"
- **Chinese**: "TuneLab 工程文件"

## Platform-Specific Implementation

### Windows

File associations on Windows are handled automatically through the application manifest and registry entries when the application is installed.

The `.csproj` file includes:
```xml
<ApplicationIcon>Assets\app.ico</ApplicationIcon>
```

For file type association, Windows uses:
- File extension: `.tlp`
- Icon: `Assets\file.ico`
- Description: "TuneLab Project"

### macOS

File associations on macOS are configured via `Info.plist`.

Key configurations:
- **CFBundleIconFile**: `app.icns` (application icon)
- **CFBundleDocumentTypes**: Defines `.tlp` file association
  - Extension: `tlp`
  - Icon: `file.icns`
  - Type Name: "TuneLab Project"
  - UTI: `com.tunelab.project`

#### Installation
Run the installation script to register file associations:
```bash
./install-macos.sh
```

This script creates a temporary app bundle and registers it with Launch Services.

### Linux

File associations on Linux follow the FreeDesktop.org standards using:
1. **Desktop Entry** (`tunelab.desktop`): Defines the application
2. **MIME Type** (`tunelab-mime.xml`): Defines the `.tlp` file type
3. **Icons**: PNG format icons in various sizes

#### Installation
Run the installation script:
```bash
./install-linux.sh
```

This script:
- Installs the desktop file to `~/.local/share/applications/`
- Registers the MIME type to `~/.local/share/mime/packages/`
- Installs file type icons to `~/.local/share/icons/hicolor/`
- Updates system databases

The MIME type is defined as:
- Type: `application/x-tunelab-project`
- Pattern: `*.tlp`
- Description: "TuneLab Project" (English), "TuneLab 工程文件" (Chinese)

## Technical Details

### Icon Formats

- **Windows (.ico)**: Multi-resolution icon format, contains multiple sizes (16x16, 32x32, 48x48, 256x256)
- **macOS (.icns)**: Apple icon format, contains multiple resolutions including Retina sizes
- **Linux (.png)**: PNG images in multiple sizes (16, 32, 48, 64, 128, 256)

### Icon Conversion

Icons are converted from the base `.ico` format:
- `app.ico` → `app.icns` (macOS) + `app*.png` (Linux)
- `file.ico` → `file.icns` (macOS) + `file*.png` (Linux)

## Testing

### Windows
1. Build and run the application
2. Create a `.tlp` file
3. The file should show the file.ico icon
4. Double-clicking should open the file in TuneLab

### macOS
1. Build and run the application
2. Run `./install-macos.sh`
3. Create a `.tlp` file
4. The file should show the file.icns icon
5. The file should be associated with TuneLab in "Open With" menu

### Linux
1. Build and run the application
2. Run `./install-linux.sh`
3. Log out and log back in (or restart your desktop environment)
4. Create a `.tlp` file
5. The file should show the file type icon
6. Right-click → "Open With" should show TuneLab

## Troubleshooting

### macOS
- If file associations don't work, try rebuilding the Launch Services database:
  ```bash
  /System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -kill -r -domain local -domain system -domain user
  ```
- Restart Finder: `killall Finder`

### Linux
- If icons don't appear, update the icon cache manually:
  ```bash
  gtk-update-icon-cache -f -t ~/.local/share/icons/hicolor
  ```
- If MIME types don't work, update the MIME database:
  ```bash
  update-mime-database ~/.local/share/mime
  ```
- Restart your desktop environment or log out and back in

## File Locations

- Project file: `TuneLab/TuneLab.csproj` - Build configuration
- Info.plist: `TuneLab/Info.plist` - macOS metadata
- Desktop file: `TuneLab/tunelab.desktop` - Linux application entry
- MIME type: `TuneLab/tunelab-mime.xml` - Linux file type definition
- Icons: `TuneLab/Assets/` - All icon files
