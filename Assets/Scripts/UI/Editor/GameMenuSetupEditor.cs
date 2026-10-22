#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DigitalSouk.UI.Editor
{
    public static class GameMenuSetupEditor
    {
        private const string UxmlPath = "Assets/UI/UXML/GameMenus.uxml";
        private const string PanelSettingsPath = "Assets/UI/DigitalSoukPanelSettings.asset";
        private const string GameObjectName = "[UI] GameMenuManager";

        [MenuItem("Tools/Digital Souk/Setup UI Toolkit Menus In Scene", false, 10)]
        public static void SetupMenusInCurrentScene()
        {
            // 1. Ensure EventSystem exists for New Input System & UI Toolkit
            EnsureEventSystem();

            // 2. Ensure PanelSettings exists with clean 1920x1080 responsive scaling
            PanelSettings panelSettings = EnsurePanelSettings();

            // 3. Check if GameMenuManager already exists in scene
            GameMenuManager existing = Object.FindFirstObjectByType<GameMenuManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log($"<color=#38bdf8>[Digital Souk]</color> GameMenuManager already exists in this scene on '{existing.gameObject.name}'.");
                return;
            }

            // 4. Load UXML asset
            VisualTreeAsset uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (uxmlAsset == null)
            {
                Debug.LogError($"[Digital Souk] Could not locate UXML asset at '{UxmlPath}'. Please verify the file exists.");
                return;
            }

            // 5. Create UI GameObject
            GameObject menuGO = new GameObject(GameObjectName);
            Undo.RegisterCreatedObjectUndo(menuGO, "Create Digital Souk UI Manager");

            // Attach UIDocument
            UIDocument uiDoc = menuGO.AddComponent<UIDocument>();
            uiDoc.visualTreeAsset = uxmlAsset;
            if (panelSettings != null)
            {
                uiDoc.panelSettings = panelSettings;
            }

            // Attach GameMenuManager
            GameMenuManager manager = menuGO.AddComponent<GameMenuManager>();

            // Auto-link references
            movement move = Object.FindFirstObjectByType<movement>();
            camFollow cam = Object.FindFirstObjectByType<camFollow>();

            SerializedObject serializedManager = new SerializedObject(manager);
            if (move != null)
            {
                serializedManager.FindProperty("playerMovement").objectReferenceValue = move;
            }
            if (cam != null)
            {
                serializedManager.FindProperty("cameraFollow").objectReferenceValue = cam;
            }
            serializedManager.ApplyModifiedProperties();

            // Mark Scene Dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            // Select in Hierarchy
            Selection.activeGameObject = menuGO;
            EditorGUIUtility.PingObject(menuGO);

            Debug.Log("<color=#38bdf8><b>[Digital Souk]</b></color> UI Toolkit Start Menu, Pause Menu, Settings, and EventSystem are ready! Press Play in Unity to test.");
        }

        private static void EnsureEventSystem()
        {
            EventSystem existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (existingEventSystem == null)
            {
                GameObject esGO = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<InputSystemUIInputModule>();
                Debug.Log("<color=#38bdf8>[Digital Souk]</color> Created EventSystem with InputSystemUIInputModule.");
            }
            else if (existingEventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                existingEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("<color=#38bdf8>[Digital Souk]</color> Added InputSystemUIInputModule to existing EventSystem.");
            }
        }

        private static PanelSettings EnsurePanelSettings()
        {
            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1920, 1080);
                settings.match = 0.5f;

                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=#38bdf8>[Digital Souk]</color> Created responsive PanelSettings asset at '{PanelSettingsPath}'.");
            }
            return settings;
        }
    }
}
#endif
