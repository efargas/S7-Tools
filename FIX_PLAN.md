# Remediation Plan: Critical Issues & Optimizations

**Date:** 2026-01-17
**Status:** Completed

This document tracks the remediation of critical issues identified in the `COMPREHENSIVE_CODE_AUDIT.md`. Fixes are applied in order of criticality (Stability > Performance > Maintenance).

## 1. Critical Stability Fixes (Crash Risks)

### [x] Fix Bootloader Memory Explosion
**Issue:** `BaseBootloaderService.cs` reads entire dump files (potentially GBs) back into RAM to satisfy a return type.
**Target:** `src/S7Tools/Services/Bootloader/BaseBootloaderService.cs`
**Action:**
- [x] Remove `File.ReadAllBytes` logic in `PerformDumpProcessStreamingAsync`.
- [x] Populate `allDumps` with `Array.Empty<byte>()` to preserve `List<byte[]>` signature compatibility.
- [x] Ensure `savedFiles` list is correctly populated (already verified, but double-check).

## 2. Critical Performance Fixes (I/O Bottlenecks)

### [x] Optimize Main Application Logging (`FileLogSink`)
**Issue:** Synchronous file open/write/close for *every* log message.
**Target:** `src/S7Tools.Infrastructure.Logging/Sinks/FileLogSink.cs`
**Action:**
- [x] Implement `System.Threading.Channels` for non-blocking queuing.
- [x] Use a long-running background task with a persistent `FileStream`.
- [x] Implement batching/periodic flushing (e.g., every 1s) to reduce disk syscalls.

### [x] Optimize Task Logging (`AsyncFileLogger`)
**Issue:** `FlushAsync()` called after *every* log entry, degrading async benefits.
**Target:** `src/S7Tools/Services/Logging/TaskLoggerFactory.cs`
**Action:**
- [x] Remove `FlushAsync` from the per-message loop for standard logs.
- [x] Implement periodic flush or flush-on-error policy.

## 3. Medium Stability/Performance Fixes (Memory Leaks)

### [x] Fix UI Log Memory Leak
**Issue:** `TaskLogsPanelViewModel` collection grows indefinitely during long tasks.
**Target:** `src/S7Tools/ViewModels/Components/TaskLogsPanelViewModel.cs`
**Action:**
- [x] In the `_logUpdater` callback, implement trimming logic.
- [x] `while (Entries.Count > MaxEntries) Entries.RemoveAt(0);`

## 4. Verification

- [x] Build succeeds (0 errors).
- [x] Code review confirms fixes match requirements.
- [x] `COMPREHENSIVE_CODE_AUDIT.md` updated.
