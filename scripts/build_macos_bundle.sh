#!/bin/zsh
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  scripts/build_macos_bundle.sh --runtime <osx-arm64|osx-x64> [--configuration Release] [--output-dir <dir>] [--version <ver>] [--skip-zip]
                                [--sign-identity <identity>]
                                [--notarize --apple-key-id <id> --apple-issuer-id <issuer> --apple-api-key-file <path>]

What it does:
  1. Publishes CBDB Desktop for a single macOS runtime
  2. Builds a standard .app bundle from the publish output
  3. Optionally signs and notarizes the app bundle
  4. Optionally zips the app bundle with ditto

Notes:
  - Uses single-file self-contained publish for the app bundle experiment path.
EOF
}

CONFIGURATION="Release"
OUTPUT_DIR=""
VERSION=""
SKIP_ZIP=0
SIGN_IDENTITY=""
NOTARIZE=0
APPLE_KEY_ID=""
APPLE_ISSUER_ID=""
APPLE_API_KEY_FILE=""
RUNTIME=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --runtime)
      RUNTIME="${2:?missing value for --runtime}"
      shift 2
      ;;
    --configuration)
      CONFIGURATION="${2:?missing value for --configuration}"
      shift 2
      ;;
    --output-dir)
      OUTPUT_DIR="${2:?missing value for --output-dir}"
      shift 2
      ;;
    --version)
      VERSION="${2:?missing value for --version}"
      shift 2
      ;;
    --skip-zip)
      SKIP_ZIP=1
      shift
      ;;
    --sign-identity)
      SIGN_IDENTITY="${2:?missing value for --sign-identity}"
      shift 2
      ;;
    --notarize)
      NOTARIZE=1
      shift
      ;;
    --apple-key-id)
      APPLE_KEY_ID="${2:?missing value for --apple-key-id}"
      shift 2
      ;;
    --apple-issuer-id)
      APPLE_ISSUER_ID="${2:?missing value for --apple-issuer-id}"
      shift 2
      ;;
    --apple-api-key-file)
      APPLE_API_KEY_FILE="${2:?missing value for --apple-api-key-file}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ "${RUNTIME}" != "osx-arm64" && "${RUNTIME}" != "osx-x64" ]]; then
  echo "--runtime must be osx-arm64 or osx-x64" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
PROJECT_PATH="${REPO_ROOT}/Cbdb.App.Avalonia/Cbdb.App.Avalonia.csproj"
ICON_PATH="${REPO_ROOT}/Cbdb.App.Avalonia/Assets/AppIcon.icns"
APP_NAME="CBDB Desktop"
EXECUTABLE_NAME="CBDB"

if [[ -z "${OUTPUT_DIR}" ]]; then
  OUTPUT_DIR="${REPO_ROOT}/artifacts/local-macos-${RUNTIME}"
fi

if [[ -z "${VERSION}" ]]; then
  VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "${PROJECT_PATH}" | head -n 1)"
fi

if [[ -z "${VERSION}" ]]; then
  echo "Could not determine version from ${PROJECT_PATH}" >&2
  exit 1
fi

if [[ ! -f "${ICON_PATH}" ]]; then
  echo "Missing app icon: ${ICON_PATH}" >&2
  exit 1
fi

if [[ ${NOTARIZE} -eq 1 && -z "${SIGN_IDENTITY}" ]]; then
  echo "--notarize requires --sign-identity" >&2
  exit 1
fi

if [[ ${NOTARIZE} -eq 1 ]]; then
  if [[ -z "${APPLE_KEY_ID}" || -z "${APPLE_ISSUER_ID}" || -z "${APPLE_API_KEY_FILE}" ]]; then
    echo "--notarize requires --apple-key-id, --apple-issuer-id, and --apple-api-key-file" >&2
    exit 1
  fi
  if [[ ! -f "${APPLE_API_KEY_FILE}" ]]; then
    echo "Missing Apple API key file: ${APPLE_API_KEY_FILE}" >&2
    exit 1
  fi
fi

PUBLISH_DIR="${OUTPUT_DIR}/publish"
BUNDLE_ROOT="${OUTPUT_DIR}/${APP_NAME}.app"
CONTENTS_DIR="${BUNDLE_ROOT}/Contents"
MACOS_DIR="${CONTENTS_DIR}/MacOS"
RESOURCES_DIR="${CONTENTS_DIR}/Resources"
ZIP_PATH="${OUTPUT_DIR}/cbdb-${VERSION}-${RUNTIME}.zip"
NOTARY_ZIP_PATH="${OUTPUT_DIR}/cbdb-${VERSION}-${RUNTIME}.notary.zip"
ENTITLEMENTS_PATH="${OUTPUT_DIR}/entitlements.plist"

rm -rf "${PUBLISH_DIR}" "${BUNDLE_ROOT}"
mkdir -p "${PUBLISH_DIR}" "${MACOS_DIR}" "${RESOURCES_DIR}"

echo "==> Restoring project"
DOTNET_CLI_HOME=/tmp dotnet restore "${PROJECT_PATH}" --configfile "${REPO_ROOT}/NuGet.Config"

echo "==> Publishing ${RUNTIME} (${CONFIGURATION})"
DOTNET_CLI_HOME=/tmp dotnet publish "${PROJECT_PATH}" \
  -c "${CONFIGURATION}" \
  -r "${RUNTIME}" \
  --self-contained true \
  --no-restore \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:EnableCompressionInSingleFile=true \
  /p:UseAppHost=true \
  /p:DebugType=None \
  /p:DebugSymbols=false \
  -o "${PUBLISH_DIR}"

echo "==> Building app bundle"
cp -R "${PUBLISH_DIR}/." "${MACOS_DIR}/"

cp "${ICON_PATH}" "${RESOURCES_DIR}/AppIcon.icns"

cat > "${CONTENTS_DIR}/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "https://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>${EXECUTABLE_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>org.cbdb.desktop</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon.icns</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleDisplayName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
  </dict>
</plist>
EOF

if [[ -d "${REPO_ROOT}/data" ]]; then
  cp -R "${REPO_ROOT}/data" "${RESOURCES_DIR}/data"
  ln -sfn ../Resources/data "${MACOS_DIR}/data"
fi

if [[ -f "${REPO_ROOT}/THIRD-PARTY-LICENSES.md" ]]; then
  cp "${REPO_ROOT}/THIRD-PARTY-LICENSES.md" "${RESOURCES_DIR}/THIRD-PARTY-LICENSES.md"
  ln -sfn ../Resources/THIRD-PARTY-LICENSES.md "${MACOS_DIR}/THIRD-PARTY-LICENSES.md"
fi

chmod +x "${MACOS_DIR}/${EXECUTABLE_NAME}"

if [[ -n "${SIGN_IDENTITY}" ]]; then
  echo "==> Codesigning app bundle"

  cat > "${ENTITLEMENTS_PATH}" <<'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
  </dict>
</plist>
EOF

  echo "==> Removing existing signatures"
  codesign --remove-signature "${BUNDLE_ROOT}" 2>/dev/null || true
  find "${BUNDLE_ROOT}" -type f -exec codesign --remove-signature {} \; 2>/dev/null || true

  echo "==> Signing nested native libraries"
  find "${MACOS_DIR}" -maxdepth 1 -type f -name "*.dylib" -print0 | while IFS= read -r -d '' file_path; do
    codesign --force --timestamp --options runtime --sign "${SIGN_IDENTITY}" "${file_path}"
  done

  echo "==> Signing main executable"
  codesign --force \
    --timestamp \
    --options runtime \
    --entitlements "${ENTITLEMENTS_PATH}" \
    --sign "${SIGN_IDENTITY}" \
    "${MACOS_DIR}/${EXECUTABLE_NAME}"

  codesign --force \
    --sign "${SIGN_IDENTITY}" \
    --timestamp \
    --options runtime \
    --entitlements "${ENTITLEMENTS_PATH}" \
    "${BUNDLE_ROOT}"

  echo "==> Verifying signature"
  codesign --verify --deep --strict --verbose=2 "${BUNDLE_ROOT}"
  codesign -dvvv "${BUNDLE_ROOT}"
  codesign -dvvv "${MACOS_DIR}/${EXECUTABLE_NAME}"
  spctl --assess --type execute --verbose=2 "${BUNDLE_ROOT}" || true
fi

if [[ ${NOTARIZE} -eq 1 ]]; then
  echo "==> Creating notarization archive"
  rm -f "${NOTARY_ZIP_PATH}"
  ditto -c -k --sequesterRsrc --keepParent "${BUNDLE_ROOT}" "${NOTARY_ZIP_PATH}"

  echo "==> Submitting for notarization"
  xcrun notarytool submit "${NOTARY_ZIP_PATH}" \
    --key "${APPLE_API_KEY_FILE}" \
    --key-id "${APPLE_KEY_ID}" \
    --issuer "${APPLE_ISSUER_ID}" \
    --wait

  echo "==> Stapling notarization ticket"
  xcrun stapler staple "${BUNDLE_ROOT}"
  xcrun stapler validate "${BUNDLE_ROOT}"
fi

if [[ ${SKIP_ZIP} -eq 0 ]]; then
  echo "==> Creating zip archive"
  rm -f "${ZIP_PATH}"
  ditto -c -k --sequesterRsrc --keepParent "${BUNDLE_ROOT}" "${ZIP_PATH}"
  echo "ZIP: ${ZIP_PATH}"
fi

echo "APP: ${BUNDLE_ROOT}"
