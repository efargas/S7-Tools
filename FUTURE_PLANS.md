# Future Plans

**Last Updated**: 2025-10-13

This document outlines the future development areas for the S7Tools application, based on the project's long-term goals.

## Phase 2: High-Priority Features

1.  **Complete PLC Communication Protocol Integration:**
    -   **Description:** Fully implement the `DumpMemoryAsync` and `InstallStagerAsync` methods in `PlcClientAdapter` to provide robust and reliable communication with Siemens S7 PLCs. This includes handling all aspects of the custom bootloader protocol.
    -   **Status:** Not Started

2.  **Implement Real `SiemensS7Bootloader` Library:**
    -   **Description:** Replace the current stub implementations for the `SiemensS7Bootloader` library with a real implementation or a more robust simulation to enable end-to-end testing and production use.
    -   **Status:** Not Started

3.  **Implement Power Controller Logic:**
    -   **Description:** Implement the `PowerControllerAdapter` with the actual logic for controlling the power supply of the PLC.
    -   **Status:** Not Started

4.  **Implement Payload Provider Logic:**
    -   **Description:** Implement the `PayloadProviderAdapter` with the actual logic for providing the stager and memory dumper payloads.
    -   **Status:** Not Started

## Phase 3: Medium-Priority Features

1.  **Implement PLC Protocol Logic:**
    -   **Description:** Implement the `PlcProtocolAdapter` with the actual logic for handling the PLC communication protocol.
    -   **Status:** Not Started

2.  **Implement PLC Transport Logic:**
    -   **Description:** Implement the `PlcTransportAdapter` with the actual logic for handling the PLC communication transport layer.
    -   **Status:** Not Started

3.  **Implement Dialog and File Operations:**
    -   **Description:** Implement the placeholder logic for dialogs and file operations in all ViewModels and services.
    -   **Status:** Not Started

## Phase 4: Low-Priority Features

1.  **Implement Configuration Management System:**
    -   **Description:** Develop a comprehensive system for managing application and user configurations.
    -   **Status:** Not Started

2.  **Develop Plugin Architecture:**
    -   **Description:** Design and implement a plugin architecture to allow for extensibility and third-party contributions.
    -   **Status:** Not Started

3.  **Performance Optimization for Large Datasets:**
    -   **Description:** Investigate and implement performance optimizations for handling large datasets in the Log Viewer and other UI components.
    -   **Status:** Not Started

## Completed (Phase 1)

- **Implement Task Manager UI (`TaskManagerView`):**
  - **Description:** Create the XAML view for the `TaskManagerViewModel` to provide a user interface for managing and monitoring bootloader tasks.
  - **Status:** Completed