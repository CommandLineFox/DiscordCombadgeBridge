# Discord - Combadge Connection for GalaxyFleetCommand

An intelligent Open Sound Control (OSC) bridge designed to link VRChat avatar parameter actions directly to your Discord desktop client. Tapping your in-game Combadge instantly synchronizes your voice state in Discord.

<<<<<<< HEAD
This repository builds two standalone binaries to give you complete freedom in how you want to bridge VRChat with Discord:

1. **`GFC_ComBadge_Keybind.exe` (Recommended):** Uses simulated global keypresses (`F23`/`F24`). Requires zero setup on the Discord Developer Portal and works out of the box for everyone.
2. **`GFC_ComBadge_RPC.exe` (Legacy Integration):** Uses Discord's native Inter-Process Communication (IPC) Named Pipes for direct API state sync.

---

## Quick Comparison

| Feature | Keybind Executable (`...Keybind.exe`) | RPC Integration Executable (`...RPC.exe`) |
| :--- | :--- | :--- |
| **Discord App Setup** | None required | Requires Whitelist OR Custom Dev Portal App |
| **First-Time Setup** | 5-second key mapping wizard | Discord Authorization Popup |
| **Desync Recovery** | Auto-forces state using Master Deafen (`F23`) | Triggered strictly via OSC events |
| **Mechanism** | Windows Virtual Keys (`F23`/`F24`) | Discord IPC / OAuth2 API |

---

## Installation & Setup

### Step 1: Avatar Unity Setup (Universal)
=======
Starting with version 2.0, the application offers two distinct modes of operation: the **Latest Keybind Mode** (no Discord Developer Portal setup required) and the **Legacy RPC Mode** (automatic state sync via Discord's developer API).

## Architecture & Frameworks

* **Framework:** .NET 10.0 (Console Application)
* **VRChat Network Protocol:** CoreOSC (`LucHeart.CoreOSC`)
* **Input Simulation (v2.x):** Windows Input Simulator / Native Virtual Key Codes
* **Discord Integration (v1.2 Legacy):** Discord Inter-Process Communication (IPC) Named Pipes
* **Configuration Engine:** Microsoft Extensions Configuration (JSON Streams)

---

## Choosing Your Version

| Feature / Requirement | Latest Mode (v2.0+) | Legacy RPC Mode (v1.2) |
| :--- | :--- | :--- |
| **Setup Complexity** | Very Low (5-second automated wizard) | Medium (Requires Discord Dev Portal / Whitelist) |
| **Discord Authorization Popup** | No | Yes (On first launch) |
| **Out-of-Sync Recovery** | Absolute alignment using Deafen tricks | State-based tracking via API |
| **Background Processing** | Uses global virtual keys (`F23`/`F24`) | Uses Discord RPC Named Pipes |

---

## Latest Version Setup (v2.x - Keybind Mode)

The latest version completely bypasses the restrictive Discord Developer Portal. Instead of relying on whitelists and RPC tokens, it runs a quick first-time console setup wizard that simulates virtual hardware keys (`F23` and `F24`) to trigger Discord's native global hotkeys.

### How to Run (v2.x)
1. Launch your **Discord** desktop application.
2. Launch **VRChat** and ensure that **OSC is enabled** (`Options -> OSC -> Enabled`).
3. Run the `GFC_ComBadge.exe` executable.
4. **First-Time Setup Wizard:**
   * The console will prompt you to open Discord -> **User Settings** -> **Keybinds**.
   * Add a keybind for **Toggle Mute** and click the recording field.
   * Press **[ENTER]** in the Combadge console window. A 5-second countdown will start, giving you time to switch to Discord and click inside the keybind box. The program will automatically send an `F24` signal to record it.
   * Repeat the exact same steps for **Toggle Deafen** using the `F23` signal.
5. Once configured, a `setup.dat` file is saved to your `AppData/Roaming/GFC_ComBadge` folder, and the setup will never run again unless you delete that file.

> [!TIP]
> **Self-Healing Sync:** The Keybind Mode utilizes Discord's Master Deafen logic. If you ever manually click mute/unmute in Discord and desynchronize the application, the very next time you tap your in-game Combadge, it will automatically force Discord back into alignment.

---

## Legacy Version Setup (v1.2 - RPC Mode)

The legacy version relies on Discord's Rich Presence / IPC architecture. It provides an elegant, hands-free integration but requires developer access. It is still compatible with newer versions of the combadge asset.

### Requirements

#### Standard Users
Due to Discord developer application restrictions, you must be added to the internal **App Testers** whitelist by the project maintainer before launching this version.

#### Developers / Custom App Setup
To run an independent RPC version, create an application on the [Discord Developer Portal](https://discord.com/developers/applications):
1. **Installation Tab:** Set the installation method exclusively to **User Install**. Disable/remove public install links.
2. **OAuth2 Tab:** Add a redirect URI pointing exactly to: `https://localhost`
3. **Application Credentials:** Copy your `Client ID` and `Client Secret` into the embedded `appsettings.json` configuration file prior to compilation.

### How to Run (v1.2)
1. Launch **Discord** and join a voice channel.
2. Launch **VRChat** and ensure **OSC is enabled**.
3. Run the legacy `GFC_ComBadge.exe`.
4. An authorization window will pop up natively inside Discord. Click **Authorize** to allow the named pipe connection.

*Note: By default, the RPC version will mute you upon initial boot. You must toggle the in-game badge once to establish active tracking.*

---

## Pipeline: Avatar Unity Setup (Universal)

>>>>>>> d0d88f188663b68be91117e0d15e54dbf4e68248
1. Download the latest `.unitypackage` from the **Releases** section.
2. Import the package into your Unity project (`Assets -> Import Package -> Custom Package...`).
3. Drag the Combadge Bridge prefab onto your avatar hierarchy.
4. Reposition the container object labeled **"Move over badge location"** so it aligns perfectly with your avatar's physical chest badge.
<<<<<<< HEAD
5. *Unity Audio Tip:* Ensure your audio clip does **not** have `Load In Background` checked and that the Spatial Blend on the Audio Source is set to **3D (1)**.
=======
5. If your avatar doesn't follow standard armature naming conventions, make sure to edit the Armature link of the "Move over badge location" object to the proper path to your left breast or whichever location you plan to link the badge to follow against
>>>>>>> d0d88f188663b68be91117e0d15e54dbf4e68248
6. Build and upload your avatar via the VRChat SDK.

---

<<<<<<< HEAD
### Step 2: Choose Your Executable

#### Option A: Using `GFC_ComBadge_Keybind.exe` (Fastest)

1. Open **Discord** -> **User Settings** -> **Keybinds**.
2. Add a keybind for **Toggle Mute** and one for **Toggle Deafen**.
3. Run `GFC_ComBadge_Keybind.exe`.
4. If running for the first time, the console wizard will guide you to press **[ENTER]** with a 5-second countdown to automatically bind `F24` (Mute) and `F23` (Deafen) into Discord.
5. Launch VRChat with **OSC enabled** (`Radial Menu -> Options -> OSC -> Enabled`) and tap your badge!

---

#### Option B: Using `GFC_ComBadge_RPC.exe` (IPC Integration)

Due to Discord Developer Portal OAuth2 restrictions, this method requires access permissions.

* **Standard Users:** You must be added to the internal **App Testers** whitelist by the project maintainer.
* **Developers / Custom Setup:** Create a custom application on the [Discord Developer Portal](https://discord.com/developers/applications):
  1. **Installation Tab:** Set the installation method strictly to **User Install**.
  2. **OAuth2 Tab:** Set Redirect URI to `https://localhost`.
  3. **Credentials:** Add your `Client ID` and `Client Secret` to `appsettings.json` before compiling.

**To Run:**
1. Open **Discord** and join any voice channel.
2. Launch **VRChat** with **OSC enabled**.
3. Run `GFC_ComBadge_RPC.exe` and click **Authorize** on the Discord pop-up window.

---

## Technical Specifications

* **Framework:** .NET 9.0 / .NET 10.0 (Console Application)
* **VRChat Network Protocol:** CoreOSC (`LucHeart.CoreOSC`)
* **Input Simulation:** Native Virtual Key Codes (`WindowsInput`)
* **Discord IPC:** Named Pipe Stream over JSON RPC
* **Configuration:** Microsoft Extensions Configuration (JSON Streams)

---

## Troubleshooting & Notes

> [!NOTE]
> **OSC Layout Refresh:** If your avatar parameter fails to register on startup, open your VRChat Expression Menu, navigate to **Options -> OSC**, and click **Reset Config** to force VRChat to regenerate your avatar's layout file.

> [!NOTE]
> **Multiple Discord Clients:** If you run multiple Discord instances, the software will attach to the instance that was launched first.

---

=======
## Troubleshooting & Notes

> [!NOTE]
> **OSC Layout Refresh:** If your avatar parameter fails to register on startup, open your VRChat Expression Menu, navigate to **Options -> OSC**, and click **Reset Config** to force VRChat to regenerate your avatar's OSC layout file.

> [!NOTE]
> **Multiple Clients:** If you are running multiple Discord client instances simultaneously, the software will always latch onto and control the *first* instance that was opened on your operating system.

> [!NOTE]
> **Sounds:** If you are using version 2.0 or higher, you may want to consider disabling the notification sound of muting, unmuting, deafening and undeafening since those four happen all at the same time when interacting with the badge to achieve a properly synced voice state.

---

>>>>>>> d0d88f188663b68be91117e0d15e54dbf4e68248
## Special Thanks
Special thanks goes to:
- **Digikind**, **LessaShuftan**, and **Lother** for helping with the Unity side of things
- **Lulalaby** for helping with the initial code and asset
- **terri_versh** for helping with the art for the asset

<<<<<<< HEAD
Credit to **Mavrickshuey** for the original idea.
=======
Credit to Mavrickshuey for the original idea.
>>>>>>> d0d88f188663b68be91117e0d15e54dbf4e68248
