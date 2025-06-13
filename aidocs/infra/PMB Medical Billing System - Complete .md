# PMB Medical Billing System - Complete Migration SOP
**From .NET Framework to .NET 8 Migration Guide**  
*Colin McAllister - Solo Developer Edition*

---

## 📋 Table of Contents
1. [Hardware Build & Windows Setup](#step-0-hardware-build--windows-setup)
2. [Development Environment Setup](#step-1-development-environment-setup)
3. [Study Materials & Migration Planning](#step-2-study-materials--migration-planning)
4. [Test Server Environment](#step-3-test-server-environment-setup)
5. [Database Migration Procedures](#step-4-database-migration-procedures)
6. [Application Deployment Workflows](#step-5-application-deployment-workflows)
7. [Migration Study Guide](#migration-study-guide)

---

## Step 0: Hardware Build & Windows Setup

### Hardware Assembly Checklist
**Your Specs:** AMD Ryzen 9 7900, RTX 4070 Super, 64GB DDR5, 2TB NVMe SSD

- [ ] **Assemble PC Components**
  - Install AMD Ryzen 9 7900 + Noctua NH-U12A cooler
  - Install 64GB DDR5 RAM in correct slots (2 & 4)
  - Install RTX 4070 Super in top PCIe slot
  - Install Samsung 990 Pro NVMe in M.2_1 slot (CPU direct)
  - Connect all power cables (24-pin, CPU, GPU)

- [ ] **BIOS Configuration**
  ```
  What: Optimize hardware performance for development
  Why: Maximize RAM speed, confirm all components detected
  
  Steps:
  1. Boot to BIOS (DEL key during startup)
  2. Enable EXPO Profile for DDR5 RAM (gets full 6000MHz speed)
  3. Confirm CPU/RAM/NVMe all detected correctly
  4. Set boot priority: USB first, then NVMe
  5. Save & Exit
  ```

### Windows 11 Pro Installation (Server-Ready)

- [ ] **Create Windows 11 Pro USB**
  ```
  What: Create bootable Windows installation media
  Why: Need Pro edition for IIS and Hyper-V features
  
  Steps:
  1. Download Microsoft Media Creation Tool
  2. Format USB drive as FAT32
  3. Select "Windows 11 Pro" (NOT Home edition)
  4. Create installation media
  ```

- [ ] **Install Windows 11 Pro**
  ```
  What: Clean OS installation with development-friendly settings
  Why: Fresh start optimized for .NET development and testing
  
  Steps:
  1. Boot from USB (F12 during startup)
  2. Select "Windows 11 Pro" edition (CRITICAL)
  3. Use GPT/UEFI partition scheme (not MBR/Legacy)
  4. Skip Microsoft account creation (use local account)
  5. Username: Colin
  6. Computer name: AI-CORE-PC
  7. Complete initial setup
  ```

### Post-Install Windows Configuration (Server Features)

**Why we need these features:** Your PC will serve as both a development machine AND a local test server. This allows you to test your .NET 8 medical billing application locally before deploying to production.

- [ ] **Enable Windows Features (GUI Method - Recommended)**
  ```
  What: Enable built-in Windows server capabilities
  Why: Transform your PC into a development + test server
  
  Steps:
  1. Press Windows key + R (opens Run dialog)
  2. Type: appwiz.cpl
  3. Press Enter (opens Programs and Features)
  4. Click "Turn Windows features on or off" (left sidebar)
  5. Wait for feature list to load
  ```

- [ ] **Enable Hyper-V (Virtual Machine Host)**
  ```
  What: Microsoft's built-in virtualization platform
  Why: Create isolated test environments for different configurations
  
  Check this box:
  ☑ Hyper-V
    ☑ Hyper-V Management Tools
    ☑ Hyper-V Platform
  
  Use case: Test your app on "clean" Windows Server VMs
  ```

- [ ] **Enable Internet Information Services (IIS Web Server)**
  ```
  What: Microsoft's enterprise web server (like Apache/Nginx)
  Why: Host your .NET 8 medical billing app locally for testing
  
  Check these boxes:
  ☑ Internet Information Services
    ☑ Web Management Tools
      ☑ IIS Management Console (GUI to manage websites)
    ☑ World Wide Web Services
      ☑ Common HTTP Features
        ☑ Default Document
        ☑ Directory Browsing  
        ☑ HTTP Errors
        ☑ Static Content
      ☑ Application Development Features
        ☑ ASP.NET 4.8 (CRITICAL - matches current production)
        ☑ .NET Extensibility 4.8 (required for compatibility)
      ☑ Health and Diagnostics
        ☑ HTTP Logging (track website access)
      ☑ Security
        ☑ Request Filtering (basic security)
  
  Result: Your PC can host websites like a real web server
  ```

- [ ] **Apply Changes & Restart**
  ```
  What: Install the selected Windows features
  Why: Actually enable the server capabilities
  
  Steps:
  1. Click "OK" button
  2. Wait for Windows to download/install features (5-10 minutes)
  3. Click "Restart now" when prompted
  4. PC will reboot and complete configuration
  ```

### Configure Performance & Security Settings

- [ ] **Configure Power & Performance**
  ```powershell
  # Set High Performance power plan (for development workstation)
  powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
  
  # Disable sleep/hibernate (prevents interruptions during development)
  powercfg /change standby-timeout-ac 0
  powercfg /change hibernate-timeout-ac 0
  ```

- [ ] **Configure Windows Firewall for Development**
  ```powershell
  # Allow IIS through firewall (for local web server testing)
  netsh advfirewall firewall add rule name="IIS HTTP" dir=in action=allow protocol=TCP localport=80
  netsh advfirewall firewall add rule name="IIS HTTPS" dir=in action=allow protocol=TCP localport=443
  
  # Allow SQL Server through firewall (for database connections)
  netsh advfirewall firewall add rule name="SQL Server" dir=in action=allow protocol=TCP localport=1433
  ```

- [ ] **Prevent Auto-Restart During Development**
  ```powershell
  # Prevent Windows Updates from interrupting development sessions
  reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU" /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 1 /f
  ```

### Essential Software Installation

- [ ] **Install Core Development Tools**
  ```powershell
  # Windows Terminal (modern command line)
  winget install Microsoft.WindowsTerminal
  
  # PowerShell 7 (latest version)
  winget install Microsoft.PowerShell
  
  # File compression utility
  winget install 7zip.7zip
  
  # Web browser for testing
  winget install Google.Chrome
  ```

### Validation Steps

- [ ] **Test Server Features After Reboot**
  ```
  What: Confirm web server and virtualization are working
  
  Test IIS Web Server:
  1. Open web browser
  2. Go to http://localhost
  3. Expected: See "IIS Windows" welcome page
  4. If working: You now have a local web server!
  
  Test Hyper-V Manager:
  1. Start menu → type "Hyper-V Manager"
  2. Expected: Hyper-V Manager application opens
  3. If working: You can create virtual machines!
  ```

**Step 0 Status: ✅ Complete when all validations pass**

---

## Step 1: Development Environment Setup

### Prerequisites Installation

- [ ] **Install .NET 8 SDK**
  ```powershell
  # Install latest .NET 8 SDK
  winget install Microsoft.DotNet.SDK.8
  
  # Verify installation
  dotnet --version
  dotnet --list-sdks
  
  # Expected output: 8.x.x version number
  ```

- [ ] **Install Visual Studio 2022 Community**
  ```powershell
  # Install VS 2022 Community (free, fully featured)
  winget install Microsoft.VisualStudio.2022.Community
  ```
  
  **Required Workloads during installation:**
  - ✅ **ASP.NET and web development** (for web apps)
  - ✅ **.NET desktop development** (for Forms apps)
  - ✅ **Data storage and processing** (for SQL Server tools)

- [ ] **Install SQL Server 2022 Developer Edition**
  ```powershell
  # Download SQL Server 2022 Developer (free for development)
  # Manual installation required
  # Configure with:
  # - Default instance (MSSQLSERVER)
  # - Mixed Mode Authentication
  # - Enable TCP/IP protocol in SQL Configuration Manager
  ```

- [ ] **Install Database Management Tools**
  ```powershell
  # SQL Server Management Studio
  winget install Microsoft.SQLServerManagementStudio
  ```

- [ ] **Configure Git (if not already done)**
  ```powershell
  # Install Git for Windows
  winget install Git.Git
  
  # Configure Git globally
  git config --global user.name "Colin McAllister"
  git config --global user.email "colin@yourdomain.com"
  git config --global init.defaultBranch main
  git config --global core.autocrlf true
  ```

### Repository Setup

- [ ] **Create Development Directory Structure**
  ```powershell
  # Create organized development folders
  mkdir "C:\Users\Colin\Desktop\Production\PMB_MODERNIZATION"
  cd "C:\Users\Colin\Desktop\Production\PMB_MODERNIZATION"
  ```

- [ ] **Clone Your Development Repository**
  ```powershell
  # Clone the .NET 8 development repo
  git clone https://github.com/perspectmc/pmb-dotnet8.git
  cd pmb-dotnet8
  
  # Verify remote connection
  git remote -v
  
  # Should show: origin https://github.com/perspectmc/pmb-dotnet8.git
  ```

- [ ] **Create Feature Branch for Setup**
  ```powershell
  # Create and switch to development branch
  git checkout -b feature/dev-environment-setup
  git push -u origin feature/dev-environment-setup
  ```

### Database Setup

- [ ] **Test SQL Server Installation**
  ```powershell
  # Test SQL Server connection via command line
  sqlcmd -S localhost -E -Q "SELECT @@VERSION"
  
  # Expected: SQL Server version information
  # If fails: Check SQL Server Configuration Manager
  ```

- [ ] **Prepare for Database Restoration**
  ```sql
  -- In SQL Server Management Studio:
  -- 1. Connect to localhost with Windows Authentication
  -- 2. Restore PMB database from backup file
  -- 3. Note: Will configure connection strings later
  
  RESTORE DATABASE [PMB_Dev] FROM DISK = 'C:\path\to\pmb_backup.bak'
  WITH REPLACE,
  MOVE 'PMB_Data' TO 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PMB_Dev.mdf',
  MOVE 'PMB_Log' TO 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PMB_Dev.ldf'
  ```

### Visual Studio Configuration

- [ ] **Open PMB Solution in Visual Studio**
  ```
  What: Load your medical billing project for development
  Steps:
  1. Launch Visual Studio 2022
  2. File → Open → Project/Solution
  3. Navigate to: C:\Users\Colin\Desktop\Production\PMB_MODERNIZATION\pmb-dotnet8
  4. Open the .sln file
  5. Wait for project to load
  ```

- [ ] **Verify Project Structure & Build**
  ```
  What: Confirm project loads and compiles successfully
  Steps:
  1. View → Solution Explorer
  2. Expand project folders to see structure
  3. Build → Build Solution (Ctrl+Shift+B)
  4. Check Output window for any errors
  ```

- [ ] **Update Database Connection Strings**
  ```json
  // In appsettings.Development.json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=localhost;Database=PMB_Dev;Trusted_Connection=true;TrustServerCertificate=true;"
    }
  }
  ```

### Additional Development Tools

- [ ] **Install API Testing Tools**
  ```powershell
  # For testing MSB API integration
  winget install Postman.Postman
  
  # Enhanced text editor for quick edits
  winget install Notepad++.Notepad++
  ```

### Validation Tests

- [ ] **Test Complete Development Environment**
  ```powershell
  # Test .NET CLI functionality
  dotnet --info
  
  # Create test console app
  dotnet new console -n TestApp
  cd TestApp
  dotnet run
  cd ..
  rmdir /s TestApp
  
  # Expected: "Hello, World!" output
  ```

- [ ] **Test Database Connectivity**
  ```
  What: Verify PMB application can connect to database
  Steps:
  1. Run the PMB application in Visual Studio (F5)
  2. Check application startup logs
  3. Verify no database connection errors
  4. Test basic functionality (if available)
  ```

- [ ] **Test Git Workflow**
  ```powershell
  # Test git operations
  echo "# Development Environment Setup Complete" >> README.md
  git add README.md
  git commit -m "Initial development environment setup"
  git push
  
  # Expected: Successful push to GitHub
  ```

**Step 1 Status: ✅ Complete when all validations pass**

---

## Step 2: Study Materials & Migration Planning

### .NET Framework to .NET 8 Migration Overview

**Migration Strategy for PMB:** Gradual migration approach - migrate code to .NET 8 but initially deploy to .NET Framework 4.8 to match current production environment, then later upgrade production infrastructure.

### Key Migration Concepts

#### **What Changes in the Migration:**
- [ ] **Project File Format Transformation**
  ```xml
  <!-- Old .NET Framework .csproj (verbose) -->
  <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" />
    <PropertyGroup>
      <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
      <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
      <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
      <UseIISExpress>true</UseIISExpress>
      <Use64BitIISExpress />
      <IISExpressSSLPort />
      <IISExpressAnonymousAuthentication />
      <IISExpressWindowsAuthentication />
      <IISExpressUseClassicPipelineMode />
      <!-- Many more lines... -->
    </PropertyGroup>
    <ItemGroup>
      <Reference Include="System" />
      <Reference Include="System.Core" />
      <Reference Include="System.Web" />
      <Reference Include="System.Web.Mvc, Version=5.2.7.0" />
      <!-- Hundreds of manual references -->
    </ItemGroup>
  </Project>
  ```

  ```xml
  <!-- New .NET 8 .csproj (minimal) -->
  <Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
      <TargetFramework>net8.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>
    
    <ItemGroup>
      <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
      <PackageReference Include="Microsoft.AspNetCore.Identity" Version="8.0.0" />
    </ItemGroup>
  </Project>
  ```

- [ ] **Entity Framework Code Changes**
  ```csharp
  // OLD: EF6 DbContext Pattern
  public class PMBContext : DbContext
  {
      public PMBContext() : base("PMBConnection") 
      { 
          Database.SetInitializer<PMBContext>(null);
      }
      
      public DbSet<Patient> Patients { get; set; }
      public DbSet<Billing> Billings { get; set; }
      
      protected override void OnModelCreating(DbModelBuilder modelBuilder)
      {
          modelBuilder.Entity<Patient>()
              .HasKey(p => p.PatientId);
          
          modelBuilder.Entity<Billing>()
              .HasRequired(b => b.Patient)
              .WithMany(p => p.Billings)
              .HasForeignKey(b => b.PatientId);
      }
  }
  ```

  ```csharp
  // NEW: EF Core 8 DbContext Pattern
  public class PMBContext : DbContext
  {
      public PMBContext(DbContextOptions<PMBContext> options) : base(options) { }
      
      public DbSet<Patient> Patients { get; set; }
      public DbSet<Billing> Billings { get; set; }
      
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          modelBuilder.Entity<Patient>(entity =>
          {
              entity.HasKey(p => p.PatientId);
              entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
          });
          
          modelBuilder.Entity<Billing>(entity =>
          {
              entity.HasKey(b => b.BillingId);
              entity.HasOne(b => b.Patient)
                    .WithMany(p => p.Billings)
                    .HasForeignKey(b => b.PatientId);
          });
      }
  }
  ```

- [ ] **Authentication System Migration**
  ```xml
  <!-- OLD: web.config Forms Authentication -->
  <configuration>
    <system.web>
      <authentication mode="Forms">
        <forms loginUrl="~/Account/Login" timeout="2880" />
      </authentication>
      <authorization>
        <deny users="?" />
      </authorization>
      <membership defaultProvider="DefaultMembershipProvider">
        <providers>
          <add name="DefaultMembershipProvider" 
               type="System.Web.Providers.DefaultMembershipProvider" 
               connectionStringName="DefaultConnection" />
        </providers>
      </membership>
    </system.web>
    
    <connectionStrings>
      <add name="PMBConnection" 
           connectionString="Server=localhost;Database=PMB;Trusted_Connection=true;" />
    </connectionStrings>
  </configuration>
  ```

  ```json
  // NEW: appsettings.json
  {
    "ConnectionStrings": {
      "PMBConnection": "Server=localhost;Database=PMB;Trusted_Connection=true;TrustServerCertificate=true;"
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    },
    "Authentication": {
      "RequireConfirmedAccount": false,
      "DefaultPasswordOptions": {
        "RequiredLength": 8,
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireNonAlphanumeric": false
      }
    }
  }
  ```

  ```csharp
  // NEW: Program.cs Authentication Setup
  var builder = WebApplication.CreateBuilder(args);
  
  // Add Entity Framework
  builder.Services.AddDbContext<PMBContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("PMBConnection")));
  
  // Add Identity services
  builder.Services.AddDefaultIdentity<IdentityUser>(options => {
      options.SignIn.RequireConfirmedAccount = false;
      options.Password.RequiredLength = 8;
  })
  .AddEntityFrameworkStores<PMBContext>();
  
  builder.Services.AddControllersWithViews();
  
  var app = builder.Build();
  
  // Configure the HTTP request pipeline
  if (!app.Environment.IsDevelopment())
  {
      app.UseExceptionHandler("/Home/Error");
      app.UseHsts();
  }
  
  app.UseHttpsRedirection();
  app.UseStaticFiles();
  app.UseRouting();
  
  // Authentication & Authorization middleware
  app.UseAuthentication();
  app.UseAuthorization();
  
  app.MapControllerRoute(
      name: "default",
      pattern: "{controller=Home}/{action=Index}/{id?}");
  app.MapRazorPages();
  
  app.Run();
  ```

#### **Tools for Migration Analysis:**
- [ ] **Install .NET Upgrade Assistant**
  ```powershell
  # Microsoft's official migration tool
  dotnet tool install -g upgrade-assistant
  
  # Usage: Analyzes your project and suggests changes
  upgrade-assistant analyze .\YourProject.csproj
  ```

- [ ] **Install .NET Portability Analyzer**
  ```powershell
  # Analyzes compatibility between .NET versions
  dotnet tool install -g Microsoft.DotNet.ApiPortability.Tool
  
  # Usage: Checks API compatibility
  ApiPort analyze -f .NET8.0 -t net8.0 .\YourAssembly.dll
  ```

### Entity Framework 6 to EF Core Migration Guide

#### **Database-First vs Code-First Approach:**
EF Core supports three approaches: Code as source of truth (code-first), Database as source of truth (database-first), and Hybrid mapping. For existing applications, database-first approach often works best.

- [ ] **Current PMB Setup Analysis**
  ```
  Current: EF6 with 16 tables, medical billing data
  Target: EF Core 8 with same data structure
  
  Migration Steps:
  1. Generate EF Core models from existing database
  2. Update DbContext to EF Core syntax
  3. Migrate data annotations and fluent configurations
  4. Test all database operations
  ```

#### **EF6 to EF Core Key Changes:**
- [ ] **DbContext Constructor**
  ```csharp
  // EF6 Style
  public class PMBContext : DbContext
  {
      public PMBContext() : base("PMBConnection") { }
  }
  
  // EF Core Style
  public class PMBContext : DbContext
  {
      public PMBContext(DbContextOptions<PMBContext> options) : base(options) { }
      
      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
          // Configuration here or in Program.cs
      }
  }
  ```

- [ ] **Migration Commands Comparison**
  ```powershell
  # OLD: EF6 Commands (Package Manager Console only)
  PM> Enable-Migrations
  PM> Add-Migration InitialCreate
  PM> Update-Database
  PM> Update-Database -TargetMigration InitialCreate  # Rollback
  ```

  ```powershell
  # NEW: EF Core Commands (CLI and Package Manager Console)
  
  # .NET CLI (Cross-platform, recommended)
  dotnet ef migrations add InitialCreate
  dotnet ef database update
  dotnet ef database update InitialCreate  # Rollback
  dotnet ef migrations remove  # Remove last migration
  dotnet ef migrations list    # List all migrations
  
  # Package Manager Console (Visual Studio)
  PM> Add-Migration InitialCreate
  PM> Update-Database
  PM> Update-Database InitialCreate
  PM> Remove-Migration
  PM> Get-Migration
  ```

### Study Resources & Documentation

- [ ] **Essential Reading List**
  ```
  Priority 1 - Core Migration Guides:
  □ Microsoft: "Port from .NET Framework to .NET"
  □ Microsoft: "Port from EF6 to EF Core"
  □ Microsoft: "Migrate ASP.NET to ASP.NET Core"
  
  Priority 2 - Specific Technologies:
  □ ASP.NET Core Identity documentation
  □ EF Core migrations documentation
  □ .NET 8 breaking changes list
  
  Priority 3 - Deployment:
  □ "Host ASP.NET Core on Windows with IIS"
  □ "Publish an ASP.NET Core app to IIS"
  ```

- [ ] **Practical Migration Labs**
  ```
  Week 1: Project File Migration
  - Convert .csproj to SDK-style
  - Update NuGet packages
  - Resolve compilation errors
  
  Week 2: Database Layer Migration
  - Scaffold EF Core models from database
  - Update DbContext and configurations
  - Test all CRUD operations
  
  Week 3: Authentication Migration
  - Replace Forms Auth with Core Identity
  - Update login/logout flows
  - Test user authentication
  
  Week 4: Integration Testing
  - Test MSB API integration
  - Verify fax service connectivity
  - End-to-end application testing
  ```

**Step 2 Status: ✅ Complete when study plan is established**

---

## Step 3: Test Server Environment Setup

### Local Test Server Options

**Recommended:** Use your development PC as the test server initially, then optionally create a separate VM or cloud instance later.

#### **Option A: Local IIS Test Environment (Recommended)**

- [ ] **Configure IIS for .NET 8 Applications**
  ```
  What: Enable your PC to host .NET 8 web applications
  Why: Test deployment process without external servers
  
  Prerequisites: IIS already enabled in Step 0
  ```

- [ ] **Install .NET 8 Hosting Bundle for IIS**
  ```powershell
  # Download and install .NET 8 Hosting Bundle
  # This includes .NET 8 Runtime + ASP.NET Core Module for IIS
  
  # Download from: https://dotnet.microsoft.com/download/dotnet/8.0
  # Select: "Windows Server Hosting" (not just Runtime)
  
  # After installation, restart IIS
  iisreset
  ```

- [ ] **Create Test Website in IIS**
  ```
  What: Set up a dedicated website for PMB testing
  Steps:
  1. Open IIS Manager (Windows + R → inetmgr)
  2. Right-click "Sites" → "Add Website"
  3. Site name: PMB-Test
  4. Physical path: C:\inetpub\wwwroot\pmb-test
  5. Port: 8080 (to avoid conflicts with default site)
  6. Click OK
  ```

- [ ] **Configure Application Pool for .NET 8**
  ```
  What: Ensure IIS can run .NET 8 applications
  Steps:
  1. In IIS Manager, click "Application Pools"
  2. Find "PMB-Test" pool (created automatically)
  3. Right-click → "Advanced Settings"
  4. Set ".NET CLR Version" to "No Managed Code"
  5. Set "Enable 32-Bit Applications" to False
  6. Click OK
  ```

#### **Option B: Hyper-V Virtual Machine (Advanced)**

- [ ] **Create Windows Server VM (Optional)**
  ```
  What: Isolated test environment mimicking production
  Why: Test deployment in clean environment
  
  Requirements:
  - Hyper-V enabled (done in Step 0)
  - Windows Server 2022 ISO
  - 8GB+ RAM allocated to VM
  
  Note: This is optional and can be done later
  ```

### Test Server Software Configuration

- [ ] **Install SQL Server on Test Environment**
  ```
  For Local IIS: Use existing SQL Server instance
  For VM: Install SQL Server Express
  
  Configuration:
  - Mixed Mode Authentication
  - TCP/IP enabled
  - Firewall rules configured
  ```

- [ ] **Create Test Database**
  ```sql
  -- Create test database from production backup
  RESTORE DATABASE [PMB_Test] FROM DISK = 'C:\path\to\pmb_backup.bak'
  WITH REPLACE,
  MOVE 'PMB_Data' TO 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PMB_Test.mdf',
  MOVE 'PMB_Log' TO 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PMB_Test.ldf'
  
  -- Update any environment-specific data
  -- Sanitize sensitive information if needed
  ```

### Test Server Validation

- [ ] **Verify Test Environment**
  ```
  Test IIS Installation:
  1. Browse to http://localhost:8080
  2. Should see IIS default page or 404 (both indicate IIS working)
  
  Test .NET 8 Hosting:
  1. Create simple test app
  2. Deploy to test site
  3. Verify it runs correctly
  
  Test Database Connectivity:
  1. Connect SSMS to test database
  2. Verify all 16 tables present
  3. Test basic queries
  ```

**Step 3 Status: ✅ Complete when test environment validated**

---

## Step 4: Database Migration Procedures

### EF6 to EF Core Migration Steps

#### **Phase 1: Model Generation**

- [ ] **Scaffold EF Core Models from Existing Database**
  ```powershell
  # Navigate to your project directory
  cd "C:\Users\Colin\Desktop\Production\PMB_MODERNIZATION\pmb-dotnet8"
  
  # Install EF Core tools if not already installed
  dotnet tool install --global dotnet-ef
  
  # Scaffold models from existing database
  dotnet ef dbcontext scaffold "Server=localhost;Database=PMB_Dev;Trusted_Connection=true;TrustServerCertificate=true;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c PMBContext --force
  ```

- [ ] **Review Generated Models**
  ```
  What: Examine auto-generated entity classes
  Check for:
  □ All 16 tables represented
  □ Relationships properly configured
  □ Data types correctly mapped
  □ Navigation properties present
  ```

#### **Phase 2: DbContext Configuration**

- [ ] **Update DbContext for EF Core**
  ```csharp
  // Replace EF6 DbContext with EF Core version
  public class PMBContext : DbContext
  {
      public PMBContext(DbContextOptions<PMBContext> options) : base(options) { }
      
      // DbSets for all entities
      public DbSet<Patient> Patients { get; set; }
      public DbSet<Billing> Billings { get; set; }
      // ... all 16 tables
      
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          // Configure relationships and constraints
          base.OnModelCreating(modelBuilder);
      }
  }
  ```

- [ ] **Update Dependency Injection**
  ```csharp
  // In Program.cs (replacing web.config connection strings)
  builder.Services.AddDbContext<PMBContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
  ```

#### **Phase 3: Migration Strategy**

EF Core migrations provide incremental updates to database schema while preserving existing data. The migration system compares current model against previous snapshots to determine necessary changes.

- [ ] **Initial Migration Setup**
  ```powershell
  # Create initial migration (represents current database state)
  dotnet ef migrations add InitialCreate
  
  # This creates:
  # - Migrations/[timestamp]_InitialCreate.cs
  # - Migrations/PMBContextModelSnapshot.cs
  ```

- [ ] **Database Update Process**
  ```powershell
  # Apply migrations to database
  dotnet ef database update
  
  # For different environments:
  # Development: dotnet ef database update
  # Test: dotnet ef database update --connection "Test connection string"
  # Production: Generate SQL scripts instead
  ```

#### **Phase 4: Data Migration & Validation**

- [ ] **Validate Data Integrity**
  ```sql
  -- Compare record counts between old and new systems
  SELECT 'Patients' as TableName, COUNT(*) as RecordCount FROM Patients
  UNION ALL
  SELECT 'Billings', COUNT(*) FROM Billings
  -- ... repeat for all 16 tables
  
  -- Verify critical relationships
  SELECT p.PatientId, COUNT(b.BillingId) as BillingCount
  FROM Patients p
  LEFT JOIN Billings b ON p.PatientId = b.PatientId
  GROUP BY p.PatientId
  HAVING COUNT(b.BillingId) = 0  -- Find patients with no billings
  ```

- [ ] **Test CRUD Operations**
  ```csharp
  // Test basic database operations with EF Core
  
  // Create
  var newPatient = new Patient { Name = "Test Patient", ... };
  context.Patients.Add(newPatient);
  await context.SaveChangesAsync();
  
  // Read
  var patients = await context.Patients.ToListAsync();
  
  // Update
  var patient = await context.Patients.FindAsync(patientId);
  patient.Name = "Updated Name";
  await context.SaveChangesAsync();
  
  // Delete
  context.Patients.Remove(patient);
  await context.SaveChangesAsync();
  ```

### Backup & Recovery Procedures

- [ ] **Database Backup Strategy**
  ```sql
  -- Full backup before migration
  BACKUP DATABASE [PMB_Dev] 
  TO DISK = 'C:\Backups\PMB_Dev_PreMigration.bak'
  WITH COMPRESSION, INIT;
  
  -- Regular backups during development
  BACKUP DATABASE [PMB_Dev] 
  TO DISK = 'C:\Backups\PMB_Dev_Daily.bak'
  WITH COMPRESSION, INIT;
  ```

- [ ] **Rollback Procedures**
  ```sql
  -- If migration fails, restore from backup
  RESTORE DATABASE [PMB_Dev] 
  FROM DISK = 'C:\Backups\PMB_Dev_PreMigration.bak'
  WITH REPLACE;
  ```

**Step 4 Status: ✅ Complete when database migration tested and validated**

---

## Step 5: Application Deployment Workflows

### Development to Test Server Deployment

#### **Build & Publish Process**

- [ ] **Configure Publishing Profile in Visual Studio**
  ```
  What: Set up automated deployment to test server
  Steps:
  1. Right-click project in Solution Explorer
  2. Select "Publish"
  3. Choose "Folder" as publish target
  4. Set path: C:\inetpub\wwwroot\pmb-test
  5. Configure settings:
     - Configuration: Release
     - Target Framework: net8.0
     - Deployment Mode: Framework Dependent
     - Target Runtime: win-x64
  6. Save profile as "TestServer"
  ```

- [ ] **Publish Application**
  ```powershell
  # Command line publish (alternative to VS GUI)
  dotnet publish -c Release -o C:\inetpub\wwwroot\pmb-test
  
  # Or use the saved profile
  dotnet publish -p:PublishProfile=TestServer
  ```

#### **Configuration Management**

- [ ] **Environment-Specific Configuration**
  ```json
  // appsettings.json (base configuration)
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=localhost;Database=PMB_Prod;Trusted_Connection=true;"
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
  
  // appsettings.Development.json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=localhost;Database=PMB_Dev;Trusted_Connection=true;"
    }
  }
  
  // appsettings.Production.json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=productionserver;Database=PMB_Prod;Trusted_Connection=true;"
    }
  }
  ```

#### **IIS Deployment Configuration**

ASP.NET Core applications deployed to IIS require the ASP.NET Core Module, which acts as a reverse proxy between IIS and the Kestrel server. The web.config file is automatically generated during publish to configure this module.

- [ ] **Verify web.config Generation**
  ```xml
  <!-- Auto-generated during publish -->
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified"/>
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\PMBApp.dll" 
                  stdoutLogEnabled="false" 
                  stdoutLogFile=".\logs\stdout"/>
    </system.webServer>
  </configuration>
  ```

#### **Deployment Validation & Testing**

- [ ] **Post-Deployment Verification**
  ```
  What: Ensure application deployed correctly and functions properly
  
  Steps:
  1. Browse to test site: http://localhost:8080
  2. Check application loads without errors
  3. Test login functionality
  4. Verify database connectivity
  5. Test core medical billing features
  6. Check integration points (MSB API, fax services)
  ```

- [ ] **Log File Monitoring**
  ```
  What: Monitor application logs for errors
  
  Log Locations:
  - IIS Logs: C:\inetpub\logs\LogFiles\W3SVC1\
  - Application Logs: C:\inetpub\wwwroot\pmb-test\logs\
  - Windows Event Viewer: Application and System logs
  
  Common Issues to Watch:
  - 503 Service Unavailable (ASP.NET Core Module issues)
  - Database connection errors
  - Missing dependencies
  ```

### Production Deployment Strategy

#### **Migration Bundle Approach (Recommended)**

Migration bundles are single-file executables that can be used to apply migrations to a database. They address some of the shortcomings of the SQL script and command-line tools

- [ ] **Create Migration Bundle**
  ```powershell
  # Generate executable for database updates
  dotnet ef migrations bundle --configuration Release
  
  # This creates: efbundle.exe
  # Deploy this to production server and run: .\efbundle.exe
  ```

#### **Blue-Green Deployment Process**

- [ ] **Prepare Production Environment**
  ```
  Phase 1: Parallel Environment Setup
  1. Set up new IIS site alongside existing production
  2. Deploy .NET 8 application to new site
  3. Test thoroughly in production environment
  4. Keep existing .NET Framework site running
  
  Phase 2: Cutover
  1. Schedule maintenance window
  2. Run database migrations
  3. Switch DNS/load balancer to new site
  4. Monitor for issues
  5. Keep old site as rollback option
  ```

### Continuous Integration/Deployment Setup

- [ ] **GitHub Actions Workflow (Future Enhancement)**
  ```yaml
  # .github/workflows/deploy.yml
  name: Deploy PMB Application
  on:
    push:
      branches: [ main ]
  
  jobs:
    deploy:
      runs-on: windows-latest
      steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --configuration Release
      - name: Test
        run: dotnet test
      - name: Publish
        run: dotnet publish --configuration Release
      # Add deployment steps here
  ```

**Step 5 Status: ✅ Complete when deployment workflow established and tested**

---

## Migration Study Guide

### Key Concepts for .NET Framework to .NET 8 Migration

#### **1. Understanding the Architectural Changes**

**Unified Platform:** .NET 8 allows you to build and run applications on Windows, Linux, and macOS, unlike .NET Framework which is Windows-only.

**Performance Improvements:** .NET 8 applications showcase notable performance improvements, consuming less memory and running faster due to runtime optimizations and efficient memory management

**Cross-Platform Support:** Your PMB application, while initially staying on Windows, gains the ability to potentially run on other platforms in the future.

#### **2. Project System Modernization**

**SDK-Style Projects:** The new project format is much simpler and includes automatic references to common assemblies.

```xml
<!-- Old .NET Framework .csproj (verbose) -->
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" />
  <PropertyGroup>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <!-- Many more lines... -->
  </PropertyGroup>
  <!-- Lots of file references -->
</Project>

<!-- New .NET 8 .csproj (minimal) -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

#### **3. Entity Framework Migration Specifics**

**Migration Command Changes:**
```powershell
# EF6 (Package Manager Console)
Add-Migration InitialCreate
Update-Database

# EF Core (CLI or PMC)
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**DbContext Constructor Pattern:**
Change DbContext constructor to consume options and/or override OnConfiguring

#### **4. Configuration System Changes**

**From web.config to appsettings.json:**
```xml
<!-- web.config -->
<configuration>
  <connectionStrings>
    <add name="PMBConnection" connectionString="..." />
  </connectionStrings>
</configuration>
```

```json
// appsettings.json
{
  "ConnectionStrings": {
    "PMBConnection": "..."
  }
}
```

#### **5. Dependency Injection Integration**

.NET Core has built-in dependency injection, unlike .NET Framework which relied on third-party containers.

```csharp
// Program.cs
builder.Services.AddDbContext<PMBContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<IBillingService, BillingService>();
```

### Breaking Changes to Watch For

If you're migrating an app to .NET 8, the breaking changes listed here might affect you. Changes are grouped by technology area, such as ASP.NET Core or Windows Forms

#### **Common Migration Challenges:**

1. **API Availability:** Some .NET Framework APIs may not be available in .NET 8
2. **Third-Party Dependencies:** NuGet packages may need updates for .NET 8 compatibility
3. **Configuration Changes:** Settings migration from web.config to appsettings.json
4. **Authentication:** Forms Authentication → ASP.NET Core Identity migration

### Migration Timeline & Planning

#### **Recommended Approach for PMB:**

**Phase 1 (Weeks 1-2): Project Setup**
- Convert project files to SDK-style
- Update NuGet packages
- Resolve compilation errors
- Basic functionality testing

**Phase 2 (Weeks 3-4): Database Migration**
- Scaffold EF Core models
- Update DbContext and configurations  
- Migrate all database operations
- Comprehensive data testing

**Phase 3 (Weeks 5-6): Authentication & Integration**
- Migrate authentication system
- Test MSB API integration
- Verify fax service connectivity
- End-to-end testing

**Phase 4 (Weeks 7-8): Deployment & Production**
- Set up test server deployment
- Performance testing and optimization
- Production deployment planning
- User acceptance testing

### Tools and Resources

#### **Microsoft Migration Tools:**
- **NET Upgrade Assistant:** The .NET Upgrade Assistant is a command-line tool that can be run on different kinds of .NET Framework apps. It's designed to assist with upgrading .NET Framework apps to .NET
- **.NET Portability Analyzer:** Analyzes API compatibility across .NET versions

#### **Essential Documentation:**
- Microsoft Learn: "Port from .NET Framework to .NET"
- Microsoft Learn: "Port from EF6 to EF Core"  
- Microsoft Learn: "Host ASP.NET Core on Windows with IIS"

#### **Community Resources:**
- .NET migration community forums
- Stack Overflow .NET migration tags
- GitHub migration example repositories

### Performance Considerations

Most importantly, .NET 8 will be supported for three years, almost three times as long as .NET 7 will be supported

**Long-term Support:** .NET 8 is an LTS release, providing stability for medical billing systems that require long-term reliability.

**Memory Management:** Modern garbage collection improvements reduce memory usage and improve application responsiveness.

**Compilation Performance:** Faster build times and improved runtime performance for better development experience.

---

## Troubleshooting Guide

### Common Issues and Solutions

#### **IIS Deployment Issues:**
- **503 Service Unavailable:** Usually indicates ASP.NET Core Module issues or application startup failures
- **404 Not Found:** Check application pool settings and physical path configuration
- **Database Connection Errors:** Verify connection strings and SQL Server accessibility

#### **EF Core Migration Issues:**
- **Model/Database Mismatch:** Use `Add-Migration` with empty name to detect differences
- **Migration Conflicts:** Roll back to previous migration and recreate changes
- **Performance with Large Datasets:** Apply migrations during off-peak hours

#### **Authentication Migration Issues:**
- **Session State Differences:** ASP.NET Core uses different session management
- **Cookie Authentication:** Update cookie configuration for Core compatibility
- **Authorization Policies:** Migrate custom authorization logic to new policy system

### Support Resources

- **Microsoft Documentation:** Official migration guides and troubleshooting
- **Community Forums:** Stack Overflow, Reddit r/dotnet, Microsoft Tech Community
- **Professional Support:** Consider Microsoft consulting services for complex migrations

---

## Conclusion

This comprehensive SOP provides a complete roadmap for migrating the PMB Medical Billing System from .NET Framework to .NET 8. The gradual migration approach minimizes risk while modernizing the application for long-term maintainability and performance.

**Key Success Factors:**
1. **Thorough Testing:** Test each phase extensively before proceeding
2. **Backup Strategy:** Maintain comprehensive backups throughout the process
3. **Incremental Approach:** Migrate components gradually rather than all at once
4. **Documentation:** Keep detailed records of changes and configurations

**Next Steps After Migration:**
- Monitor application performance and user feedback
- Plan for production infrastructure upgrade to .NET 8
- Consider additional modernization opportunities (cloud deployment, microservices, etc.)
- Establish ongoing maintenance and update procedures

---

*Document Version: 1.0 | Created: June 2025 | Author: Claude Assistant for Colin McAllister*