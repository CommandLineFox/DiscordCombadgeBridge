# Discord - Combadge Connection for GalaxyFleetCommand

An intelligent Open Sound Control (OSC) bridge designed to link VRChat avatar parameter actions directly to your Discord desktop client. Tapping your in-game Combadge instantly synchronizes your voice state in Discord.

## Built With

* **Framework:** .NET 9.0 (Console Application)
* **VRChat Network Protocol:** CoreOSC (`LucHeart.CoreOSC`)
* **Discord Integration:** Discord Inter-Process Communication (IPC) Named Pipes
* **Configuration Engine:** Microsoft Extensions Configuration (JSON Streams)

---

## Requirements

### Standard Users
Due to Discord developer application restrictions, you must be added to the internal **App Testers** whitelist to use the default configuration. Please contact the project maintainer to get your Discord account whitelisted before launching.

### Developers / Custom Setup
If you want to run your own independent version, create a custom application on the [Discord Developer Portal](https://discord.com/developers/applications) using these specifications:

1. **Installation Tab:** Set the installation method exclusively to **User Install**. Disable/remove any public install links.
2. **OAuth2 Tab:** Add a redirect URI pointing exactly to: `https://localhost`
3. **Application Credentials:** Copy your `Client ID` and `Client Secret` into the embedded `appsettings.json` configuration file prior to compiling the single-file executable.

---

## Installation Pipeline

### 1. Avatar Unity Setup
1. Download the latest `.unitypackage` from the **Releases** section.
2. Import the package into your Unity project (`Assets -> Import Package -> Custom Package...`).
3. Drag the Combadge prefab onto your avatar hierarchy.
4. Reposition the container object labeled **"Move me"** so it aligns perfectly with your avatar's physical chest badge.
5. Build and upload your avatar via the VRChat SDK.

### 2. Desktop Client Setup
1. Download the compiled standalone `GFC_ComBadge.exe` from the **Releases** section.
2. No installation or external `.NET Runtime` files are required if using the self-contained build.

---

## How to Run

1. Launch your **Discord** desktop application and join a voice channel.
2. Launch **VRChat** and ensure that **OSC is enabled** in your in-game Radial Menu (`Options -> OSC -> Enabled`).
3. Run the `GFC_ComBadge.exe` executable.
4. A Discord authorization window will pop up automatically. Click **Authorize** to allow the local named pipe connection.
5. Tap your avatar's Combadge in-game to toggle your voice mute status.

Once you open the program, it'll mute you in Discord by default. The toggle of the badge is also off by default so you have to turn it on to toggle between states at the time of writing this.

---

## Special thanks
Special thanks goes to Digikind, LessaShuftan, Lulalaby and Lother for helping with this project.
Credit to Mavrickshuey for the original idea

---

> [!NOTE]
> If your avatar parameter fails to register on startup, open your VRChat Expression Menu, navigate to **Options -> OSC**, and click **Reset Config** to force VRChat to regenerate your avatar's OSC layout file.

> [!NOTE]
> If you are using multiple Discord client instances, the first one you have opened will be controlled by the software.
