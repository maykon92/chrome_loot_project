# DarkThemeInstaller – Ethical Exfiltration Simulation

This repository demonstrates an ethical and educational proof-of-concept on data exfiltration using social engineering in a fully controlled lab environment.

## 🧠 Objective

To simulate how a deceptive Windows executable (disguised as a theme installer) can collect and exfiltrate sensitive browser data when run by a user in a lab VM environment.

---

## 🗂️ Project Structure

```
chrome_loot_project/
├── victim_machine/
│   ├── DarkthemeInstaller.csproj
│   ├── Program.cs                  # Main logic with server communication and wallpaper change
│   ├── Properties/
│   │   └── Resources.resx
│   ├── assets/
│   │   └── wallpaper.jpg
│   └── darktheme.ico
├── kali_server/
│   ├── app.py                      # Flask server to receive .zip
│   ├── server.log                  # Logs from POST requests
│   └── loot/                       # Received zip files
├── demo/
│   └── chrome_dark_theme.mp4       # Final video with voiceover
├── README.md
└── LICENSE
```

---

## 🧪 How it Works

1. **Flask Server (Kali Machine):**
   - Listens on `http://<kali_ip>:5000/receive`
   - Accepts `POST` requests and saves incoming `.zip` files to `/loot`

2. **Theme Installer (Windows VM):**
   - Written in C# and compiled via `.NET`
   - Executes:
     - Changes wallpaper
     - Copies Chrome data from `AppData\Local\Google\Chrome\User Data\Default`
     - Zips content
     - Sends via HTTP POST to Kali

---

## 🧪 Test Environment

- **Victim OS:** Windows Server 2022 (Virtual Machine)
- **Attacker OS:** Kali Linux (Virtual Machine)
- **Connection:** Host-only adapter or NAT with port forwarding
- **Server:** Python Flask server to receive POST data from the C# executable

---

## 🛠️ Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- Visual Studio or VS Code
- Python 3.x with Flask (for the receiver server)
- Kali Linux (or another Linux machine for testing)

---

## 🔧 Build Instructions

From the root folder of the project, run the following commands:

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Publish to a folder
dotnet publish -c Release -o ./publish
```

This will generate a standalone build inside the `./publish` folder, including all required DLLs.

---

## 🚀 Running the Installer

1. Start the Flask server on Kali Linux:

```bash
cd chrome_loot_server
python3 app.py
```

2. On the Windows VM, run the `DarkThemeInstaller.exe`:

```powershell
.\DarkThemeInstaller.exe
```

The application will:
- Apply a new dark wallpaper.
- Copy Chrome data folder.
- Zip the data and send it to the Flask server using `curl`.

---

## 📺 Demonstration

A complete video was recorded showing the simulation from start to finish. Subtitles and narration explain the steps taken, the technologies used, and the purpose of each stage.

---

## 🔒 Ethical Considerations

This project was created solely to raise awareness about how social engineering techniques and visual deception can lead to data compromise. All tests were conducted in a **closed lab** with **full control** over all machines involved.

---

## 📄 License

This project is provided under the MIT License. You are free to use, modify, and distribute it for educational or ethical research purposes. **You must not use it for malicious purposes.**

---

## 🙋 About the Author

Developed by **Maykon Da Luz**, cybersecurity student and software developer.

> *"Ethical hacking is not about breaking the system. It's about learning how to protect it."*