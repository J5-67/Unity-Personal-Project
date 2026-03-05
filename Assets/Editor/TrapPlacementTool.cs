using UnityEditor;
using UnityEngine;
using Trap;

public class TrapPlacementTool : EditorWindow
{
    private GameObject spikePrefab;
    private GameObject movingSawPrefab;
    private GameObject laserHazardPrefab;

    [MenuItem("Tools/Trap Placement Tool")]
    public static void ShowWindow()
    {
        GetWindow<TrapPlacementTool>("Trap Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Trap Prefabs", EditorStyles.boldLabel);
        
        spikePrefab = (GameObject)EditorGUILayout.ObjectField("Spike Prefab", spikePrefab, typeof(GameObject), false);
        movingSawPrefab = (GameObject)EditorGUILayout.ObjectField("Moving Saw Prefab", movingSawPrefab, typeof(GameObject), false);
        laserHazardPrefab = (GameObject)EditorGUILayout.ObjectField("Laser Hazard Prefab", laserHazardPrefab, typeof(GameObject), false);

        GUILayout.Space(10);
        GUILayout.Label("Quick Place", EditorStyles.boldLabel);

        if (GUILayout.Button("Place Spike at Origin"))
        {
            PlaceTrap(spikePrefab, "Spike");
        }

        if (GUILayout.Button("Place Moving Saw at Origin"))
        {
            PlaceTrap(movingSawPrefab, "MovingSaw");
        }

        if (GUILayout.Button("Place Laser Hazard at Origin"))
        {
            PlaceTrap(laserHazardPrefab, "LaserHazard");
        }
    }

    private void PlaceTrap(GameObject prefab, string defaultName)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[Trap Placement Tool] Please assign the {defaultName} prefab first!");
            return;
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        
        // Scene View 카메라가 보고 있는 위치 근처에 배치
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            go.transform.position = sceneView.pivot;
        }
        else
        {
            go.transform.position = Vector3.zero;
        }

        Undo.RegisterCreatedObjectUndo(go, $"Create {defaultName}");
        Selection.activeGameObject = go;
    }
}
