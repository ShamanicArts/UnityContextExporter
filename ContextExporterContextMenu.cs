// Assets/Editor/ContextExporterContextMenu.cs
using UnityEditor;
using UnityEngine;

public static class ContextExporterContextMenu
{
    [MenuItem("CONTEXT/Component/Export This Component As Context", false, 1000)]
    private static void ExportSpecificComponentContext(MenuCommand command)
    {
        Component component = command.context as Component;
        if (component != null)
        {
            ContextExporterWindow.OpenForSingleComponent(component);
        }
    }

    [MenuItem("CONTEXT/Component/Export This Component As Context", true)]
    private static bool ExportSpecificComponentContextValidation(MenuCommand command)
    {
        return command.context is Component;
    }
}