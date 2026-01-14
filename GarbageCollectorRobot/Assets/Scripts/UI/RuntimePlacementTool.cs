using System;
using UnityEngine;

public class RuntimePlacementTool : MonoBehaviour
{
    private enum PlacementMode
    {
        Obstacles = 0,
        Garbage = 1,
        TrashBins = 2,
        Delete = 3
    }

    [Header("Runtime UI")]
    [SerializeField] private bool placementEnabled = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.P;
    [SerializeField] private KeyCode collapseKey = KeyCode.O;

    private PlacementMode mode = PlacementMode.Obstacles;
    private Types.GType selectedGarbageType = Types.GType.Red;
    private Types.GType selectedTrashBinType = Types.GType.Red;
    private int selectedObstacleIndex = 0;
    private bool uiCollapsed = false;

    private Camera cam;
    private Rect uiRect;

    private GameObject garbagePrefab;
    private GameObject trashBinPrefab;
    private GameObject[] obstaclePrefabs;
    private string[] obstacleLabels;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        uiRect = new Rect(10f, 10f, 300f, 220f);

        garbagePrefab = Resources.Load<GameObject>("Placement/Garbage");
        trashBinPrefab = Resources.Load<GameObject>("Placement/Trashbin");
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
        if (Input.GetKeyDown(collapseKey))
        {
            uiCollapsed = !uiCollapsed;
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
        if (uiCollapsed)
        {
            uiRect.height = 60f;
        }
        else
        {
            if (mode == PlacementMode.Garbage) uiRect.height = 260f;
            else if (mode == PlacementMode.TrashBins) uiRect.height = 280f;
            else if (mode == PlacementMode.Delete) uiRect.height = 170f;
            else uiRect.height = 245f;
        }

        GUILayout.BeginArea(uiRect, GUI.skin.window);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Для расстановки");
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(uiCollapsed ? "Развернуть" : "Свернуть", GUILayout.Width(80)))
        {
            uiCollapsed = !uiCollapsed;
        }
        GUILayout.EndHorizontal();

        placementEnabled = GUILayout.Toggle(placementEnabled, "Включить размещение (P)");
        GUILayout.Label("Свернуть окно (O)");

        if (uiCollapsed)
        {
            GUILayout.EndArea();
            return;
        }

        mode = (PlacementMode)GUILayout.Toolbar((int)mode, new[] { "Obstacles", "Garbage", "TrashBins", "Delete" });
        GUILayout.Space(6);

        if (mode == PlacementMode.Obstacles)
        {
            GUILayout.Label("Префаб препятсвия:");
            selectedObstacleIndex = GUILayout.SelectionGrid(selectedObstacleIndex, obstacleLabels, 1);
        }
        else if (mode == PlacementMode.Garbage)
        {
            GUILayout.Label("Цвет мусора:");
            int sel = GUILayout.Toolbar(TypeToIndex(selectedGarbageType), new[] { "Red", "Blue", "Yellow" });
            selectedGarbageType = IndexToType(sel);
        }
        else if (mode == PlacementMode.TrashBins)
        {
            GUILayout.Label("Тим мусорки:");
            int sel = GUILayout.Toolbar(TypeToIndex(selectedTrashBinType), new[] { "Red", "Blue", "Yellow" });
            selectedTrashBinType = IndexToType(sel);
        }
        else
        {
            GUILayout.Label("Удаление:");
            GUILayout.Label("Левый клик для удаления");
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
        if (mode == PlacementMode.TrashBins && trashBinPrefab == null)
        {
            GUILayout.Label("ERROR: Resources/Placement/Trashbin prefab not found.");
        }

        if (mode != PlacementMode.Delete)
            GUILayout.Label("Левый клик, чтобы поставить");
        GUILayout.EndArea();
    }

    private void TryPlaceAtMouse()
    {
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        if (mode == PlacementMode.Delete)
        {
            TryDeleteAt(world);
            return;
        }

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

        if (mode == PlacementMode.TrashBins)
        {
            if (trashBinPrefab == null) return;
            GameObject go = Instantiate(trashBinPrefab, world, Quaternion.identity);
            if (go.TryGetComponent<TrashBin>(out var bin))
            {
                bin.garbageType = selectedTrashBinType;
                bin.ApplyVisuals();
            }
            return;
        }

        if (selectedObstacleIndex < 0 || selectedObstacleIndex >= obstaclePrefabs.Length) return;
        GameObject prefab = obstaclePrefabs[selectedObstacleIndex];
        if (prefab == null) return;

        Instantiate(prefab, world, Quaternion.identity);
    }

    private void TryDeleteAt(Vector2 world)
    {
        Collider2D col = Physics2D.OverlapPoint(world);
        if (col == null) return;

        KeepGarbage keep = col.GetComponentInParent<KeepGarbage>();
        if (keep != null)
        {
            Destroy(keep.gameObject);
            return;
        }

        DeathObject death = col.GetComponentInParent<DeathObject>();
        if (death != null)
        {
            Destroy(death.gameObject);
            return;
        }
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

