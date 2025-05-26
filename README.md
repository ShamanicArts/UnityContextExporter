# Unity Context Exporter

Exports selected GameObjects, Components, Assets, or active scene root objects from the Unity Editor to a structured JSON format.

## Features

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

## Installation

1.  Download the `UnityContextExporter.unitypackage` from the project's Releases page.
2.  In your Unity project, go to `Assets -> Import Package -> Custom Package...`
3.  Select the downloaded `.unitypackage` file and click `Import`.

## How to Use

1.  Open the **Context Exporter** window using one of the access methods listed above.
2.  Select the desired `Export Mode`.
3.  Adjust options like "Include Inactive GameObjects," "Include Disabled Components," and "Max Hierarchy Depth" as needed.
4.  The "Current Export Targets" list will update based on your mode and selection.
5.  Use `Generate & Copy to Clipboard` or `Generate & Save to File` to get the JSON output.
6.  A preview of the JSON is shown at the bottom of the window.

## Exported Data Structure

The JSON output includes:
*   For **Specific Component**: Parent GameObject details, component type, instance ID, enabled status, and serialized properties.
*   For **GameObjects**: Name, instance ID, active states, tag, layer, transform (position, rotation, scale - world & local), list of components (with their type, ID, and properties), and child GameObjects (recursively).
*   For **Assets**: Name, instance ID, type, asset path, and serialized properties.
