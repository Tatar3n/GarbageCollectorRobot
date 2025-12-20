using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI2D : MonoBehaviour
{
    [Header("Текстовые элементы")]
    public TMP_Text statusText;
    public TMP_Text collectedText;
    public TMP_Text modeText;
    public TMP_Text fuzzyInfoText;
    
    [Header("Кнопки")]
    public Button startButton;
    public Button pauseButton;
    public Button resetButton;
    public Button clearButton;
    
    [Header("Кнопки режимов")]
    public Button obstacleModeButton;
    public Button garbageModeButton;
    public Button trashbinModeButton;
    
    [Header("Слайдеры")]
    public Slider garbageTypeSlider;
    public TMP_Text garbageTypeText;
    public Slider trashbinTypeSlider;
    public TMP_Text trashbinTypeText;
    
    [Header("Панель нечеткой логики")]
    public GameObject fuzzyPanel;
    public TMP_Text rulesText;
    public TMP_Text variablesText;
    
    [Header("Ссылки")]
    public RobotController2D robot;
    public ObjectPlacer2D placer;
    public FuzzySystem2D fuzzySystem;
    
    void Start()
    {
        // Настройка кнопок
        startButton.onClick.AddListener(StartSimulation);
        pauseButton.onClick.AddListener(PauseSimulation);
        resetButton.onClick.AddListener(ResetSimulation);
        clearButton.onClick.AddListener(ClearEnvironment);
        
        obstacleModeButton.onClick.AddListener(() => placer.SetModeObstacle());
        garbageModeButton.onClick.AddListener(() => placer.SetModeGarbage());
        trashbinTypeSlider.onValueChanged.AddListener(OnTrashbinTypeChanged);
        
        // Настройка слайдеров
        garbageTypeSlider.onValueChanged.AddListener(OnGarbageTypeChanged);
        trashbinTypeSlider.onValueChanged.AddListener(OnTrashbinTypeChanged);
        
        garbageTypeSlider.minValue = 1;
        garbageTypeSlider.maxValue = 3;
        trashbinTypeSlider.minValue = 1;
        trashbinTypeSlider.maxValue = 3;
        
        // Начальные значения
        UpdateTypeDisplays();
    }
    
    void Update()
    {
        if (robot != null)
        {
            // Обновление статуса
            string carryingText = robot.carryingGarbageType > 0 ? 
                $"Несу: Тип {robot.carryingGarbageType}" : "Пустой";
            statusText.text = $"Статус: {carryingText}";
            
            collectedText.text = $"Собрано: {robot.collectedCount}/{robot.totalGarbage}";
            
            // Обновление режима размещения
            UpdateModeDisplay();
            
            // Обновление информации о нечеткой логике
            UpdateFuzzyInfo();
        }
    }
    
    void UpdateModeDisplay()
    {
        switch (placer.currentMode)
        {
            case ObjectPlacer2D.PlacementMode.Obstacle:
                modeText.text = "Режим: 🚧 Препятствия";
                break;
            case ObjectPlacer2D.PlacementMode.Garbage:
                modeText.text = $"Режим: 🗑️ Мусор (Тип {placer.currentGarbageType})";
                break;
            case ObjectPlacer2D.PlacementMode.Trashbin:
                modeText.text = $"Режим: 🏠 Мусорки (Тип {placer.currentTrashbinType})";
                break;
        }
    }
    
    void UpdateTypeDisplays()
    {
        garbageTypeText.text = $"Тип {placer.currentGarbageType}";
        trashbinTypeText.text = $"Тип {placer.currentTrashbinType}";
    }
    
    void UpdateFuzzyInfo()
    {
        if (fuzzySystem != null)
        {
            string info = $"Время бездействия: {fuzzySystem.timeSinceLastAction:F1}с\n";
            info += $"Уровень мусора: {fuzzySystem.trashLevel:P0}\n";
            info += $"Правил: {fuzzySystem.rules.Count}";
            
            fuzzyInfoText.text = info;
        }
    }
    
    void StartSimulation()
    {
        Time.timeScale = 1f;
        robot.enabled = true;
        startButton.interactable = false;
        pauseButton.interactable = true;
    }
    
    void PauseSimulation()
    {
        Time.timeScale = 0f;
        startButton.interactable = true;
        pauseButton.interactable = false;
    }
    
    void ResetSimulation()
    {
        Time.timeScale = 0f;
        robot.ResetRobot();
        startButton.interactable = true;
        pauseButton.interactable = false;
    }
    
    void ClearEnvironment()
    {
        GarbageManager2D.Instance.ClearAll();
        robot.ResetRobot();
    }
    
    void OnGarbageTypeChanged(float value)
    {
        int type = Mathf.RoundToInt(value);
        placer.SetGarbageType(type);
        garbageTypeText.text = $"Тип {type}";
    }
    
    void OnTrashbinTypeChanged(float value)
    {
        int type = Mathf.RoundToInt(value);
        placer.SetTrashbinType(type);
        trashbinTypeText.text = $"Тип {type}";
    }
    
    public void ToggleFuzzyPanel()
    {
        if (fuzzyPanel != null)
            fuzzyPanel.SetActive(!fuzzyPanel.activeSelf);
    }
}