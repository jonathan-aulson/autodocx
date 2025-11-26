#!/bin/bash

# CONFIGURATION
FUNCTION_SOURCE_DIR="./src/TownePark.Billing.Api"
OUTPUT_DIR="./output"
ZIP_NAME="function.zip"
PUBLISH_DIR="$OUTPUT_DIR/app"

# CLEAN OUTPUT
echo "Cleaning output directory..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$PUBLISH_DIR"

# FIND .csproj
CSPROJ_PATH=$(find "$FUNCTION_SOURCE_DIR" -maxdepth 1 -name "*.csproj" | head -n 1)

if [ -z "$CSPROJ_PATH" ]; then
  echo "No .csproj file found in $FUNCTION_SOURCE_DIR"
  exit 1
fi

# .NET PUBLISH
echo "Publishing .NET project from $CSPROJ_PATH..."
dotnet publish "$CSPROJ_PATH" -c Release -o "$PUBLISH_DIR"

if [ $? -ne 0 ]; then
  echo ".NET publish failed"
  exit 1
fi

# ZIP PACKAGE
echo "Creating ZIP package..."
cd "$PUBLISH_DIR" || exit 1
zip -r "../$ZIP_NAME" . > /dev/null
cd - > /dev/null

echo "Package created at $OUTPUT_DIR/$ZIP_NAME"
