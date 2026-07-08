# Privacy Policy

**Effective Date:** July 8, 2026

Your privacy is critically important. Because the Discord - Combadge Connection for GalaxyFleetCommand (the "Software") runs entirely as a local utility on your personal computer, it is designed with a strict "zero data collection" architecture.

## 1. No Central Data Collection
* **No Remote Servers:** The Software does not operate, communicate with, or transfer data to any external, centralized web servers or databases.
* **No Telemetry:** There are no tracking scripts, analytics cookies, or background crash telemetry systems embedded within the executable.

## 2. Information Handled Locally
To function properly, the Software interacts with two platforms natively on your local machine:
* **Discord IPC:** The application connects via local Windows Named Pipes to your running Discord desktop client. It requests access tokens strictly to send the local `SetMuteAsync` state command.
* **VRChat OSC:** The application binds locally to loopback network ports (`127.0.0.1`) to receive network packets coming directly from your local running instance of VRChat.
* **Local Tokens:** Any authentication tokens exchanged with the Discord client are held safely in memory or handled locally on your machine. They are never transmitted to the project maintainers or any third party.

## 3. Third-Party Services
While this Software does not collect your data, it acts as a bridge between two independent platforms. Your use of those platforms is governed by their respective privacy policies:
* **Discord Privacy Policy:** [https://discord.com/privacy](https://discord.com/privacy)
* **VRChat Privacy Policy:** [https://vrchat.com/privacy](https://vrchat.com/privacy)

## 4. Contact
If you have any questions regarding the local security mechanics of this software, feel free to open an Issue or a Discussion thread directly on the project's public GitHub repository.