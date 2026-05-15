#!/bin/bash
# Build a .deb package for OE2EmpireTracker.Desktop
# Requires: dpkg-deb (standard on Debian/Ubuntu)
# Run from the solution root after publishing for linux-x64

set -e

VERSION="2.0.0"
PACKAGE_NAME="oe2empiretracker"
ARCH="amd64"
PUBLISH_DIR="OE2EmpireTracker.Desktop/bin/publish/linux-x64"
DEB_DIR="OE2EmpireTracker.Desktop/bin/deb"

echo "=== Building .deb package ==="

# Check publish output exists
if [ ! -f "$PUBLISH_DIR/OE2EmpireTracker.Desktop" ]; then
    echo "ERROR: Publish output not found. Run publish.sh first."
    exit 1
fi

# Clean and create deb structure
rm -rf "$DEB_DIR"
mkdir -p "$DEB_DIR/DEBIAN"
mkdir -p "$DEB_DIR/opt/$PACKAGE_NAME"
mkdir -p "$DEB_DIR/usr/share/applications"
mkdir -p "$DEB_DIR/usr/local/bin"

# Control file
cat > "$DEB_DIR/DEBIAN/control" << EOF
Package: $PACKAGE_NAME
Version: $VERSION
Section: games
Priority: optional
Architecture: $ARCH
Depends: libx11-6, libfontconfig1
Maintainer: OE2 Empire Tracker Team
Description: Empire management tool for Outer Empires 2
 Cross-platform desktop application for tracking and managing
 player empires in the game Outer Empires 2. Built with
 Avalonia UI and .NET 8.
EOF

# Copy published files
cp -r "$PUBLISH_DIR/"* "$DEB_DIR/opt/$PACKAGE_NAME/"
chmod +x "$DEB_DIR/opt/$PACKAGE_NAME/OE2EmpireTracker.Desktop"

# Symlink to /usr/local/bin
ln -sf "/opt/$PACKAGE_NAME/OE2EmpireTracker.Desktop" "$DEB_DIR/usr/local/bin/oe2empiretracker"

# Desktop entry
cp "OE2EmpireTracker.Desktop/Assets/oe2empiretracker.desktop" "$DEB_DIR/usr/share/applications/"

# Build the .deb
dpkg-deb --build "$DEB_DIR" "OE2EmpireTracker.Desktop/bin/${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"

echo ""
echo "=== .deb package built ==="
echo "Output: OE2EmpireTracker.Desktop/bin/${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"
echo ""
echo "Install with: sudo dpkg -i ${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"
