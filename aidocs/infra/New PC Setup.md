# 🧰 System Build Overview

- **CPU:** AMD Ryzen 9 7900 (12-core, AM5)
- **GPU:** RTX 4070 Super (CUDA-ready)
- **Motherboard:** ASUS B650M-E (WiFi, Bluetooth, USB-C compatible)
- **RAM:** 64GB DDR5 (G.SKILL Flare X5)
- **Storage:** Samsung 990 Pro 2TB Gen4 NVMe SSD
- **PSU:** Corsair RM850x (fully modular, 850W, PCIe 5.1)
- **Case:** Thermaltake Core V21 (Micro ATX, modular airflow)
- **CPU Cooler:** Noctua NH-U12A
- **Display Adapter:** USB-C to DisplayPort (for Apple Studio Display)
- **USB Drive:** Corsair Flash Voyager GTX 256GB (for Windows image + backup)
- **Mouse:** Microsoft Arc (Bluetooth)
- **Case Fan:** Arctic F12 added for optional airflow boost

## 🛠️ Assembly Guide
  
1. Unbox and verify all components against the parts list.
2. Mount CPU onto the motherboard and install Noctua NH-U12A cooler.
3. Insert RAM into correct DIMM slots and enable EXPO later in BIOS.
4. Install Samsung 990 Pro SSD into the M.2_1 slot.
5. Insert motherboard into Thermaltake Core V21 case.
6. Connect power supply (Corsair RM850x) and route cables.
7. Install GPU into primary PCIe slot, connect power.
8. Mount Arctic F12 case fan if airflow needs improvement.
9. Plug in DisplayPort adapter and peripherals (keyboard, mouse).
10. Boot into BIOS to validate component recognition and configure settings.

---

# 🖥️ New PC Setup Instructions for AI + Dev Work

## 👤 Owner: Colin McAllister  
**Purpose:** Optimized workstation for AI development, .NET modernization, and legacy system work (Perspect Medical Billing).

**Workflow Strategy:** Windows PC is the **primary development and deployment** environment. All Git repositories are cloned and active here for commits, builds, and testing. Mac Studio is used for **remote access, documentation, coordination**, and **GitHub review only**. A read-only clone may exist on the Mac for reference, but no changes should be committed or pushed from it. Remote Desktop is configured for full-screen development with seamless Mac multitasking (email, research, etc.).
  - No shared folders between Mac and PC at this time. Remote Desktop access is the only cross-platform integration currently in use. Shared folder setup may be revisited later based on workflow needs.

---


## 🛠️ Special Installation Considerations

### 🧠 1. BIOS Update Before OS Install
- B650 motherboards often ship with older BIOS versions.
- Use ASUS EZ Flash utility (from BIOS) to update via USB **before Windows install** if needed.
- Especially important for full RAM compatibility and system stability.

### ⚡ 2. Enable EXPO Profile for DDR5 RAM
- RAM defaults to base 4800 MHz unless EXPO is enabled.
- In BIOS, turn on EXPO (sometimes called DOCP/XMP depending on firmware).
- Use EXPO I first; EXPO II is more aggressive and may require tuning.  
  EXPO I is the more stable default; only use EXPO II if you're comfortable with tuning.

### 🚀 3. PCIe 4.0 NVMe SSD Optimization
- Install the Samsung 990 Pro in the **M.2_1** slot (connected directly to CPU).
- Ensure the M.2 slot is set to PCIe Gen4 mode in BIOS for full speed.

### 🔌 4. Apple Studio Display Support
- Connect only the USB-C to DisplayPort adapter during initial boot.
- If no display:
  - Update GPU drivers in Safe Mode
  - Boot with HDMI first, then switch back to USB-C
  - Try alternate USB-C ports on GPU

### 🌬️ 5. Cooling and Airflow
- Noctua NH-U12A provides excellent CPU cooling.
- Confirm that the Arctic F12 case fan is installed correctly:
  - Front = intake / Rear or top = exhaust
- Use HWMonitor or CoreTemp to verify temps after install.

### 💽 6. Windows USB Installation Notes
- Format the Corsair Flash Voyager USB drive as **FAT32** before flashing.
- Use GPT/UEFI mode during install (avoid MBR/Legacy).
- Use Microsoft’s Media Creation Tool for compatibility.

---

## 💾 2. OS Installation (Windows 11 Pro)
- [X ] Download and flash Windows 11 Pro using Media Creation Tool
- [X ] Install to primary SSD (NVMe preferred)
- [X ] Skip Microsoft account sign-in (use local account)
-
---

## ⚙️ 3. Post-Install Configuration
- [ ] Update Windows to latest version
- [ ] Install GPU drivers (NVIDIA or AMD)
- [ ] Enable BitLocker (only if required)
- [ ] Power settings: set to "High Performance"
- [ ] Disable sleep for dev sessions

---

## 🌐 4. Networking & Security
- [ ] Connect to Ethernet or secure Wi-Fi
- [ ] Install:
  - Chrome & Firefox
  - 7zip
  - Windows Terminal
  - Microsoft Defender (ensure it's active)
- [ ] Disable telemetry (via Settings or O&O ShutUp10++)

### 💤 4a. Enable Wake-on-LAN (optional)
1. **BIOS** – `Advanced ▸ APM` → *Enable* **Wake-on-LAN**  
2. **Windows NIC** – Device Manager ▸ *Realtek 2.5 GbE* → **Properties**  
   * Power Management tab:  
     * ☑ Allow this device to wake the computer  
     * ☑ Only allow a magic packet…  
   * Advanced tab: set **Wake on Magic Packet** → **Enabled**  
3. **Sleep, don’t shut down** – end sessions by choosing *Sleep* so WoL can bring the PC back.  
4. **Test from Mac**  
   ```bash
   brew install wakeonlan      # one-time  
   wakeonlan <MAC-address>     # wakes PC in ~2 s
   ```  
5. Optional: script “wake + RDP” or use a Mac WoL utility for one-click open.

---

### 🖥️ 4b. Enable Remote Desktop Access (for Mac RDP)
To allow access from your Mac using the Microsoft Remote Desktop app:
1. **Enable Remote Desktop**
   - Go to `Settings ▸ System ▸ Remote Desktop`
   - Toggle **Enable Remote Desktop** → ON
   - Confirm network firewall access when prompted

2. **Verify User Permissions**
   - Ensure your user account (e.g. `PMB`) is listed under **Remote Desktop Users**
   - If not, click *Select Users…* and add it

3. **Find PC Name or IP Address**
   - Open `Settings ▸ System ▸ About` → Find **Device name**
   - Or use `ipconfig` in Command Prompt to get **IPv4 address** (e.g., 192.168.1.42)

4. **Test from Mac**
   - Open **Microsoft Remote Desktop** on your Mac
   - Add a new PC:
     - **PC Name:** Use device name or IP address
     - **Credentials:** Use saved Windows login (e.g. PMB)
     - **Optional:** Enable **Connect to an admin session** for full session continuity

5. **Firewall Check**
   - Remote Desktop should auto-configure the firewall
   - If blocked, allow `Remote Desktop` through `Control Panel ▸ Windows Defender Firewall ▸ Allow an app`

6. **Router Consideration (local only)**
   - This setup assumes both Mac and PC are on the same local network
   - For external access, port forwarding and dynamic DNS are required (not recommended yet)

This enables fast, stable RDP sessions from your Mac to the new PC. Make sure the PC remains powered on or in Sleep mode with Wake-on-LAN enabled for convenience.

---

## 🧰 5. Developer Toolchain
### Essential Installs:
- [ ] **Visual Studio 2022**
- [ ] **.NET 8 SDK** – Download from [official site](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) and verify via `dotnet --list-sdks`
  - .NET 4.8, .NET 6+, ASP.NET + Web Dev, Desktop Dev with C#
- [ ] **SQL Server 2022 Express**
  - With SQL Server Management Studio (SSMS)
- [ ] **Git for Windows** + GitHub Desktop
- [ ] **WSL2** (Ubuntu, optional for AI work)
- [ ] **Python 3.11+** via Microsoft Store
- [ ] **Node.js** LTS (if needed for frontend packages)
- [ ] **Docker Desktop** (if containerization is used later)

---

## 🧠 6. AI + Data Tools
- [ ] **Anaconda or Miniconda** (Python environment mgmt)
- [ ] **VS Code** with:
  - Python extension
  - Jupyter extension
  - C# extension
- [ ] **ChatGPT Desktop (optional)**
- [ ] **Cursor IDE (optional)**

---

## 🗃️ 7. Project Directories
- [ ] Create base dev directory: `Z:\Desktop\Production`
- [ ] Create folders:
  - `AI_PROJECTS`
  - `PMB_MODERNIZATION`
  - `.aidocs` (for AI context docs)

---

## 🔐 8. Security & Backups
- [ ] Enable Windows Security or install preferred AV
- [ ] Set up backup strategy:
  - Consider Duplicati or Robocopy script to external drive
  - Schedule daily snapshot to NAS or external disk

---

## ✅ 9. Final Checklist
- [ ] Confirm all drivers and tools are working
- [ ] Ensure VS Code and Visual Studio open projects
- [ ] Test AI workflow (e.g., Jupyter Notebook runs)
- [ ] Snapshot the final system image (optional)

---