#!/bin/bash

# TuneLab macOS Installation Script
# This script sets up file associations for .tlp files on macOS

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
APP_DIR="$SCRIPT_DIR"

echo "Setting up TuneLab file associations for macOS..."

# Create a temporary app bundle structure for file association
BUNDLE_DIR="/tmp/TuneLab.app"
CONTENTS_DIR="$BUNDLE_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"

# Clean up any existing temporary bundle
rm -rf "$BUNDLE_DIR"

# Create the bundle structure
mkdir -p "$MACOS_DIR"
mkdir -p "$RESOURCES_DIR"

# Copy the Info.plist
if [ -f "$APP_DIR/Info.plist" ]; then
    cp "$APP_DIR/Info.plist" "$CONTENTS_DIR/"
    echo "Info.plist copied"
else
    echo "Error: Info.plist not found"
    exit 1
fi

# Copy icons
if [ -f "$APP_DIR/Assets/app.icns" ]; then
    cp "$APP_DIR/Assets/app.icns" "$RESOURCES_DIR/"
    echo "Application icon copied"
fi

if [ -f "$APP_DIR/Assets/file.icns" ]; then
    cp "$APP_DIR/Assets/file.icns" "$RESOURCES_DIR/"
    echo "File type icon copied"
fi

# Create a symlink to the executable
ln -s "$APP_DIR/TuneLab" "$MACOS_DIR/TuneLab"

# Register the file association
echo "Registering file associations..."
/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -f "$BUNDLE_DIR"

# Clean up
rm -rf "$BUNDLE_DIR"

echo "Installation complete!"
echo "The .tlp file association should now be registered with macOS."
echo "You may need to restart Finder for changes to take effect."
