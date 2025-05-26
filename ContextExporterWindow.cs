// Assets/Editor/ContextExporterWindow.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

public class ContextExporterWindow : EditorWindow
{
    public enum ExportMode
    {
        FromCurrentSelection,
        ActiveSceneRoots,
        SpecificComponentOnly
    }

    private ExportMode _exportMode = ExportMode.FromCurrentSelection;
    private bool _includeInactiveGameObjects = false;
    private bool _includeDisabledComponents = false;
    private int _maxHierarchyDepth = 10; 
    private Vector2 _scrollPositionObjects;
    private Vector2 _scrollPositionPreview;

    private static List<Object> _initialTargetsFromContextMenu = new List<Object>();
    private List<Object> _currentDisplayTargets = new List<Object>();

    private static Component _specificComponentTarget = null;

    private const string TOOLS_MENU_ITEM_PATH = "Tools/Context Exporter"; 

    [MenuItem(TOOLS_MENU_ITEM_PATH)]
    public static void ShowWindowMenu() 
    {
        _specificComponentTarget = null;
        ExportMode mode = Selection.objects.Length > 0 ? ExportMode.FromCurrentSelection : ExportMode.ActiveSceneRoots;
        _initialTargetsFromContextMenu.Clear();
        if (mode == ExportMode.FromCurrentSelection)
        {
            _initialTargetsFromContextMenu.AddRange(Selection.objects);
        }
        ShowWindow(false, mode, "Context Exporter");
    }

    private const string ASSET_CONTEXT_MENU_PATH = "Assets/Export Selection to Context";

    [MenuItem(ASSET_CONTEXT_MENU_PATH, false, 1000)] 
    private static void ExportAssetsToContext() 
    {
        _initialTargetsFromContextMenu.Clear();
        _initialTargetsFromContextMenu.AddRange(Selection.objects.Where(o => o != null && AssetDatabase.Contains(o) && !(o is Component)).ToArray());
        
        if (_initialTargetsFromContextMenu.Count == 0 && Selection.objects.Length > 0) {
            Debug.LogWarning("Context Exporter: No exportable assets found in current project selection.");
            return;
        }
        if (_initialTargetsFromContextMenu.Count == 0) return;

        _specificComponentTarget = null;
        ShowWindow(true, ExportMode.FromCurrentSelection, "Context Exporter");
    }

    [MenuItem(ASSET_CONTEXT_MENU_PATH, true)]
    private static bool ExportAssetsToContextValidation()
    {
        return Selection.objects.Any(o => o != null && AssetDatabase.Contains(o) && !(o is Component));
    }

    private const string GAMEOBJECT_CONTEXT_MENU_PATH = "GameObject/Export Selection to Context";

    [MenuItem(GAMEOBJECT_CONTEXT_MENU_PATH, false, 1000)]
    private static void ExportGameObjectsToContext() 
    {
        _initialTargetsFromContextMenu.Clear();
        _initialTargetsFromContextMenu.AddRange(Selection.gameObjects); 
        if (_initialTargetsFromContextMenu.Count == 0) return;

        _specificComponentTarget = null;
        ShowWindow(true, ExportMode.FromCurrentSelection, "Context Exporter");
    }

    [MenuItem(GAMEOBJECT_CONTEXT_MENU_PATH, true)]
    private static bool ExportGameObjectsToContextValidation()
    {
        return Selection.gameObjects.Length > 0;
    }
    
    public static void OpenForSingleComponent(Component component)
    {
        _initialTargetsFromContextMenu.Clear();
        _specificComponentTarget = component;
        if (component != null)
        {
            _initialTargetsFromContextMenu.Add(component);
        }
        ShowWindow(true, ExportMode.SpecificComponentOnly, "Context Exporter");
    }

    public static void ShowWindow(bool fromContextMenuInvoked, ExportMode desiredMode, string windowTitle)
    {
        ContextExporterWindow window = GetWindow<ContextExporterWindow>(windowTitle); 
        window._exportMode = desiredMode;
        window.UpdateGUIDisplayTargets(); 
        _initialTargetsFromContextMenu.Clear(); 
        window.Focus();
    }

    void OnEnable()
    {
        Selection.selectionChanged += OnUnitySelectionChanged;
        if (_exportMode != ExportMode.SpecificComponentOnly) {
            _specificComponentTarget = null;
        }
        UpdateGUIDisplayTargets();
    }

    void OnDisable()
    {
        Selection.selectionChanged -= OnUnitySelectionChanged;
    }

    void OnUnitySelectionChanged()
    {
        if (_exportMode == ExportMode.FromCurrentSelection)
        {
            _specificComponentTarget = null;
            UpdateGUIDisplayTargets(true);
        }
    }
    
    private void UpdateGUIDisplayTargets(bool useCurrentSelectionFromUnity = false)
    {
        _currentDisplayTargets.Clear();

        if (_exportMode == ExportMode.SpecificComponentOnly)
        {
            if (_initialTargetsFromContextMenu.Count > 0 && _initialTargetsFromContextMenu[0] is Component) {
                 _currentDisplayTargets.Add(_initialTargetsFromContextMenu[0]);
            } else if (_specificComponentTarget != null) {
                 _currentDisplayTargets.Add(_specificComponentTarget);
            }
        }
        else if (_exportMode == ExportMode.FromCurrentSelection)
        {
            var source = (useCurrentSelectionFromUnity || _initialTargetsFromContextMenu.Count == 0) ?
                            Selection.objects.Where(o => o != null).ToList() :
                            new List<Object>(_initialTargetsFromContextMenu);
            _currentDisplayTargets.AddRange(source);
        }
        else if (_exportMode == ExportMode.ActiveSceneRoots)
        {
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                _currentDisplayTargets.AddRange(activeScene.GetRootGameObjects());
            }
        }
        Repaint();
    }

    void OnGUI()
    {
        GUILayout.Label("Context Exporter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        ExportMode previousMode = _exportMode;
        _exportMode = (ExportMode)EditorGUILayout.EnumPopup("Export Mode:", _exportMode);

        if (EditorGUI.EndChangeCheck()) 
        {
            if (_exportMode != ExportMode.SpecificComponentOnly) {
                _specificComponentTarget = null; 
            }
            if (_exportMode == ExportMode.SpecificComponentOnly && _specificComponentTarget == null) {
                _exportMode = ExportMode.FromCurrentSelection;
            }
            UpdateGUIDisplayTargets(true); 
        }

        if (_exportMode == ExportMode.SpecificComponentOnly)
        {
            if (_specificComponentTarget != null) {
                EditorGUILayout.HelpBox($"Exporting component: {_specificComponentTarget.GetType().Name} from GameObject: {_specificComponentTarget.gameObject.name}", MessageType.Info);
            } else {
                EditorGUILayout.HelpBox("Specific component mode, but no component targeted. Please re-select via context menu.", MessageType.Warning);
            }
            _includeInactiveGameObjects = EditorGUILayout.Toggle("Export if Parent GO is Inactive", _includeInactiveGameObjects); 
            _includeDisabledComponents = EditorGUILayout.Toggle("Export Component if Disabled", _includeDisabledComponents);
            
            EditorGUI.BeginDisabledGroup(true); 
            EditorGUILayout.IntSlider("Max Hierarchy Depth (N/A):", _maxHierarchyDepth, 1, 50); 
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            _includeInactiveGameObjects = EditorGUILayout.Toggle("Include Inactive GameObjects", _includeInactiveGameObjects);
            _includeDisabledComponents = EditorGUILayout.Toggle("Include Disabled Components", _includeDisabledComponents);
            _maxHierarchyDepth = EditorGUILayout.IntSlider("Max Hierarchy Depth:", _maxHierarchyDepth, 1, 50);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Current Export Targets:", EditorStyles.boldLabel);
        _scrollPositionObjects = EditorGUILayout.BeginScrollView(_scrollPositionObjects, GUILayout.MinHeight(80), GUILayout.MaxHeight(200));

        if (_currentDisplayTargets.Count == 0)
        {
            EditorGUILayout.HelpBox("No objects targeted. Select objects/component or change Export Mode.", MessageType.Info);
        }
        else
        {
            foreach (var target in _currentDisplayTargets)
            {
                if (target != null)
                {
                    string label = target.name;
                    if (target is Component compTarget) {
                        label = $"{compTarget.GetType().Name} (on {compTarget.gameObject.name})";
                    }
                    EditorGUILayout.ObjectField(label, target, target.GetType(), true, GUILayout.ExpandWidth(true));
                }
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate & Copy to Clipboard"))
        {
            bool hasTargets = (_exportMode == ExportMode.SpecificComponentOnly && _specificComponentTarget != null) ||
                              (_exportMode != ExportMode.SpecificComponentOnly && _currentDisplayTargets.Count > 0);
            if (!hasTargets) { EditorUtility.DisplayDialog("No Targets", "No objects are targeted for export.", "OK"); }
            else
            {
                string data = GenerateDataJson();
                if (!string.IsNullOrEmpty(data) && data != "{}" && !data.StartsWith("No data generated") && !data.StartsWith("No specific component"))
                {
                    EditorGUIUtility.systemCopyBuffer = data;
                    Debug.Log("Context Exporter: Data copied to clipboard."); 
                    ShowNotification(new GUIContent("Data copied to clipboard!"));
                } else {
                    Debug.LogWarning("Context Exporter: No data generated for current targets/settings. Output: " + data); 
                    ShowNotification(new GUIContent("No data generated!"));
                }
            }
        }

        if (GUILayout.Button("Generate & Save to File"))
        {
            bool hasTargets = (_exportMode == ExportMode.SpecificComponentOnly && _specificComponentTarget != null) ||
                              (_exportMode != ExportMode.SpecificComponentOnly && _currentDisplayTargets.Count > 0);
            if (!hasTargets) { EditorUtility.DisplayDialog("No Targets", "No objects are targeted for export.", "OK"); }
            else
            {
                string data = GenerateDataJson();
                 if (!string.IsNullOrEmpty(data) && data != "{}" && !data.StartsWith("No data generated") && !data.StartsWith("No specific component"))
                {
                    string filename = "context_export_data"; 
                    if (_exportMode == ExportMode.SpecificComponentOnly && _specificComponentTarget != null) {
                        filename = $"context_export_component_{_specificComponentTarget.gameObject.name}_{_specificComponentTarget.GetType().Name}";
                    } else if (_currentDisplayTargets.Count == 1 && _currentDisplayTargets[0] != null) {
                        filename = $"context_export_{_currentDisplayTargets[0].name}";
                    } else if (_currentDisplayTargets.Count > 1) {
                        filename = $"context_export_multiple_items";
                    }

                    string path = EditorUtility.SaveFilePanel("Save Context Data", "", filename, "json"); 
                    if (!string.IsNullOrEmpty(path)) {
                        System.IO.File.WriteAllText(path, data);
                        Debug.Log($"Context Exporter: Data saved to: {path}"); 
                        ShowNotification(new GUIContent($"Data saved to: {System.IO.Path.GetFileName(path)}"));
                    }
                }  else {
                     Debug.LogWarning("Context Exporter: No data generated for current targets/settings. Output: " + data); 
                     ShowNotification(new GUIContent("No data generated!"));
                }
            }
        }

        EditorGUILayout.Space();
        GUILayout.Label("Preview (first 1000 chars):", EditorStyles.miniBoldLabel);
        _scrollPositionPreview = EditorGUILayout.BeginScrollView(_scrollPositionPreview, GUILayout.ExpandHeight(true));
        bool previewHasTargets = (_exportMode == ExportMode.SpecificComponentOnly && _specificComponentTarget != null) ||
                                 (_exportMode != ExportMode.SpecificComponentOnly && _currentDisplayTargets.Count > 0);
        string previewData = previewHasTargets ? GenerateDataJson(true) : "Select objects/component or change mode to see preview.";
        EditorGUILayout.TextArea(previewData.Length > 1000 ? previewData.Substring(0, 1000) + "..." : previewData, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private string GenerateDataJson(bool previewOnly = false)
    {
        FullJsonDataV2 fullData = new FullJsonDataV2(); 

        if (_exportMode == ExportMode.SpecificComponentOnly)
        {
            if (_specificComponentTarget == null)
            {
                return previewOnly ? "No specific component targeted." : "{}";
            }

            GameObject parentGo = _specificComponentTarget.gameObject;
            if (!_includeInactiveGameObjects && !parentGo.activeInHierarchy) {
                 return previewOnly ? $"Parent GameObject '{parentGo.name}' is inactive and not included." : "{}";
            }

            bool isTargetComponentEnabled = true; 
            PropertyInfo enabledProp = _specificComponentTarget.GetType().GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
            if (enabledProp != null && enabledProp.PropertyType == typeof(bool) && enabledProp.CanRead)
            {
                try { isTargetComponentEnabled = (bool)enabledProp.GetValue(_specificComponentTarget, null); }
                catch (System.Exception) { /* Default to true if reading fails */ }
            }

            if (!_includeDisabledComponents && !isTargetComponentEnabled) {
                return previewOnly ? $"Component '{_specificComponentTarget.GetType().Name}' is disabled and not included." : "{}";
            }
            
            fullData.specificComponent = new ComponentDataContextual
            {
                parentGameObjectName = parentGo.name,
                parentGameObjectInstanceID = parentGo.GetInstanceID(),
                isParentGameObjectActiveInHierarchy = parentGo.activeInHierarchy,
                type = _specificComponentTarget.GetType().FullName,
                instanceID = _specificComponentTarget.GetInstanceID(),
                componentIsEnabled = isTargetComponentEnabled,
                properties = ExtractProperties(_specificComponentTarget)
            };
        }
        else 
        {
            if (_currentDisplayTargets.Count == 0)
            {
                 return previewOnly ? "No objects selected for preview in current mode." : "{}";
            }

            List<GameObjectData> gameObjectsData = new List<GameObjectData>();
            List<AssetData> assetsData = new List<AssetData>();

            foreach (var target in _currentDisplayTargets)
            {
                if (target == null) continue;
                if (target is GameObject go)
                {
                    ProcessGameObjectRecursive(go, gameObjectsData, 0);
                }
                else if (target is Object && !(target is Component) && AssetDatabase.Contains(target))
                {
                    ProcessAsset(target, assetsData);
                }
            }
            fullData.gameObjects = gameObjectsData.Count > 0 ? gameObjectsData : null;
            fullData.assets = assetsData.Count > 0 ? assetsData : null;
        }
        
        if (fullData.specificComponent == null && fullData.gameObjects == null && fullData.assets == null) {
            return previewOnly ? "No data generated for current targets/settings." : "{}";
        }

        return JsonUtility.ToJson(fullData, true);
    }

    private void ProcessGameObjectRecursive(GameObject go, List<GameObjectData> dataList, int depth)
    {
        if (go == null) return; 

        bool isTopLevelInSelection = _currentDisplayTargets.Contains(go);

        if (isTopLevelInSelection) 
        {
            if (!_includeInactiveGameObjects && !go.activeInHierarchy) return;
        }
        else 
        {
            if (!_includeInactiveGameObjects && !go.activeSelf) return;
        }
        
        if (depth >= _maxHierarchyDepth && _maxHierarchyDepth > 0 ) return; 

        string layerNameStr = "UnknownLayer";
        try
        {
            if (go.layer >= 0 && go.layer <= 31) 
            {
                layerNameStr = LayerMask.LayerToName(go.layer);
                if (string.IsNullOrEmpty(layerNameStr)) 
                {
                    layerNameStr = $"Layer {go.layer} (Unnamed)";
                }
            }
            else
            {
                layerNameStr = $"InvalidLayerIndex:{go.layer}";
            }
        }
        catch (System.Exception ex) 
        {
            layerNameStr = $"ErrorFetchingLayerName ({go.layer}): {ex.GetType().Name}";
            Debug.LogWarning($"Error fetching layer name for GameObject '{go.name}' (layer int: {go.layer}): {ex.Message}");
        }

        GameObjectData goData = new GameObjectData
        {
            name = go.name,
            instanceID = go.GetInstanceID(),
            isActiveSelf = go.activeSelf,
            isActiveInHierarchy = go.activeInHierarchy,
            tag = go.tag,
            layer = layerNameStr, 
            transformInfo = new TransformData
            {
                position = go.transform.position,
                rotation = go.transform.eulerAngles,
                localScale = go.transform.localScale,
                localPosition = go.transform.localPosition,
                localRotation = go.transform.localEulerAngles
            },
            components = new List<ComponentData>()
        };

        Component[] components = go.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null) continue;

            bool isComponentEnabled = true;
            PropertyInfo enabledProp = component.GetType().GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
            if (enabledProp != null && enabledProp.PropertyType == typeof(bool) && enabledProp.CanRead)
            {
                try { isComponentEnabled = (bool)enabledProp.GetValue(component, null); }
                catch { /* Mute */ }
            }

            if (!_includeDisabledComponents && !isComponentEnabled && 
                (component is Behaviour || component is Renderer || component is Collider || component is LODGroup || component is Light || component is ParticleSystem || component is Animation) )
            {
                continue;
            }

            ComponentData compData = new ComponentData
            {
                type = component.GetType().FullName,
                instanceID = component.GetInstanceID(),
                properties = ExtractProperties(component)
            };
            goData.components.Add(compData);
        }
        if(goData.components.Count == 0) goData.components = null;
        dataList.Add(goData);

        if ((_maxHierarchyDepth <= 0 || depth < _maxHierarchyDepth - 1))
        {
            goData.children = new List<GameObjectData>();
            for (int i = 0; i < go.transform.childCount; i++)
            {
                ProcessGameObjectRecursive(go.transform.GetChild(i).gameObject, goData.children, depth + 1);
            }
            if (goData.children.Count == 0) goData.children = null;
        }
    }

    private void ProcessAsset(Object asset, List<AssetData> assetDataList)
    {
        if (asset == null) return;
        AssetData assetInfo = new AssetData
        {
            name = asset.name,
            instanceID = asset.GetInstanceID(),
            type = asset.GetType().FullName,
            assetPath = AssetDatabase.GetAssetPath(asset),
            properties = ExtractProperties(asset)
        };
        assetDataList.Add(assetInfo);
    }

    private List<PropertyData> ExtractProperties(Object obj)
    {
        List<PropertyData> properties = new List<PropertyData>();
        if (obj == null) return properties;

        var fieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var propertyFlags = BindingFlags.Public | BindingFlags.Instance;
        System.Type currentType = obj.GetType();
        HashSet<string> processedNames = new HashSet<string>();

        if (obj is Transform transformComponent)
        {
            properties.Add(new PropertyData { name = "position", value = ConvertValueToString(transformComponent.position, 0) });
            properties.Add(new PropertyData { name = "rotation (Euler)", value = ConvertValueToString(transformComponent.eulerAngles, 0) });
            properties.Add(new PropertyData { name = "localScale", value = ConvertValueToString(transformComponent.localScale, 0) });
            properties.Add(new PropertyData { name = "localPosition", value = ConvertValueToString(transformComponent.localPosition, 0) });
            properties.Add(new PropertyData { name = "localRotation (Euler)", value = ConvertValueToString(transformComponent.localEulerAngles, 0) });
            processedNames.UnionWith(new string[] {"position", "rotation", "eulerAngles", "localScale", "localPosition", "localEulerAngles", "localRotation"});
        }

        while (currentType != null && currentType != typeof(UnityEngine.Object) && currentType != typeof(System.Object))
        {
            FieldInfo[] fields = currentType.GetFields(fieldFlags | BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in fields)
            {
                if (processedNames.Contains(field.Name)) continue;
                processedNames.Add(field.Name);

                if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                {
                    if (field.GetCustomAttribute<HideInInspector>() != null || field.GetCustomAttribute<System.NonSerializedAttribute>() != null)
                        continue;
                    if (field.Name.StartsWith("<") && field.Name.EndsWith(">k__BackingField"))
                        continue;
                    try
                    {
                        object value = field.GetValue(obj);
                        properties.Add(new PropertyData { name = field.Name, value = ConvertValueToString(value, 0) });
                    }
                    catch (System.Exception) { /* Mute */ }
                }
            }

            PropertyInfo[] props = currentType.GetProperties(propertyFlags | BindingFlags.DeclaredOnly);
            foreach (PropertyInfo propInfo in props)
            {
                if (processedNames.Contains(propInfo.Name)) continue;
                if (propInfo.GetIndexParameters().Length > 0) continue;
                processedNames.Add(propInfo.Name);

                if (propInfo.CanRead)
                {
                    if (propInfo.GetCustomAttribute<HideInInspector>() != null || propInfo.GetCustomAttribute<System.ObsoleteAttribute>() != null)
                        continue;
                    
                    if (propInfo.Name == "runInEditMode" && !(obj is MonoBehaviour)) continue; 
                    if (propInfo.Name == "useGUILayout" && !(obj is MonoBehaviour)) continue; 
                    if (propInfo.Name == "tag" && !(obj is GameObject) && !(obj is Component)) continue;
                    if (propInfo.Name == "enabled" && !(obj is Behaviour || obj is Renderer || obj is Collider || obj is LODGroup || obj is Light || obj is Camera || obj is Animation || obj is ParticleSystem)) continue;
                    if (propInfo.Name == "gameObject" && obj is Component) continue; 
                    if (propInfo.Name == "transform" && obj is Component) continue;
                    if (propInfo.Name == "material" && obj is Renderer && ((Renderer)obj).sharedMaterial != null) continue;
                    if (propInfo.Name == "materials" && obj is Renderer && ((Renderer)obj).sharedMaterials.Length > 0) continue;

                    if (IsSerializableType(propInfo.PropertyType))
                    {
                        try
                        {
                            object value = propInfo.GetValue(obj);
                            properties.Add(new PropertyData { name = propInfo.Name, value = ConvertValueToString(value, 0) });
                        }
                        catch (TargetInvocationException) { properties.Add(new PropertyData { name = propInfo.Name, value = "Error: TargetInvocationException" }); }
                        catch (System.Exception) { properties.Add(new PropertyData { name = propInfo.Name, value = "Error: Exception getting value" }); }
                    }
                }
            }
            currentType = currentType.BaseType;
        }
        
        if (obj is UnityEngine.Object uObj) {
            if (!processedNames.Contains("name")) {
                properties.Add(new PropertyData { name = "name", value = uObj.name });
            }
            if (!processedNames.Contains("hideFlags")) properties.Add(new PropertyData { name = "hideFlags", value = uObj.hideFlags.ToString() });
        }

        return properties.Count > 0 ? properties.OrderBy(p => p.name).ToList() : null;
    }

    private bool IsSerializableType(System.Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type.IsEnum ||
               IsSimpleUnityType(type) ||
               typeof(UnityEngine.Object).IsAssignableFrom(type) ||
               (type.IsArray && type.GetArrayRank() == 1 && IsSerializableType(type.GetElementType())) || 
               (typeof(System.Collections.IList).IsAssignableFrom(type) && type.IsGenericType && type.GetGenericArguments().Length == 1 && IsSerializableType(type.GetGenericArguments()[0]));
    }

    private bool IsSimpleUnityType(System.Type type)
    {
        return type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
               type == typeof(Quaternion) || type == typeof(Color) || type == typeof(Color32) || type == typeof(Rect) ||
               type == typeof(Bounds) || type == typeof(Matrix4x4) || type == typeof(AnimationCurve) ||
               type == typeof(Gradient) || type == typeof(Vector2Int) || type == typeof(Vector3Int) || type == typeof(RectInt) || type == typeof(BoundsInt) ||
               type == typeof(LayerMask);
    }

    private string ConvertValueToString(object value, int depth, int maxCollectionDepth = 1, int maxRecursionDepth = 2)
    {
        if (value == null) return "null";
        if (depth > maxRecursionDepth && maxRecursionDepth >=0) return $"Object({value.GetType().FullName}) - Max recursion depth";

        System.Type type = value.GetType();

        if (type.IsPrimitive || type == typeof(string)) return value.ToString();
        if (type.IsEnum) return value.ToString();

        if (value is LayerMask lm)
        {
            List<string> layerNames = new List<string>();
            if (lm.value == 0) {
                layerNames.Add("Nothing");
            } else if (lm.value == -1) { 
                 layerNames.Add("Everything");
            } else {
                for (int i = 0; i < 32; i++) 
                {
                    if ((lm.value & (1 << i)) != 0) 
                    {
                        string layerName = "";
                        try
                        {
                            if (i >= 0 && i <= 31) { layerName = LayerMask.LayerToName(i); }
                            else { layerName = $"InvalidLayerIndex:{i}"; }
                        }
                        catch (System.Exception ex) { layerName = $"LayerIndex:{i} (NameError: {ex.GetType().Name})"; }
                        
                        if (string.IsNullOrEmpty(layerName)) { layerNames.Add($"Layer {i} (Unnamed)"); }
                        else { layerNames.Add(layerName); }
                    }
                }
            }
            if (layerNames.Count == 0 && lm.value != 0 && lm.value != -1) { 
                return $"LayerMask (Value: {lm.value}, No recognized layers in mask)";
            }
            return $"LayerMask ({string.Join(", ", layerNames)} / Value: {lm.value})";
        }
        
        if (value is Vector2 v2) return $"{{x:{v2.x:F3}, y:{v2.y:F3}}}";
        if (value is Vector3 v3) return $"{{x:{v3.x:F3}, y:{v3.y:F3}, z:{v3.z:F3}}}";
        if (value is Vector4 v4) return $"{{x:{v4.x:F3}, y:{v4.y:F3}, z:{v4.z:F3}, w:{v4.w:F3}}}";
        if (value is Quaternion q) return $"{{x:{q.x:F3}, y:{q.y:F3}, z:{q.z:F3}, w:{q.w:F3}}} (Euler: {q.eulerAngles.ToString("F1")})";
        if (value is Color c) return $"{{r:{c.r:F3}, g:{c.g:F3}, b:{c.b:F3}, a:{c.a:F3}}}";
        if (value is Color32 c32) return $"{{r:{c32.r}, g:{c32.g}, b:{c32.b}, a:{c32.a}}}";
        if (value is Rect r) return $"{{x:{r.x:F2}, y:{r.y:F2}, w:{r.width:F2}, h:{r.height:F2}}}";
        if (value is Bounds bVal) return $"{{c:{ConvertValueToString(bVal.center, depth + 1, maxCollectionDepth, maxRecursionDepth)}, s:{ConvertValueToString(bVal.size, depth + 1, maxCollectionDepth, maxRecursionDepth)}}}";
        if (value is Vector2Int v2i) return $"{{x:{v2i.x}, y:{v2i.y}}}";
        if (value is Vector3Int v3i) return $"{{x:{v3i.x}, y:{v3i.y}, z:{v3i.z}}}";
        if (value is RectInt ri) return $"{{x:{ri.x}, y:{ri.y}, w:{ri.width}, h:{ri.height}}}";
        if (value is BoundsInt bi) return $"{{pos:{ConvertValueToString(bi.position, depth + 1, maxCollectionDepth, maxRecursionDepth)}, size:{ConvertValueToString(bi.size, depth + 1, maxCollectionDepth, maxRecursionDepth)}}}";
        if (value is AnimationCurve ac) return $"AnimationCurve ({ac.length} keys)";
        if (value is Gradient grad) return $"Gradient ({grad.colorKeys.Length}C, {grad.alphaKeys.Length}A)";
        if (value is Matrix4x4) return $"Matrix4x4 (not expanded)";

        if (value is UnityEngine.Object unityObj)
        {
            string path = AssetDatabase.Contains(unityObj) ? AssetDatabase.GetAssetPath(unityObj) : "";
            return $"{unityObj.GetType().Name} \"{unityObj.name}\" (ID:{unityObj.GetInstanceID()}){(string.IsNullOrEmpty(path) ? "" : $" Path: '{path}'")}";
        }

        if (type.IsArray && type.GetArrayRank() == 1)
        {
            System.Array arr = value as System.Array;
            if (arr.Length == 0) return $"Array<{type.GetElementType()?.Name ?? "Unknown"}> (Empty)";
            if (depth >= maxCollectionDepth && arr.Length > 0 && maxCollectionDepth >=0) return $"Array<{type.GetElementType()?.Name ?? "Unknown"}> (Length: {arr.Length})";
            
            StringBuilder sb = new StringBuilder();
            sb.Append($"Array<{type.GetElementType()?.Name ?? "Unknown"}> [");
            int count = 0;
            for (int i = 0; i < arr.Length; i++) 
            {
                if (count >= 5 && arr.Length > 5) { sb.Append(", ..."); break; }
                sb.Append(ConvertValueToString(arr.GetValue(i), depth + 1, maxCollectionDepth, maxRecursionDepth));
                if (i < arr.Length - 1 && (count < 4 || arr.Length <= 5)) sb.Append(", ");
                count++;
            }
            sb.Append("]");
            return sb.ToString();
        }
        if (typeof(System.Collections.IList).IsAssignableFrom(type) && type.IsGenericType && type.GetGenericArguments().Length == 1)
        {
             System.Collections.IList list = value as System.Collections.IList;
            if (list.Count == 0) return $"List<{type.GetGenericArguments()[0]?.Name ?? "Unknown"}> (Empty)";
            if (depth >= maxCollectionDepth && list.Count > 0 && maxCollectionDepth >=0) return $"List<{type.GetGenericArguments()[0]?.Name ?? "Unknown"}> (Count: {list.Count})";

            StringBuilder sb = new StringBuilder();
            sb.Append($"List<{type.GetGenericArguments()[0]?.Name ?? "Unknown"}> [");
            int count = 0;
            for (int i = 0; i < list.Count; i++) 
            {
                if (count >= 5 && list.Count > 5) { sb.Append(", ..."); break; }
                sb.Append(ConvertValueToString(list[i], depth + 1, maxCollectionDepth, maxRecursionDepth));
                if (i < list.Count - 1 && (count < 4 || list.Count <= 5)) sb.Append(", ");
                count++;
            }
            sb.Append("]");
            return sb.ToString();
        }
        return $"Object({type.FullName}) - Not directly serialized";
    }
}

// --- Data structures (FullJsonDataV2, ComponentDataContextual, AssetData, GameObjectData, TransformData, ComponentData, PropertyData) ---
[System.Serializable]
public class FullJsonDataV2
{
    public ComponentDataContextual specificComponent; 
    public List<GameObjectData> gameObjects;
    public List<AssetData> assets;
}

[System.Serializable]
public class ComponentDataContextual
{
    public string parentGameObjectName;
    public int parentGameObjectInstanceID;
    public bool isParentGameObjectActiveInHierarchy;
    public string type; 
    public int instanceID; 
    public bool componentIsEnabled; 
    public List<PropertyData> properties;
}

[System.Serializable]
public class AssetData
{
    public string name;
    public int instanceID;
    public string type;
    public string assetPath;
    public List<PropertyData> properties;
}

[System.Serializable]
public class GameObjectData
{
    public string name;
    public int instanceID;
    public bool isActiveSelf;
    public bool isActiveInHierarchy;
    public string tag;
    public string layer;
    public TransformData transformInfo;
    public List<ComponentData> components;
    public List<GameObjectData> children;
}

[System.Serializable]
public class TransformData
{
    public Vector3 position;
    public Vector3 rotation; 
    public Vector3 localScale;
    public Vector3 localPosition;
    public Vector3 localRotation; 
}

[System.Serializable]
public class ComponentData 
{
    public string type;
    public int instanceID;
    public List<PropertyData> properties;
}

[System.Serializable]
public class PropertyData
{
    public string name;
    public string value;
}