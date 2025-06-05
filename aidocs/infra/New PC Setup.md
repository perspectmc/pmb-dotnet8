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

---


# 🖥️ New PC Setup Instructions for AI + Dev Work

## 👤 Owner: Colin McAllister  
**Purpose:** Optimized workstation for AI development, .NET modernization, and legacy system work (Perspect Medical Billing).

---

## 📦 1. Hardware Assembly
- [ ] Assemble components carefully following manufacturer instructions.
- [ ] Ensure cable management allows for airflow.
- [ ] Boot into BIOS to confirm:
  - CPU and RAM are recognized
  - NVMe / SSD detected
  - Fan curves look appropriate
  - Enable XMP profile for RAM if applicable

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
- [ ] Download and flash Windows 11 Pro using Media Creation Tool
- [ ] Install to primary SSD (NVMe preferred)
- [ ] Skip Microsoft account sign-in (use local account)
- [ ] Set device name: `AI-CORE-PC`
- [ ] Initial user account: `Colin` (Admin rights)

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

---

## 🧰 5. Developer Toolchain
### Essential Installs:
- [ ] **Visual Studio 2022**
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


---