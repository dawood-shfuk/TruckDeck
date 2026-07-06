# Truck Deck Android App

This is a native Android wrapper (WebView) for the Truck Command Deck.

## Prerequisites
1. **Java 17+** (Detected on your system)
2. **Gradle** (You mentioned you have it)
3. **Android SDK** (Detected at `%LOCALAPPDATA%\Android\Sdk`)

## How to Build the APK
1. Open a terminal in this folder (`android_app`).
2. Run the build script:
   ```bash
   build_apk.bat
   ```
   The script first copies the latest `..\pwa` shell into the app's assets, then
   builds. (It also auto-detects the Gradle install.)
3. Once finished, your APK will be at:
   `app\build\outputs\apk\debug\app-debug.apk`

## How it works (no hardcoded IP anymore)
The app no longer hardcodes a server URL. Instead it bundles the **PWA shell**
inside the APK and loads it from `file:///android_asset/pwa/index.html`.

On first launch, open the **3-dot menu (top-right)** and:
- enter your **Server IP** (the PC running the Telemetry Server) and tap SAVE,
- pick a **dashboard** from the list.

Your IP and chosen dashboard are saved on the device, so the app keeps working
even when the PC's IP changes - just update it in the menu. The shell talks to
`http://<your-ip>:25555` for skins/telemetry and embeds the server's dashboard
in a WebView.

> The WebView is configured for this (DOM storage, cleartext http, and
> cross-origin access from the bundled `file://` page) in `MainActivity.java`.

## Controls
Buttons that control the truck require the **Input Bridge** running on the PC
(see `../input_bridge`). The dashboard talks to it on port `25556`.
