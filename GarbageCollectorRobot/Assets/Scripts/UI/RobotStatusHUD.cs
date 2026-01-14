using UnityEngine;

namespace UI
{
    public class RobotStatusHUD : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private bool visible = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.H;
        [SerializeField] private Vector2 margin = new Vector2(10f, 10f);
        [SerializeField] private Vector2 size = new Vector2(320f, 150f);

        private Fuzzy.FuzzyLogicSystem robot;
        private Inventory inventory;
        private Rigidbody2D rb;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
            }

            if (robot == null)
            {
                robot = FindObjectOfType<Fuzzy.FuzzyLogicSystem>();
                if (robot != null)
                {
                    rb = robot.GetComponent<Rigidbody2D>();
                    inventory = robot.GetComponentInParent<Inventory>();
                    if (inventory == null) inventory = robot.GetComponent<Inventory>();
                }
            }
        }

        private void OnGUI()
        {
            if (!visible) return;

            float x = Screen.width - size.x - margin.x;
            float y = margin.y;
            Rect rect = new Rect(x, y, size.x, size.y);

            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("Robot status (toggle: H)");

            if (robot == null)
            {
                GUILayout.Label("Robot: not found");
                GUILayout.EndArea();
                return;
            }

            float speed = rb != null ? rb.velocity.magnitude : robot.BaseSpeed;
            float angle = robot.CurrentTurnAngle;
            float memAngle = robot.HasTurnMemory ? robot.RememberedTurnAngle : 0f;
            string memState = robot.HasTurnMemory ? $"ON ({robot.TurnMemoryTimer:F2}s)" : "OFF";
            Types.GType cell = inventory != null ? inventory.getCell() : Types.GType.None;

            GUILayout.Label($"Speed: {speed:F2}");
            GUILayout.Label($"Angle: {angle:F1}°");
            GUILayout.Label($"Memory angle: {memAngle:F1}°   [{memState}]");
            GUILayout.Label($"Inventory: {cell}");

            GUILayout.EndArea();
        }
    }
}

public static class RobotStatusHUDBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (Object.FindObjectOfType<UI.RobotStatusHUD>() != null)
            return;

        GameObject go = new GameObject("RobotStatusHUD");
        go.AddComponent<UI.RobotStatusHUD>();
    }
}

