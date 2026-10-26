#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalSouk.Editor
{
    public static class CarSetupEditor
    {
        private const string DriveUxmlPath = "Assets/UI Toolkit/DriveControle.uxml";

        [MenuItem("Tools/Digital Souk/Setup Car Driving UI & Camera", false, 20)]
        public static void SetupCarInCurrentScene()
        {
            // 1. Locate Car Controller
            SimpleArcadeCarMouves carController = Object.FindFirstObjectByType<SimpleArcadeCarMouves>();
            if (carController == null)
            {
                Debug.LogError("[Digital Souk] Could not find any GameObject with SimpleArcadeCarMouves in the scene.");
                return;
            }
            GameObject carGO = carController.gameObject;

            // 2. Ensure CarEnterExitUIBuilder exists on Car
            CarEnterExitUIBuilder enterExit = carGO.GetComponent<CarEnterExitUIBuilder>();
            if (enterExit == null)
            {
                enterExit = Undo.AddComponent<CarEnterExitUIBuilder>(carGO);
                Debug.Log($"<color=#38bdf8>[Digital Souk]</color> Added CarEnterExitUIBuilder to '{carGO.name}'.");
            }

            // Ensure InteractionZone is centered and marked as trigger
            Transform interactionZone = carGO.transform.Find("InteractionZone");
            if (interactionZone != null)
            {
                interactionZone.localPosition = new Vector3(0f, 0.5f, 0f);
                interactionZone.localScale = new Vector3(3f, 2f, 4f);
                Collider zoneCol = interactionZone.GetComponent<Collider>();
                if (zoneCol != null) zoneCol.isTrigger = true;
                EditorUtility.SetDirty(interactionZone.gameObject);
            }

            // 3. Locate or Setup UIDocument on DrivePrompt
            UIDocument driveUiDoc = null;
            GameObject drivePromptGO = GameObject.Find("DrivePrompt");
            if (drivePromptGO != null)
            {
                driveUiDoc = drivePromptGO.GetComponent<UIDocument>();
            }
            if (driveUiDoc == null)
            {
                driveUiDoc = Object.FindFirstObjectByType<UIDocument>();
            }

            // Ensure UXML is assigned
            VisualTreeAsset driveUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DriveUxmlPath);
            if (driveUiDoc != null && driveUxml != null)
            {
                driveUiDoc.visualTreeAsset = driveUxml;
                Debug.Log("<color=#38bdf8>[Digital Souk]</color> Assigned DriveControle.uxml to UIDocument.");
            }

            // 4. Ensure SmoothCarCamera exists on Main Camera
            Camera mainCam = Camera.main;
            SmoothCarCamera carCam = null;
            if (mainCam != null)
            {
                carCam = mainCam.GetComponent<SmoothCarCamera>();
                if (carCam == null)
                {
                    carCam = Undo.AddComponent<SmoothCarCamera>(mainCam.gameObject);
                }
                carCam.playerCamFollow = mainCam.GetComponent<camFollow>();
                carCam.target = carGO.transform;
                carCam.distance = 12f;
                carCam.targetOffset = Vector3.zero;
                carCam.enabled = false; // Start disabled until player enters vehicle
                Debug.Log("<color=#38bdf8>[Digital Souk]</color> Configured SmoothCarCamera on Main Camera.");
            }

            // 5. Wire CarEnterExitUIBuilder references
            enterExit.carController = carController;
            enterExit.DriveUi = driveUiDoc;
            enterExit.carCameraFollow = carCam;

            Transform exitPoint = carGO.transform.Find("ExitPoint");
            if (exitPoint != null)
            {
                exitPoint.localPosition = new Vector3(-1.0f, 0.35f, -1.33f);
                enterExit.exitPoint = exitPoint;
            }

            // Ensure car starts disabled
            carController.isBeingDriven = false;
            carController.enabled = false;

            EditorUtility.SetDirty(carGO);
            if (driveUiDoc != null) EditorUtility.SetDirty(driveUiDoc.gameObject);
            if (carCam != null) EditorUtility.SetDirty(carCam.gameObject);
            if (exitPoint != null) EditorUtility.SetDirty(exitPoint.gameObject);

            // SAVE SCENE EXPLICITLY TO DISK
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Selection.activeGameObject = carGO;
            EditorGUIUtility.PingObject(carGO);

            Debug.Log("<color=#38bdf8><b>[Digital Souk]</b></color> Car Enter/Exit, Driving UI, and Camera Setup completed and SAVED! Press Play to test.");
        }
    }
}
#endif
