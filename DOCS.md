# Unity Context Exporter Documentation

## Table of Contents
1.  [Overview](#1-unity-context-exporter---overview)
2.  [Features](#2-features)
3.  [How to Use](#3-how-to-use)
4.  [Exported Data Structure](#4-exported-data-structure)
5.  [Source Code and License](#5-source-code-and-license)

---

## 1. Unity Context Exporter - Overview

Exports selected GameObjects, Components, Assets, or active scene root objects from the Unity Editor to a structured JSON format.

For further details, updates, or to contribute, please visit the [GitHub repository](https://github.com/ShamanicArts/UnityContextExporter/).

## 2. Features

*   **Export Modes:**
    *   `FromCurrentSelection`: Exports currently selected GameObjects or Assets.
    *   `ActiveSceneRoots`: Exports all root GameObjects in the active scene.
    *   `SpecificComponentOnly`: Exports a single targeted component.
*   **Export Options:**
    *   Include/Exclude inactive GameObjects.
    *   Include/Exclude disabled Components.
    *   Set maximum hierarchy depth for GameObject traversal (not applicable for `SpecificComponentOnly`).
*   **Output:**
    *   Generates JSON data.
    *   Copy to clipboard.
    *   Save to file.
    *   In-window preview of JSON.
*   **Access:**
    *   `Tools -> Context Exporter` menu.
    *   Context menu on Components (in Inspector): `CONTEXT/Component -> Export This Component As Context`.
    *   Context menu on Assets (Project view): `Assets -> Export Asset(s) to Context`.
    *   Context menu on GameObjects (Hierarchy/Scene): `GameObject -> Export Selection to Context`.

## 3. How to Use

1.  Open the **Context Exporter** window using one of the access methods listed in section "2. Features".
2.  Select the desired `Export Mode`.
3.  Adjust options like "Include Inactive GameObjects," "Include Disabled Components," and "Max Hierarchy Depth" as needed.
4.  The "Current Export Targets" list will update based on your mode and selection.
5.  Use `Generate & Copy to Clipboard` or `Generate & Save to File` to get the JSON output.
6.  A preview of the JSON is shown at the bottom of the window.

## 4. Exported Data Structure

The JSON output includes:

*   **For Specific Component**:
    *   Parent GameObject details
    *   Component type
    *   Instance ID
    *   Enabled status
    *   Serialized properties
*   **For GameObjects**:
    *   Name
    *   Instance ID
    *   Active states (self and in hierarchy)
    *   Tag
    *   Layer
    *   Transform (position, rotation, scale - world & local)
    *   List of components (with their type, ID, and properties)
    *   Child GameObjects (recursively, respecting Max Hierarchy Depth)
*   **For Assets**:
    *   Name
    *   Instance ID
    *   Type
    *   Asset path
    *   Serialized properties

## 5. Source Code and License

The source code for the Unity Context Exporter is available on [GitHub](https://github.com/ShamanicArts/UnityContextExporter/).

This software is licensed under the MIT License. You can find a copy of the license in the GitHub repository.