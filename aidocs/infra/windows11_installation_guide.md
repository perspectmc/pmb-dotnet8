# Windows 11 Pro Installation Guide
## Development-Optimized Setup for Maximum Performance

---

## Prerequisites ✅
- [ ] BIOS updated to version 3067 with EXPO and virtualization enabled
- [ ] Hardware validation completed successfully
- [ ] USB drive with Windows 11 Pro installer ready
- [ ] All components properly installed and detected

---

## Installation Process

### 1. Boot from USB Drive
- Insert Windows 11 Pro USB installer
- Boot from USB (F8 or F12 during startup)
- Select UEFI boot option (not Legacy/MBR)

### 2. Language and Region Settings
- **Language:** English (United States)
- **Time/Currency:** English (United States)
- **Keyboard:** US QWERTY
- **Click Next**

⚠️ **Avoid extra language packs** - reduces bloat and download time

### 3. Installation Type
- **Click "Install Now"**
- **Skip "I don't have a product key"** (Windows 11 Pro will activate automatically)
- **Select "Windows 11 Pro"** (not Home)

### 4. License Agreement
- **Accept license terms**
- **Click Next**

### 5. Installation Method
- **Select "Custom: Install Windows only (advanced)"**
- **Choose your Samsung 990 Pro NVMe drive** (will appear as "Unallocated Space")
- **Select the unallocated space** and click Next
- Windows creates GPT partitions automatically

⚠️ **New drives show as unallocated** - no partitions to delete on brand new SSDs

---

## Initial Windows Setup (OOBE)

### 6. Region and Keyboard
- **Confirm Country/Region:** United States
- **Confirm Keyboard Layout:** US
- **Skip "Add a second keyboard layout"**

### 7. Network Setup - CRITICAL STEP
**🚫 SKIP NETWORK CONNECTION INITIALLY**
- **Disconnect ethernet cable** or select "I don't have internet"
- **Click "Continue with limited setup"**

**Why skip network:** Allows local account creation without forcing Microsoft account

### 8. Account Creation
- **Select "Set up for personal use"** (not work/school)
- **Choose "Offline account"** or **"Limited experience"**
- **Create local account:**
  - Username: `PMB` (or your preference)
  - Strong password
  - Security questions (required)

⚠️ **Avoid Microsoft account initially** - maintain full control over system

### 9. Privacy Settings - DISABLE EVERYTHING
**Location Services:**
- [ ] **Location for this device = OFF**
- [ ] **Location history = OFF**
- [ ] **Find my device = OFF**

**Diagnostic Data:**
- [ ] **Send diagnostic data = Required diagnostic data only**
- [ ] **Improve inking and typing = OFF**
- [ ] **Tailored experiences = OFF**

**Online Speech Recognition:**
- [ ] **Online speech recognition = OFF**

**Advertising:**
- [ ] **Let apps use advertising ID = OFF**

### 10. Additional Privacy Options
**If prompted for more settings:**
- [ ] **Cortana = Skip/Decline**
- [ ] **Activity history = OFF**
- [ ] **Digital Assistant = Decline**
- [ ] **Office 365 = Skip trial**
- [ ] **OneDrive = Skip for now**
- [ ] **Xbox = Skip/Decline**
- [ ] **Use phone to finish setup = Skip**

---

## Post-Installation Immediate Setup

### 11. Connect to Network
- **Now connect ethernet or WiFi**
- **Check for Windows Updates**
- **Install critical security updates**

### 12. Essential System Configuration

**Power Settings:**
- Control Panel → Power Options → **High Performance**
- Advanced settings → **Never sleep, never hibernate**

**File Explorer Settings:**
- View → Options → **Show file extensions**
- View → Options → **Show hidden files**
- View → **Uncheck "Hide protected operating system files"**

**Windows Defender Configuration:**
- Windows Security → Virus & threat protection
- **Add exclusion for future development folders**
- Settings → **Cloud-delivered protection = OFF** (for dev performance)

### 13. Enable Developer Mode
- Settings → Privacy & Security → **For developers**
- **Developer Mode = ON**
- **Accept warning and restart if prompted**

**Benefits:** Enables symlinks, sideloading, PowerShell script execution

### 14. Configure Windows Updates
- Settings → Windows Update → **Advanced options**
- **Receive updates for other Microsoft products = OFF**
- **Download updates over metered connections = OFF**
- **Restart this device ASAP = OFF**

**Optional Features:**
- Optional features → **Pause feature updates for 1 year**
- **Install security updates immediately, delay feature updates**

---

## Critical Windows Features Setup

### 15. Enable Required Windows Features
**Programs and Features → Turn Windows features on/off:**

**For Future Development:**
- [x] **.NET Framework 3.5** (includes .NET 2.0 and 3.0)
- [x] **.NET Framework 4.8 Advanced Services**
- [x] **Internet Information Services (IIS)**
  - [x] World Wide Web Services
  - [x] Application Development Features → ASP.NET 4.8
- [x] **Windows Subsystem for Linux** (if needed)

**For Future Hyper-V (Phase 2):**
- [ ] **Hyper-V** (leave unchecked for now - Phase 2)
- [x] **Windows Hypervisor Platform** (prepare for future)

### 16. User Account Configuration
- Settings → Accounts → **Sign-in options**
- **Password = Keep current**
- **PIN = Skip** (not needed for development)
- **Windows Hello = Skip** (not needed)

**User Account Control:**
- Control Panel → User Accounts → **Change User Account Control settings**
- **Set to "Notify me only when apps try to make changes"**
- **Don't dim desktop** (for development efficiency)

---

## Network and Security Configuration

### 17. Windows Firewall Setup
- Control Panel → Windows Defender Firewall
- **Leave enabled** but prepare for development
- **Advanced settings** → **Inbound Rules**
- **Note:** Will configure specific rules for IIS/SQL Server in development setup phase

### 18. Network Profile
- Settings → Network & Internet → **Ethernet/WiFi**
- **Network profile = Private** (enables file sharing for development)

---

## Installation Validation

### 19. System Information Check
**Run `msinfo32` to verify:**
- [x] OS: Windows 11 Pro
- [x] System Type: x64-based PC
- [x] Processor: AMD Ryzen 9 7900 (12 cores)
- [x] Installed Physical Memory: 64.0 GB
- [x] Total Virtual Memory: >64 GB
- [x] BIOS Mode: UEFI
- [x] Secure Boot State: On

### 20. Device Manager Verification
**Check all hardware detected:**
- [x] CPU: AMD Ryzen 9 7900
- [x] Memory: 64GB DDR5 at EXPO speeds
- [x] Storage: Samsung 990 Pro 2TB
- [x] GPU: NVIDIA GeForce RTX 4070 Super
- [x] Network: Realtek 2.5Gb Ethernet + WiFi 6

---

## Next Steps Preparation

### 21. Create System Restore Point
- Control Panel → System → **System Protection**
- **Create restore point** → Name: "Fresh Windows 11 Pro Install"

### 22. Download Essential Browsers
**Using Edge (one time only):**
- Download **Google Chrome** installer
- **Set Chrome as default browser**
- **Pin Chrome to taskbar**

---

## Success Indicators ✅
- [ ] Windows 11 Pro activated successfully
- [ ] Local account created (no Microsoft account)
- [ ] All privacy settings disabled
- [ ] Developer Mode enabled
- [ ] .NET Framework and IIS features installed
- [ ] All hardware detected in Device Manager
- [ ] Chrome installed and set as default
- [ ] System restore point created
- [ ] Ready for Phase 2: Debloat and optimization

---

## ⚠️ Important Notes

**Don't Install Yet:**
- Development tools (Visual Studio, Git, etc.) - **Phase 3**
- Hyper-V virtualization - **Phase 2**
- Additional software - **Phase 3**

**Next Phases:**
1. **Phase 2:** Windows debloat and performance optimization
2. **Phase 3:** Development tools installation
3. **Phase 4:** Hyper-V server setup

**Troubleshooting:**
- If activation fails: Settings → Activation → Troubleshoot
- If hardware not detected: Check BIOS settings, reinstall drivers
- If performance issues: Run Windows Performance Toolkit

---

**Installation Complete!** 
Ready for Windows debloat and optimization phase.