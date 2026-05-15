#!/bin/bash
# Build an .rpm package for OE2EmpireTracker.Desktop
# Requires: rpmbuild (install with: sudo yum install rpm-build)
# Run from the solution root after publishing for linux-x64

set -e

VERSION="2.0.0"
RELEASE="1"
PACKAGE_NAME="oe2empiretracker"
ARCH="x86_64"
PUBLISH_DIR="OE2EmpireTracker.Desktop/bin/publish/linux-x64"
RPM_DIR="OE2EmpireTracker.Desktop/bin/rpm"

echo "=== Building .rpm package ==="

# Check publish output exists
if [ ! -f "$PUBLISH_DIR/OE2EmpireTracker.Desktop" ]; then
    echo "ERROR: Publish output not found. Run publish.sh first."
    exit 1
fi

# Clean and create rpm build structure
rm -rf "$RPM_DIR"
mkdir -p "$RPM_DIR/BUILD"
mkdir -p "$RPM_DIR/RPMS"
mkdir -p "$RPM_DIR/SOURCES"
mkdir -p "$RPM_DIR/SPECS"
mkdir -p "$RPM_DIR/BUILDROOT/opt/$PACKAGE_NAME"
mkdir -p "$RPM_DIR/BUILDROOT/usr/share/applications"
mkdir -p "$RPM_DIR/BUILDROOT/usr/local/bin"

# Copy files to buildroot
cp -r "$PUBLISH_DIR/"* "$RPM_DIR/BUILDROOT/opt/$PACKAGE_NAME/"
chmod +x "$RPM_DIR/BUILDROOT/opt/$PACKAGE_NAME/OE2EmpireTracker.Desktop"
cp "OE2EmpireTracker.Desktop/Assets/oe2empiretracker.desktop" "$RPM_DIR/BUILDROOT/usr/share/applications/"
ln -sf "/opt/$PACKAGE_NAME/OE2EmpireTracker.Desktop" "$RPM_DIR/BUILDROOT/usr/local/bin/oe2empiretracker"

# Spec file
cat > "$RPM_DIR/SPECS/$PACKAGE_NAME.spec" << EOF
Name:           $PACKAGE_NAME
Version:        $VERSION
Release:        $RELEASE
Summary:        Empire management tool for Outer Empires 2
License:        Proprietary
Group:          Games

%description
Cross-platform desktop application for tracking and managing
player empires in the game Outer Empires 2. Built with
Avalonia UI and .NET 8.

%install
cp -r %{_builddir}/../BUILDROOT/* %{buildroot}/

%files
/opt/$PACKAGE_NAME/
/usr/share/applications/oe2empiretracker.desktop
/usr/local/bin/oe2empiretracker
EOF

# Build RPM
rpmbuild --define "_topdir $(pwd)/$RPM_DIR" -bb "$RPM_DIR/SPECS/$PACKAGE_NAME.spec"

echo ""
echo "=== .rpm package built ==="
echo "Output: $RPM_DIR/RPMS/$ARCH/"
echo ""
echo "Install with: sudo rpm -i ${PACKAGE_NAME}-${VERSION}-${RELEASE}.${ARCH}.rpm"
