# ASUS B650M-E WiFi Setup Cheat Sheet

## Hardware Installation (Install First) 🔧
- **DDR5 Memory**: Install 2x32GB in **slots 2 and 4** (leave slots 1 and 3 empty)
- **Samsung 990 Pro**: Install in **M.2_1 slot** (CPU-direct connection)
- **RTX 4070 Super**: Install in **top PCIe x16 slot** (closest to CPU)
- **CPU Power**: Connect 8-pin CPU power connector
- **Motherboard Power**: Connect 24-pin main power connector

## Prerequisites ✅
- [ ] USB drive with A5463.CAP in root directory
- [ ] Samsung 990 Pro installed in **M.2_1 slot** (CPU-direct connection)
- [ ] All components properly connected
- [ ] Stable power supply

---

## Phase 1: BIOS Update (Version 3067)

### Step 1: First Boot
1. Power on system
2. Wait for memory training (30-60 seconds, may cycle 2-3 times)
3. When ASUS splash screen appears → **Press DEL** immediately
4. Enter BIOS setup

### Step 2: BIOS Update Process
1. Navigate to **"Tools"** or **"Advanced"** menu
2. Select **"ASUS EZ Flash 3"** utility
3. Select your USB drive
4. Choose **A5463.CAP** file
5. Confirm update (takes 5-10 minutes)
6. System will restart automatically when complete

### Step 3: Stability Testing
1. Boot system several times (3-5 cycles)
2. Enter BIOS to verify:
   - Memory detected as 64GB
   - Running at 4800MHz (default)
   - No error messages
3. **If stable → proceed to Windows installation**

---

## Phase 2: Enable EXPO Profile (Before Windows)

### Step 1: Access Memory Settings (Immediately After BIOS Update)
1. **Stay in BIOS** after update completes
2. Navigate to **"AI Tweaker"** or **"Overclocking"** section
3. Find **"Memory"** or **"DRAM"** settings

### Step 2: Enable EXPO
1. Locate **"EXPO"** option
2. Select **"EXPO I"** (conservative profile)
3. **Save & Exit** BIOS
4. System will restart with optimized memory

### Step 3: Verify Before Windows
1. Boot to BIOS again (press DEL)
2. Confirm memory running at EXPO speeds
3. **If stable → proceed to Windows installation**

---

## Advanced Storage Settings (if needed)
- PCIe 4.0 should auto-enable for M.2_1 slot
- If needed: BIOS → Advanced → PCIe Configuration → Set M.2_1 to Gen4

---

## Troubleshooting
- **BIOS update fails**: Use BIOS Flashback button if available
- **EXPO boot issues**: Power off completely, then power on (don't restart)
- **Memory not detected**: Reseat RAM in slots 2 and 4
- **System won't boot with EXPO**: Clear CMOS, start over with EXPO I

---

## Success Indicators ✅
- [ ] BIOS version shows 3067
- [ ] 64GB memory detected
- [ ] Memory running at EXPO speeds (5600MHz+) **before Windows install**
- [ ] AMD Virtualization (SVM) = Enabled
- [ ] IOMMU = Enabled  
- [ ] Secure Boot = Enabled
- [ ] Samsung 990 Pro detected in M.2_1
- [ ] Ready for Windows installation with optimal settings

**Remember: Change one thing at a time for easier troubleshooting!**