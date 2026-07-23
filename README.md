# Discord - Combadge Connection for GalaxyFleetCommand

An intelligent Open Sound Control (OSC) bridge designed to link VRChat avatar parameter actions directly to your Discord desktop client. Tapping your in-game Combadge instantly synchronizes your voice state in Discord.

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

1. Download the latest `.unitypackage` from the **Releases** section.
2. Import the package into your Unity project (`Assets -> Import Package -> Custom Package...`).
3. Drag the Combadge Bridge prefab onto your avatar hierarchy.
4. Reposition the container object labeled **"Move over badge location"** so it aligns perfectly with your avatar's physical chest badge.
5. If your avatar doesn't follow standard armature naming conventions, make sure to edit the Armature link of the **"Move over badge location"** object to point to the proper path (e.g., left breast/chest bone) so the badge follows properly.
6. Build and upload your avatar via the VRChat SDK.

---

### Step 2: Choose & Run Your Executable

#### Option A: Using `GFC_ComBadge_Keybind.exe` (Fastest)

1. Open **Discord** -> **User Settings** -> **Keybinds**.
2. Add a keybind for **Toggle Mute** and one for **Toggle Deafen**.
3. Run `GFC_ComBadge_Keybind.exe`.
4. **First-Time Setup Wizard:**
   * The console will prompt you to click the recording field for **Toggle Mute** in Discord.
   * Press **[ENTER]** in the Combadge console window. A 5-second countdown will start, giving you time to switch to Discord and click inside the keybind box. The program will automatically send an `F24` signal to record it.
   * Repeat the exact same steps for **Toggle Deafen** using the `F23` signal.
   * Once configured, setup data is saved to your `AppData/Roaming/GFC_ComBadge` folder.
5. Launch VRChat with **OSC enabled** (`Radial Menu -> Options -> OSC -> Enabled`) and tap your badge!

---

#### Option B: Using `GFC_ComBadge_RPC.exe` (IPC Integration)

Due to Discord Developer Portal OAuth2 restrictions, this method requires access permissions:

* **Standard Users:** You must be added to the internal **App Testers** whitelist by the project maintainer before launching.
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
* **Discord Integration:** Discord IPC Named Pipes / OAuth2 API
* **Configuration:** Microsoft Extensions Configuration (JSON Streams)

---

## Troubleshooting & Notes

> [!NOTE]
> **OSC Layout Refresh:** If your avatar parameter fails to register on startup, open your VRChat Expression Menu, navigate to **Options -> OSC**, and click **Reset Config** to force VRChat to regenerate your avatar's layout file.

> [!NOTE]
> **Multiple Discord Clients:** If you run multiple Discord instances simultaneously, the software will attach to the instance that was launched first on your system.

> [!NOTE]
> **Notification Sounds (Keybind Mode):** When using `GFC_ComBadge_Keybind.exe`, you may want to disable Discord's notification sounds for **Mute**, **Unmute**, **Deafen**, and **Undeafen** in Discord Settings -> **Notifications**. Since multiple key signals are sent rapidly to guarantee exact state synchronization, turning off these sounds prevents rapid audio bleeps.

---

## Special Thanks

Special thanks goes to:
- **Digikind**, **LessaShuftan**, and **Lother** for helping with the Unity side of things
- **Lulalaby** for helping with the initial code and asset
- **terri_versh** for helping with the art for the asset

Credit to **Mavrickshuey** for the original idea.
