# Hardware Performance Validation Checklist
## Post-Windows Installation Verification

## Benchmarking Tools Download List 📥
- **Glary Utilities Pro**: glarysoft.com ⭐ (install first!)
- **CrystalDiskMark**: crystalmark.info
- **CPU-Z**: cpuid.com  
- **GPU-Z**: techpowerup.com (optional)
- **HWiNFO64**: hwinfo.com
- **Samsung Magician**: samsung.com/semiconductor/minisite/ssd/download/tools/
- **Ryzen Master**: amd.com/en/technologies/ryzen-master
- **NVIDIA App**: nvidia.com (official GPU management)

---

## Windows Optimization & Maintenance ✅
**Install immediately after Windows setup, before other tools:**

1. **Glary Utilities Pro** ($12-20/year with discounts)
   - Download from glarysoft.com
   - Complete CleanMyMac equivalent for Windows
   - Comprehensive software removal tracking
   - Registry cleaning and system optimization
   - Install BEFORE verification tools for clean tracking

**Why install first:** Glary monitors all subsequent software installations and maintains complete removal records, ensuring clean uninstalls when needed.

---

## Memory Performance ✅
**Check DDR5 is running at EXPO speeds:**

1. **Task Manager** → Performance → Memory
   - Speed should show **5600MHz+** (not 4800MHz)
   - Available: ~64GB
   - Form factor: DDR5

2. **CPU-Z** (free download from cpuid.com)
   - Memory tab should show:
   - DRAM Frequency: ~2800MHz (DDR5-5600 shows as 2800MHz)
   - Timings: Should match EXPO profile

**Expected Results:** Memory running 15-20% faster than JEDEC 4800MHz

**🎯 Success Target:** 5600MHz+ DDR5 speed  
**🚩 Red Flag:** Memory at 4800MHz = EXPO not enabled properly

---

## Storage Performance ✅
**Verify Samsung 990 Pro PCIe 4.0 speeds:**

1. **CrystalDiskMark** (free from crystalmark.info)
   - **Target:** Sequential Read ~7,000 MB/s
   - **Target:** Sequential Write ~6,900 MB/s
   - **Minimum acceptable:** 6,000+ MB/s

2. **Samsung Magician** (official Samsung tool)
   - Download from Samsung website
   - Shows drive health, temperature, firmware
   - Built-in benchmark tool

3. **Device Manager verification:**
   - Disk drives → Samsung SSD 990 PRO → Properties
   - Should show PCIe interface

**Expected Results:** Near-maximum NVMe performance

**🎯 Success Target:** 6,000+ MB/s NVMe speed  
**🚩 Red Flag:** SSD under 6,000 MB/s = PCIe 4.0 not active

---

## CPU Performance ✅
**Verify Ryzen 9 7900 is running optimally:**

1. **Task Manager** → Performance → CPU
   - Base speed: 3.7 GHz
   - Max boost should reach ~5.4 GHz under load
   - All 12 cores visible

2. **Ryzen Master** (free from AMD)
   - Download from AMD website
   - Shows real-time boost clocks
   - Temperature monitoring
   - PBO (Precision Boost Overdrive) status

**Expected Results:** CPU reaching advertised boost speeds

**🎯 Success Target:** Boost to 5.0+ GHz under load  
**🚩 Red Flag:** CPU not boosting = Check motherboard power delivery

---

## GPU Performance ✅
**Verify RTX 4070 Super configuration:**

1. **NVIDIA App** (official - replaces GeForce Experience)
   - Download from nvidia.com
   - Shows RTX 4070 Super detection
   - Driver version verification and updates
   - GPU performance monitoring

2. **GPU-Z** (free from techpowerup.com) - optional
   - Shows PCIe x16 connection details
   - GPU running at full speed verification
   - Memory and core clock information

3. **NVIDIA Control Panel** (comes with drivers)
   - System Information → Display tab
   - Hardware configuration verification

**Expected Results:** GPU detected with full PCIe bandwidth

**🎯 Success Target:** Full PCIe x16 detection  
**🚩 Red Flag:** Missing hardware = Check BIOS detection

---

## System Stability ✅
**Quick stability verification:**

1. **Windows System Information**
   - Run `msinfo32` command
   - Verify all hardware detected
   - Check for any hardware conflicts

2. **Event Viewer** check
   - Windows Logs → System
   - Look for critical errors or warnings
   - Should be mostly clean after setup

3. **Temperature check**
   - **HWiNFO64** (free) for comprehensive monitoring
   - CPU: Should stay under 85°C under load
   - GPU: Should stay under 80°C under load
   - NVMe: Should stay under 70°C

**Expected Results:** Clean system with normal temperatures

**🎯 Success Target:** All components under thermal limits (CPU <85°C, GPU <80°C, NVMe <70°C)  
**🚩 Red Flag:** High temperatures = Check cooler installation

---

## Network Performance ✅
**Verify WiFi 6 and Ethernet:**

1. **WiFi 6 verification**
   - Device Manager → Network adapters
   - Should show WiFi 6 AX capability
   - Connect to WiFi 6 router for best speeds

2. **Ethernet verification**
   - Should show 2.5Gb Ethernet adapter
   - Test with ethernet cable for maximum speed

**Expected Results:** Modern network connectivity at full speeds

**🎯 Success Target:** WiFi 6 and 2.5Gb Ethernet working properly  
**🚩 Red Flag:** No errors in Event Viewer for clean system validation

---

## Final System Validation ✅
If all individual tests show success targets met, your system is running at optimal performance!