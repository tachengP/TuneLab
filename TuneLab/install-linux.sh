#!/bin/bash

# TuneLab Linux Installation Script
# This script installs the desktop file and MIME type associations for .tlp files

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
APP_DIR="$SCRIPT_DIR"

echo "Installing TuneLab desktop integration..."

# Install desktop file
if [ -f "$APP_DIR/tunelab.desktop" ]; then
    mkdir -p ~/.local/share/applications
    cp "$APP_DIR/tunelab.desktop" ~/.local/share/applications/
    
    # Update Exec path in desktop file to use absolute path
    sed -i "s|Exec=TuneLab|Exec=$APP_DIR/TuneLab|g" ~/.local/share/applications/tunelab.desktop
    sed -i "s|Icon=tunelab|Icon=$APP_DIR/Assets/app.png|g" ~/.local/share/applications/tunelab.desktop
    
    echo "Desktop file installed to ~/.local/share/applications/"
else
    echo "Warning: tunelab.desktop not found"
fi

# Install MIME type
if [ -f "$APP_DIR/tunelab-mime.xml" ]; then
    mkdir -p ~/.local/share/mime/packages
    cp "$APP_DIR/tunelab-mime.xml" ~/.local/share/mime/packages/
    
    # Update MIME database
    if command -v update-mime-database >/dev/null 2>&1; then
        update-mime-database ~/.local/share/mime
        echo "MIME type registered"
    else
        echo "Warning: update-mime-database not found, MIME type may not be registered"
    fi
else
    echo "Warning: tunelab-mime.xml not found"
fi

# Install MIME type icon
if [ -f "$APP_DIR/Assets/file.png" ]; then
    mkdir -p ~/.local/share/icons/hicolor/256x256/mimetypes
    cp "$APP_DIR/Assets/file.png" ~/.local/share/icons/hicolor/256x256/mimetypes/application-x-tunelab-project.png
    
    # Copy other sizes if available
    for size in 16 32 48 64 128; do
        if [ -f "$APP_DIR/Assets/file-${size}.png" ]; then
            mkdir -p ~/.local/share/icons/hicolor/${size}x${size}/mimetypes
            cp "$APP_DIR/Assets/file-${size}.png" ~/.local/share/icons/hicolor/${size}x${size}/mimetypes/application-x-tunelab-project.png
        fi
    done
    
    # Update icon cache
    if command -v gtk-update-icon-cache >/dev/null 2>&1; then
        if gtk-update-icon-cache -f -t ~/.local/share/icons/hicolor 2>&1; then
            echo "Icon cache updated"
        else
            echo "Warning: Failed to update icon cache. Icons may not appear until next login."
        fi
    fi
    
    echo "MIME type icon installed"
else
    echo "Warning: file.png not found"
fi

# Update desktop database
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database ~/.local/share/applications
    echo "Desktop database updated"
fi

echo "Installation complete!"
echo "You may need to log out and log back in for changes to take effect."
