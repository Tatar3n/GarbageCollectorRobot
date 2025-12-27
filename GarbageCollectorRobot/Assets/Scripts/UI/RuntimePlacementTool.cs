using System;
using UnityEngine;

public class RuntimePlacementTool : MonoBehaviour
{
    private enum PlacementMode
    {
        Obstacles = 0,
        Garbage = 1
    }

    [Header("Runtime UI")]
    [SerializeField] private bool placementEnabled = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.P;

    private PlacementMode mode = PlacementMode.Obstacles;
    private Types.GType selectedGarbageType = Types.GType.Red;
    private int selectedObstacleIndex = 0;

    private Camera cam;
    private Rect uiRect;

    private GameObject garbagePrefab;
    private GameObject[] obstaclePrefabs;
    private string[] obstacleLabels;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        uiRect = new Rect(10f, 10f, 280f, 220f);

        garbagePrefab = Resources.Load<GameObject>("Placement/Garbage");
        obstaclePrefabs = new[]
        {
            Resources.Load<GameObject>("Placement/Obstacle_Box"),
            Resources.Load<GameObject>("Placement/Obstacle_Stone"),
            Resources.Load<GameObject>("Placement/Obstacle_TrafficCone"),
        };

        obstacleLabels = new[] { "Box", "Stone", "Traffic Cone" };
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            placementEnabled = !placementEnabled;
        }

        if (!placementEnabled) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 mouseGui = Input.mousePosition;
        mouseGui.y = Screen.height - mouseGui.y;
        if (uiRect.Contains(mouseGui)) return;

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceAtMouse();
        }
    }

    private void OnGUI()
    {
        uiRect.height = mode == PlacementMode.Garbage ? 250f : 235f;

        GUILayout.BeginArea(uiRect, GUI.skin.window);
        GUILayout.Label("Placement UI");

        placementEnabled = GUILayout.Toggle(placementEnabled, "Enable placing (toggle: P)");

        mode = (PlacementMode)GUILayout.Toolbar((int)mode, new[] { "Obstacles", "Garbage" });
        GUILayout.Space(6);

        if (mode == PlacementMode.Obstacles)
        {
            GUILayout.Label("Obstacle prefab:");
            selectedObstacleIndex = GUILayout.SelectionGrid(selectedObstacleIndex, obstacleLabels, 1);
        }
        else
        {
            GUILayout.Label("Garbage color:");
            int sel = GUILayout.Toolbar(TypeToIndex(selectedGarbageType), new[] { "Red", "Blue", "Yellow" });
            selectedGarbageType = IndexToType(sel);
        }

        GUILayout.Space(8);

        if (garbagePrefab == null)
        {
            GUILayout.Label("ERROR: Resources/Placement/Garbage prefab not found.");
        }
        if (mode == PlacementMode.Obstacles && (selectedObstacleIndex < 0 || selectedObstacleIndex >= obstaclePrefabs.Length || obstaclePrefabs[selectedObstacleIndex] == null))
        {
            GUILayout.Label("ERROR: Obstacle prefab not found in Resources/Placement.");
        }

        GUILayout.Label("Left click in world: place");
        GUILayout.EndArea();
    }

    private void TryPlaceAtMouse()
    {
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        if (mode == PlacementMode.Garbage)
        {
            if (garbagePrefab == null) return;

            GameObject go = Instantiate(garbagePrefab, world, Quaternion.identity);
            if (go.TryGetComponent<KeepGarbage>(out var keep))
            {
                keep.garbageType = selectedGarbageType;
                keep.ApplyVisuals();
            }
            return;
        }

        if (selectedObstacleIndex < 0 || selectedObstacleIndex >= obstaclePrefabs.Length) return;
        GameObject prefab = obstaclePrefabs[selectedObstacleIndex];
        if (prefab == null) return;

        Instantiate(prefab, world, Quaternion.identity);
    }

    private static int TypeToIndex(Types.GType t)
    {
        return t switch
        {
            Types.GType.Red => 0,
            Types.GType.Blue => 1,
            Types.GType.Yellow => 2,
            _ => 0
        };
    }

    private static Types.GType IndexToType(int i)
    {
        return i switch
        {
            0 => Types.GType.Red,
            1 => Types.GType.Blue,
            2 => Types.GType.Yellow,
            _ => Types.GType.Red
        };
    }
}

