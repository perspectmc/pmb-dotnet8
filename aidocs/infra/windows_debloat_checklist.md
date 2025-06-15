# Windows 11 Pro Debloat Checklist
## Enterprise Post-Installation Cleanup for Maximum Performance

---

## ⚡ Business Advantage
**Clean, fast Windows before installing expensive development tools**
- Removes Microsoft bloatware consuming CPU/RAM
- Prevents startup delays and background services
- Locks down settings to prevent re-installation
- Creates stable foundation for development environment

---

## 📋 Phase 1: Manual Bloatware Removal

### Remove Unwanted Microsoft Apps
**Settings → Apps → Installed apps**

- [ ] **Xbox Console Companion** (Search "Xbox")
- [ ] **Xbox Game Bar** 
- [ ] **Xbox Live** 
- [ ] **Microsoft Teams (Chat)** (NOT Teams for work)
- [ ] **Get Help**
- [ ] **Microsoft Tips**
- [ ] **Mixed Reality Portal**
- [ ] **3D Viewer**
- [ ] **Paint 3D**
- [ ] **Music**
- [ ] **Movies & TV**
- [ ] **People**
- [ ] **Mail and Calendar** (if using Outlook)
- [ ] **Maps**
- [ ] **Weather**
- [ ] **Your Phone**
- [ ] **Microsoft News**
- [ ] **Microsoft Solitaire Collection**
- [ ] **Candy Crush** (if present)
- [ ] **Disney Magic Kingdoms** (if present)

⚠️ **Keep These:** Windows Security, Microsoft Store, Edge Browser (system dependency)

---

## 🚀 Phase 2: Startup Optimization

### Disable Startup Bloatware
**Task Manager → Startup tab**

- [ ] **Microsoft Teams (Chat)** → Disable
- [ ] **Xbox Live** → Disable  
- [ ] **Spotify** → Disable (if installed)
- [ ] **Adobe Updater** → Disable (if present)
- [ ] **Microsoft OneDrive** → Disable (unless needed for business)
- [ ] **Office applications** → Disable (unless immediate use required)

**Keep Enabled:** Windows Security, Audio drivers, GPU drivers

---

## ⚙️ Phase 3: Windows Services Configuration

### Disable Unnecessary Services
**Services.msc**

- [ ] **Xbox Live Auth Manager** → Disabled
- [ ] **Xbox Live Game Save** → Disabled
- [ ] **Xbox Live Networking Service** → Disabled
- [ ] **Windows Mixed Reality Service** → Disabled
- [ ] **Touch Keyboard and Handwriting Panel Service** → Manual (if no touchscreen)
- [ ] **Fax Service** → Disabled
- [ ] **Windows Media Player Network Sharing** → Disabled

⚠️ **Don't Touch:** Audio services, Network services, Security services, System services

---

## 🛠️ Phase 4: Glary Utilities Installation

### Download and Install
1. **Download Glary Utilities Pro** from official website
2. **Install with custom settings:**
   - [ ] Decline browser toolbar additions
   - [ ] Decline homepage changes
   - [ ] Enable auto-scan schedule (weekly)

### Initial Glary Setup
- [ ] **Run Registry Cleaner** (backup registry first)
- [ ] **Run Disk Cleaner** (select all temp files)
- [ ] **Run Startup Manager** (verify our manual changes)
- [ ] **Set maintenance schedule** (weekly scans)

---

## 🔒 Phase 5: Prevention Settings

### Prevent Microsoft Bloatware Reinstallation
**Settings → Privacy & Security → For developers**
- [ ] **Enable Developer Mode** (prevents some auto-installs)

**Group Policy Editor (gpedit.msc)**
- [ ] **Computer Config → Admin Templates → Windows Components → Store**
- [ ] **Enable "Turn off automatic download and install of updates"**

**Registry Prevention (Advanced)**
- [ ] **Block Xbox services reinstallation**
- [ ] **Block unwanted app suggestions**

### Windows Update Settings
**Settings → Windows Update → Advanced options**
- [ ] **Disable "Get the latest updates as soon as they're available"**
- [ ] **Set active hours** (prevent forced restarts during work)
- [ ] **Pause updates for 1 week** (test stability first)

---

## ✅ Phase 6: Performance Verification

### System Performance Check
- [ ] **Boot time under 30 seconds** (from BIOS splash to desktop)
- [ ] **Task Manager shows <15 startup apps**
- [ ] **RAM usage under 4GB at idle**
- [ ] **No Xbox processes in Task Manager**
- [ ] **No unwanted background apps**

### Glary Utilities Validation
- [ ] **Registry scan shows minimal issues**
- [ ] **Startup manager matches our manual settings**
- [ ] **Disk cleaner finds minimal junk**
- [ ] **Scheduled maintenance configured**

---

## 🎯 Success Criteria
✅ **Clean system ready for development tools**
✅ **Fast boot and startup performance**
✅ **Minimal background processes**
✅ **Prevented bloatware reinstallation**
✅ **Automated maintenance configured**

---

## ⏭️ Next Phase Ready
**Development Environment Setup**
- Visual Studio 2022
- Git and development tools
- WSL2 for Linux development
- Docker Desktop
- Database tools

**Time Investment:** 45-60 minutes for complete debloat
**Performance Gain:** 20-30% faster boot, reduced RAM usage, cleaner system