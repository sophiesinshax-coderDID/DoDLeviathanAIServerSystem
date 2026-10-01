using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace DivisionOfDivinity.EditorTools
{
    public class UIRepairTool : EditorWindow
    {
        [MenuItem("Tools/Division of Divinity/Fix All Scene UIs & Scripts")]
        public static void ShowWindow()
        {
            GetWindow<UIRepairTool>("Comprehensive Repair Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("Division of Divinity - Comprehensive Repair", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Repair Cameras, Canvases, & Text", GUILayout.Height(45)))
            {
                RepairEverything();
            }
        }

        private static void RepairEverything()
        {
            // 1. Ensure a Main Camera exists
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
                Debug.Log("[UIRepairTool]: Created missing Main Camera.");
            }

            // 2. Fix all Canvases
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            int canvasFixedCount = 0;

            foreach (var canvas in allCanvases)
            {
                Undo.RecordObject(canvas, "Repair Canvas Settings");
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null))
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = mainCam;
                    canvas.planeDistance = 100f;
                    canvasFixedCount++;
                }
            }

            // 3. Sweep GameObjects for Missing Scripts & Refresh Text Components
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int missingScriptsRemoved = 0;
            int textFixedCount = 0;

            foreach (var obj in allObjects)
            {
                int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                if (removedCount > 0)
                {
                    missingScriptsRemoved += removedCount;
                    EditorUtility.SetDirty(obj);
                }

                var tmpComponents = obj.GetComponents<TextMeshProUGUI>();
                foreach (var tmp in tmpComponents)
                {
                    if (tmp != null)
                    {
                        Undo.RecordObject(tmp, "Refresh TMP Component");
                        tmp.enabled = false;
                        tmp.enabled = true;
                        tmp.SetAllDirty();
                        textFixedCount++;
                    }
                }
            }

            Debug.Log($"[UIRepairTool]: Repair complete! Fixed {canvasFixedCount} canvases, stripped {missingScriptsRemoved} dead script references, and refreshed {textFixedCount} text components.");
            EditorUtility.DisplayDialog("Repair Successful", 
                $"Repaired Canvases: {canvasFixedCount}\n" +
                $"Removed Missing Scripts: {missingScriptsRemoved}\n" +
                $"Refreshed Text Components: {textFixedCount}", "OK");
        }
    }
}