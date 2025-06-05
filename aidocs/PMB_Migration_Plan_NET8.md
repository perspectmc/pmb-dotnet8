# PMB Migration Plan: Migration to .NET 8.0

---

## Phase 0: Preparation and Environment Setup

### Purpose
Establish a reliable foundation for migration by preparing the development environment, repository structure, tooling, and documentation standards.

### AI Tools
- **ChatGPT-4o / Claude 4 Opus:** Selection of AI tools, repository structure guidance, and documentation best practices.
- **Cursor AI:** Tracking of refactoring progress and batch code analysis.

### Action Steps
- [ ] Create GitHub repository: `perspect/pmb-dotnet8`
- [ ] Configure repository directories: `/src`, `/aidocs`, `/tests`, `/tools`, `/infra`
- [ ] Install required developer tools:
    - GitHub Desktop or CLI
    - Visual Studio Code with C# Dev Kit and .NET 8 SDK
    - SQL Server Management Studio
    - Postman
    - GitLens and REST Client (VS Code extensions)
- [ ] Install AI tools: Cursor AI, Claude 4 Opus, ChatGPT-4o (Pro), Gemini 2.5 Pro
- [ ] Create `.aidocs/README.md` to explain folder structure
- [ ] Initialize `.gitignore`, `README.md`, and `migration_journal.md`
- [ ] Set Visual Studio Code as the default `.md` editor for both macOS and Windows environments
- [ ] Document hardware and development environment specifications in `/aidocs/Infra/HomeSetup.md`
- [ ] Execute environment setup tasks in parallel with architecture documentation to optimize project timelines.

---

## Phase 1: High-Level Architecture and Business Analysis

### Purpose
Achieve a comprehensive understanding of the current system architecture, business workflows, and integration points to inform migration decisions and ensure alignment with business requirements.

### Risks and Dependencies
- Outdated or incomplete architecture diagrams may impact migration accuracy.
- Insufficient documentation of business rules may lead to functional regressions.
- Dependencies on legacy systems (e.g., MSB APIs, Interfax) require thorough assessment.

### AI Tools
- **Claude 4 Opus:** Extraction of architectural constraints and business rules from documentation.
- **ChatGPT-4o:** Workflow summarization and clarification.
- **Gemini 2.5 Pro:** Mapping of dependencies and visualization of component interactions.

### Action Steps
- [ ] Collect and review all available architecture diagrams and business process documentation.
- [ ] Conduct stakeholder interviews to clarify ambiguous areas.
- [ ] Utilize AI tools to create updated diagrams and glossaries.
- [ ] Document primary integration points and data flows in `/aidocs/Architecture/`.
- [ ] Validate architectural documentation with development and business teams.

---

## Phase 2: Codebase Analysis and Dependency Mapping

### Purpose
Analyze the legacy codebase to identify modules, dependencies, and potential migration challenges, resulting in a detailed mapping of the code structure and external dependencies.

### Risks and Dependencies
- Highly coupled modules may necessitate extensive refactoring.
- Third-party library versions may lack compatibility with .NET 8.
- Unidentified dependencies could result in runtime errors after migration.

### AI Tools
- **Cursor AI:** Codebase navigation and module summary generation.
- **Gemini 2.5 Pro:** Correlation of dependencies across projects and services.
- **DeepSeek Coder:** Syntax conversion and asynchronous code transformation.

### Action Steps
- [ ] Execute static code analysis to identify modules and dependencies.
- [ ] Use AI tools to generate summaries for each module.
- [ ] Document all external libraries and their respective versions.
- [ ] Identify and flag deprecated or unsupported APIs for replacement.
- [ ] Produce a dependency graph in `/aidocs/Architecture/DependencyGraph.md`.
- [ ] Use SonarQube and AI tools to identify dead code, unused modules, and security risks; remove or quarantine these before migration begins.
- [ ] Document all removed files and rationale in `/aidocs/RiskLogs/CodeCleanupCandidates.md`.

---

## Phase 3: Code Migration and Refactoring

### Purpose
Migrate code to .NET 8, introducing modern patterns, asynchronous programming, and enhanced maintainability. Integrate AI-driven refactoring strategies and prompt management.

### Risks and Dependencies
- Potential for functional regressions due to code refactoring.
- Migration to asynchronous patterns may introduce concurrency issues.
- Some legacy features may not have direct .NET 8 equivalents.

### AI Tools
- **DeepSeek Coder:** Syntax conversion and asynchronous refactoring.
- **ChatGPT-4o:** Generation of migration scripts and code snippets.
- **Claude 4 Opus:** Architectural compliance review of refactored code.
- **Rose AI:** Advanced pattern recognition and legacy architecture inference.

### Action Steps
- [ ] Migrate modules incrementally, prioritizing those with minimal dependencies.
- [ ] Refactor synchronous code to asynchronous patterns where applicable.
- [ ] Replace deprecated APIs with supported .NET 8 alternatives.
- [ ] Develop unit and integration tests concurrently with migration.
- [ ] Commit changes frequently with clear, descriptive messages.
- [ ] Use AI tools (Cursor, Claude, ChatGPT-4o, Rose) for code review and optimization.
- [ ] Leverage Rose AI to identify deep architectural patterns and anti-patterns before transformation begins.
- [ ] Implement predefined AI prompts to standardize refactoring tasks, such as:
    - "Rewrite authentication using ASP.NET Core Identity."
    - "Convert synchronous database operations to async methods."
- [ ] Maintain a repository of reusable AI-guided code review prompts in `/aidocs/Prompts/CodeReviewPrompts.md`, including:
    - "Ensure this module conforms to ASP.NET Core best practices."
    - "Verify whether this legacy dependency has a .NET 8-compatible equivalent."
- [ ] Begin by migrating utility and non-UI class libraries (e.g., `MBS.Common`, `MBS.DomainModel`) to .NET 8. These modules can be tested in isolation with new .NET 8 test projects.
- [ ] Use boundary isolation to allow legacy .NET Framework code to consume .NET Standard-compatible libraries when possible.
- [ ] For service modules (e.g., `MBS.WebApiService`), migrate as standalone .NET 8 APIs and expose HTTP endpoints. Test these APIs independently using Postman, Swagger, or integration tests.
- [ ] Delay full portal migration (`MBS.Web.Portal`) until backend services and libraries have been modernized.
- [ ] Avoid global rewrites; focus modernization on code that is actively in use or undergoing functional change.
- [ ] Consider a shadow testing approach: deploy .NET 8 services in a local or staging environment while the legacy .NET Framework application remains in production. Use parallel data to validate behavior before switching over.

---

## Phase 4: Debugging, Issue Resolution, and Security

### Purpose
Detect, diagnose, and resolve issues resulting from migration to achieve functional parity and system stability. Integrate security compliance checks into the debugging and triage process.

### Risks and Dependencies
- Complex bugs may require in-depth knowledge of both legacy and modern systems.
- Insufficient test coverage may obscure critical issues.
- Performance regressions may manifest after migration.

### AI Tools
- **ChatGPT-4o:** Debugging strategies and root cause analysis.
- **Gemini 2.5 Pro:** Cross-module issue tracing.
- **Serilog & MiniProfiler:** Runtime diagnostics and instrumentation.

### Action Steps
- [ ] Implement comprehensive logging and monitoring.
- [ ] Apply AI-assisted debugging to analyze error logs and stack traces.
- [ ] Prioritize and resolve critical issues.
- [ ] Execute regression testing following issue resolution.
- [ ] Document resolved issues in `/aidocs/RiskLogs/`.
- [ ] Conduct security compliance checks, including static analysis, OWASP rules, and secrets scanning, during issue triage.
- [ ] Integrate OWASP dependency checks, secrets scanning, and static analysis tools into CI/CD pipelines to ensure ongoing security compliance.

---

## Phase 5: Documentation and Context Preservation

### Purpose
Maintain current, comprehensive documentation and context throughout the migration process to support ongoing development and future maintenance. Ensure documentation formats support integration with AI tools.

### Risks and Dependencies
- Documentation lag relative to code changes may reduce team effectiveness.
- Loss of institutional knowledge if not adequately captured.
- Inconsistent documentation standards may hinder automation and AI integration.

### AI Tools
- **Cursor AI:** Synchronization of code comments and `.aidocs` content.
- **Claude 4 Opus:** Extraction of architectural insights from code and documentation.
- **ChatGPT-4o:** Generation of documentation drafts and summaries.

### Action Steps
- [ ] Enforce documentation updates as a standard part of the development workflow.
- [ ] Use AI tools to generate and validate all documentation.
- [ ] Maintain a glossary and migration journal in `/aidocs/`.
- [ ] Schedule regular documentation reviews with team members.
- [ ] Archive legacy documentation for future reference.
- [ ] Maintain a changelog of removed modules and unused files for audit traceability.
- [ ] Record test strategies used during partial modernization (e.g., dual runtime, isolated module testing).

---

## Phase 6: Final Recommendations and Ongoing Maintenance

### Purpose
Summarize key findings, establish best practices, and outline next steps for continuous improvement and future enhancements.

### Risks and Dependencies
- Omission of key recommendations may result in technical debt.
- Insufficient training on new systems may hinder adoption.
- Neglecting performance and security best practices may compromise system integrity.

### AI Tools
- **ChatGPT-4o:** Drafting of final reports and best practice documentation.
- **Claude 4 Opus:** Validation of architectural compliance.
- **Gemini 2.5 Pro:** Identification of opportunities for optimization.

### Action Steps
- [ ] Compile a comprehensive migration summary and lessons learned.
- [ ] Document recommended coding standards and development workflows.
- [ ] Propose and schedule team training sessions.
- [ ] Plan enhancements to continuous integration and deployment pipelines.
- [ ] Schedule regular technical debt and optimization reviews.
- [ ] Define deployment strategies, including blue-green deployment or feature flag rollouts, to minimize risk during production releases.

---

## AI Tool Usage Reference

| Tool            | Primary Role                                 |
|-----------------|----------------------------------------------|
| Claude 4 Opus   | Architectural analysis and documentation     |
| ChatGPT-4o      | Migration scripts, documentation, debugging  |
| Cursor AI       | Refactoring tracking and code navigation     |
| DeepSeek Coder  | Syntax and asynchronous migration            |
| Gemini 2.5 Pro  | Dependency mapping and high-context analysis |

---

## Testing Tools

- **xUnit / NUnit:** Unit testing
- **Pact / WireMock:** API contract testing and mocking
- **Playwright / Selenium:** UI testing
- **SQL Profiler / MiniProfiler:** Performance diagnostics
- **Postman / Swagger:** API endpoint validation

---

## .aidocs Directory Structure (Proposed)

```
 /aidocs/
   Architecture/
     DependencyGraph.md
     ComponentMap.md
   Infra/
     HomeSetup.md
   RiskLogs/
     KnownIssues.md
   MigrationJournal.md
   Glossary.md
   README.md
```

---

## Executive Flow of Activities

This section provides a high-level outline of the logical flow, dependencies, and sequencing for the .NET 8 modernization project. It is intended for project sponsors and executive oversight.

### Stage 0: Foundation Setup *(sequential)*
- Create GitHub repo and folder structure
- Install required dev tools and AI assistants
- Initialize baseline project documentation and environment records

### Stage 1: Business and Architectural Clarity *(parallel streams)*
**Stream A – Architecture**
- Review existing system diagrams and integration points
- Document infrastructure and component interactions

**Stream B – Business Rules**
- Conduct stakeholder interviews
- Document workflows, billing logic, and claim statuses

### Stage 2: Codebase Analysis and Dead Code Cleanup *(parallel)*
- Run static and AI-driven code analysis
- Remove or quarantine unused, obsolete, or insecure code
- Record all deletions in the cleanup log for audit traceability

### Stage 3: Modular .NET 8 Migration and Refactoring *(sequential within modules)*
- Begin with utility libraries (e.g., `MBS.Common`, `DomainModel`)
- Migrate APIs (e.g., `WebApiService`) to standalone .NET 8 services
- Postpone portal migration until dependencies are updated
- Run isolated unit/integration tests alongside migration

### Stage 4: Debugging, Testing, and Security *(parallel)*
- Conduct regression, functional, and performance tests
- Resolve issues and harden security using OWASP scans and diagnostics
- Monitor runtime behavior in isolated environments

### Stage 5: Documentation and Context Preservation *(ongoing)*
- Maintain `/aidocs/` as the system of record
- Capture migration journals, testing strategy, and glossary terms

### Stage 6: Final Transition and Go-Live *(sequential)*
- Deploy staging environment and validate
- Plan cutover with rollback strategies
- Launch modernized platform and monitor post-launch health
```