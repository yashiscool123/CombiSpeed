Core Responsibilities
	Target Platforms: Windows, Mac and Linux OS platform application.
	Guarantee safe recovery, efficient resource usage, and transparent documentation.
	Objective: Deliver a production-ready, fully tested application capable of an ultra-fast, multi-OS application that combines multiple network adapters (Wi-Fi, Gigabit Ethernet, 4G/5G Cellular USB dongles, Mobile Hotspots, Thunderbolt bridges, and LAN connections) into a single, high-speed aggregate connection with dynamic traffic shaping. Featuring dedicated, separate native codebases for **Windows 10/11 (`CombiSpeed/`)**, **Linux (`CombiSpeed-Linux/`)**, and **macOS (`CombiSpeed-macOS/`)**, CombiSpeed runs directly on each OS kernel's network stack with native low-level socket binding, delivering zero runtime overhead, instant startup (<200ms), and an identical, accessible dark cyberpunk glassmorphic dashboard across all platforms!

1. Task Persistence


2. Failure Recovery
On crash, restart, or error:
   Reload the last saved playback state (channel, position, buffer).
   Resume unfinished EPG sync jobs from the most recent checkpoint.
   Ensure no context loss during channel zapping or app restart.

3. Task Lifecycle
   Maintain a task log with statuses:
      pending 
      in‑progress 
      completed 
      failed 
   Never discard or forget user queries unless explicitly marked as completed or canceled.

4. User Confirmation


5. Reliability
   Use retries with exponential backoff for transient stream failures.
   Ensure 100% compatibility and operability on Windows, Mac & Linux OS platform devices.
   Auto check‑in code to GitHub after every feature completion.
   Auto increment version at every check‑in.

6. Memory Management
   Run optimally with efficient buffer allocation (DefaultLoadControl).
   Avoid excessive memory usage on low‑RAM devices.

7. Resource Cleanup
   Release instances when not in use.
   Delete and shut down background workers once no longer needed.

8. Continuous Improvement
   Apply optimizations at every request.
   Fix pending bugs continuously.
   Auto validate code before merging.

9. Code Lineage
   Always understand code context and lineage before resolving defects.
   Avoid trial‑and‑error changes in code without context and lineage knowledge.

10. Documentation
   Maintain ToDos.md for tasks (e.g., AFR improvements, fixes, etc).
   Maintain KnownIssues.md for bugs.
   Update ReadMe.md with:
      Latest summary
      Features
      MOATs
      Benefits
      Real‑world challenges solved
      Auto‑generate docs from commit messages/issues where applicable.

11. Release Management
   Create Release Notes upon every app release in GitHub repositories.

12. Versioning Rules
   Routine Updates / Features / Bug Fixes:
         Increment patch version: 0.0.1
         Example: 6.1.1 → 6.1.2 → 6.1.3
      Major Version Updates:
         Increment minor version: 0.x.0
         Example: 6.1.0 → 6.2.0
      Super Major Version Updates:
         Increment major version: x.0.0
         Example: 6.0.0 → 7.0.0
   Always increment versionCode by +1 at every release check‑in.

13. Engineering Guidelines
   System Architecture: Scalable, Extensible and modular appplication.
   File Structure: Clean separation of modules.
   Database Schema: Normalize tables and add indexes for fast queries.
   API Endpoints: Secure endpoint calls with authentication.
   UI Architecture: Minimal, responsive, Performance Optimized.
   Production Code: No dummy functions; all features must work.
   Bad Decisions to Avoid:
      Duplicate code / logic
      Tight coupling between data, logic and UI
      Ignoring scalability risks
      Poor maintainability

14. Hallucination Control
   Grounding & Verification
      Always ground in authoritative URLs / sources.
      Never rely solely on cached memory for dynamic updates.
      Cross‑verify URLs before access and executions.
      Require syntax validation before presenting code.
      Run generated code in sandbox to catch runtime errors.
      Enforce static analysis tools (Detekt, Lint, SonarQube).
      Never suggest code without verifying it compiles.

   Transparency
      Explicitly state uncertainty when code /  feature / functions cannot be verified.
      Use English like phrases.

   Boundaries
      Never fabricate data.
      Never invent citations or references.
      Never assume user intent — clarify ambiguous queries before answering.

   Response Discipline
      Stick to application query scope.
      Avoid speculative predictions unless explicitly requested.

   Error Handling
      Respond with a clear refusal or safe fallback.
      Continuous Monitoring
      Log all issues & defects found through expliot / vulnerability scans in KnownIssues.md.
      Add corrective measures to ToDos.md.
      Update ReadMe.md with anti‑hallucination strategies.

   Failure Recovery
      Add transactional rollback rules in application where required.
      Ensure resumed tasks are idempotent.

   Testing & Validation
      Require unit tests and integration tests before merging.
      Minimum unit test coverage: 80% across application modules.
      Integration test coverage: 70% minimum.
      Tests must run in CI/CD pipelines before merging where applicable in the application architecture.
      Automatically add static analysis and vulnerability scans.
      
15. Build Automation
   Every commit triggers:
      Run unit tests (≥80% coverage).
      Run integration tests (≥70% coverage).
      Run static analysis (Detekt, Lint, SonarQube).
      Compile and Build application artifacts.
      Artifacts are stored in GitHub Actions or your CI/CD artifact repository.
   Always create a Release build.

16. Release Automation
   After successful build:
      Auto‑increment versionCode and semantic version (x.y.z).
      Generate Release Notes from commit messages.
   Push a GitHub Release with:
      Updated ReadMe.md, ToDos.md, KnownIssues.md
      Release Notes (features, fixes, improvements / enhancements)
      Do not upload binaries in Github.