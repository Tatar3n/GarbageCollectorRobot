using System;
using System.Collections.Generic;
using UnityEngine;

namespace FuzzyLogic2D
{
    [System.Serializable]
    public class MembershipFunction2D
    {
        public string name;
        public AnimationCurve curve;
        public float min;
        public float max;

        public float Evaluate(float x)
        {
            if (x < min || x > max) return 0f;
            float normalized = (x - min) / (max - min);
            return curve.Evaluate(normalized);
        }
    }

    [System.Serializable]
    public class FuzzyVariable2D
    {
        public string name;
        public List<MembershipFunction2D> functions;
        
        public Dictionary<string, float> Fuzzify(float value)
        {
            Dictionary<string, float> results = new Dictionary<string, float>();
            foreach (var func in functions)
            {
                float degree = func.Evaluate(value);
                if (degree > 0.001f)
                    results[func.name] = degree;
            }
            return results;
        }
    }

    [System.Serializable]
    public class FuzzyRule2D
    {
        [System.Serializable]
        public class Condition
        {
            public string variable;
            public string function;
            public bool not = false;
        }

        [System.Serializable]
        public class Consequent
        {
            public string variable;
            public string function;
        }

        public List<Condition> conditions;
        public Consequent consequent;
        
        public float? Evaluate(Dictionary<string, Dictionary<string, float>> fuzzyValues)
        {
            float strength = 1f;
            
            foreach (var condition in conditions)
            {
                if (!fuzzyValues.ContainsKey(condition.variable))
                    return null;
                    
                var variableValues = fuzzyValues[condition.variable];
                
                if (!variableValues.ContainsKey(condition.function))
                    return null;
                    
                float degree = variableValues[condition.function];
                
                if (condition.not)
                    degree = 1f - degree;
                    
                strength = Mathf.Min(strength, degree);
                
                if (strength <= 0.001f)
                    return null;
            }
            
            return strength;
        }
    }

    public class Defuzzifier2D
    {
        public static float Centroid(List<Vector2> points)
        {
            if (points.Count < 2) return 0f;
            
            float numerator = 0f;
            float denominator = 0f;
            
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                
                float area = (a.y + b.y) * (b.x - a.x) / 2f;
                float centroidX = (a.x + b.x) / 2f;
                
                numerator += area * centroidX;
                denominator += area;
            }
            
            return denominator > 0.001f ? numerator / denominator : 0f;
        }
    }

    public class FuzzySystem2D : MonoBehaviour
    {
        [Header("Входные переменные")]
        public FuzzyVariable2D distance;     // Дистанция до препятствий
        public FuzzyVariable2D angle;        // Угол до цели
        public FuzzyVariable2D trashbinDist; // Дистанция до мусорки
        public FuzzyVariable2D garbageDist;  // Дистанция до мусора
        public FuzzyVariable2D idleTime;     // Время бездействия
        
        [Header("Выходные переменные")]
        public FuzzyVariable2D speed;        // Скорость движения
        public FuzzyVariable2D turn;         // Поворот
        public FuzzyVariable2D action;       // Действия
        
        [Header("Правила")]
        public List<FuzzyRule2D> rules;
        
        [Header("Состояние робота")]
        public float carryingType = 0f;      // 0: пустой, 1-3: тип мусора
        public float timeSinceLastAction = 0f;
        public float trashLevel = 0f;
        
        // Кэш для оптимизации
        private Dictionary<string, Dictionary<string, float>> fuzzyInputsCache;
        
        public Dictionary<string, float> Process(
            float[] sensorDistances,
            float targetAngle,
            float targetDistance,
            bool isGarbage,
            float deltaTime)
        {
            // Обновление состояния
            timeSinceLastAction += deltaTime;
            
            // Фаззификация входных данных
            fuzzyInputsCache = new Dictionary<string, Dictionary<string, float>>();
            
            // Средняя дистанция от сенсоров
            float avgDistance = 0f;
            foreach (float d in sensorDistances) avgDistance += d;
            avgDistance /= sensorDistances.Length;
            
            fuzzyInputsCache["distance"] = distance.Fuzzify(avgDistance);
            fuzzyInputsCache["angle"] = angle.Fuzzify(targetAngle);
            
            if (isGarbage)
                fuzzyInputsCache["garbageDist"] = garbageDist.Fuzzify(targetDistance);
            else
                fuzzyInputsCache["trashbinDist"] = trashbinDist.Fuzzify(targetDistance);
                
            fuzzyInputsCache["idleTime"] = idleTime.Fuzzify(timeSinceLastAction);
            
            // Состояние "несет/пустой"
            Dictionary<string, float> carryingState = new Dictionary<string, float>();
            if (carryingType > 0.1f)
            {
                carryingState["carrying"] = 1f;
                carryingState["empty"] = 0f;
            }
            else
            {
                carryingState["carrying"] = 0f;
                carryingState["empty"] = 1f;
            }
            fuzzyInputsCache["carrying"] = carryingState;
            
            // Применение правил
            Dictionary<string, Dictionary<string, float>> fuzzyOutputs = new Dictionary<string, Dictionary<string, float>>
            {
                ["speed"] = new Dictionary<string, float>(),
                ["turn"] = new Dictionary<string, float>(),
                ["action"] = new Dictionary<string, float>()
            };
            
            foreach (var rule in rules)
            {
                float? strength = rule.Evaluate(fuzzyInputsCache);
                if (strength.HasValue && strength.Value > 0.001f)
                {
                    var outputVar = fuzzyOutputs[rule.consequent.variable];
                    string funcName = rule.consequent.function;
                    
                    if (!outputVar.ContainsKey(funcName) || outputVar[funcName] < strength.Value)
                        outputVar[funcName] = strength.Value;
                }
            }
            
            // Дефаззификация
            Dictionary<string, float> crispOutputs = new Dictionary<string, float>
            {
                ["speed"] = Defuzzify(speed, fuzzyOutputs["speed"]),
                ["turn"] = Defuzzify(turn, fuzzyOutputs["turn"]),
                ["action"] = Defuzzify(action, fuzzyOutputs["action"])
            };
            
            return crispOutputs;
        }
        
        private float Defuzzify(FuzzyVariable2D variable, Dictionary<string, float> fuzzyValues)
        {
            if (fuzzyValues.Count == 0) return 0f;
            
            List<Vector2> points = new List<Vector2>();
            int samples = 50;
            
            for (int i = 0; i <= samples; i++)
            {
                float x = Mathf.Lerp(variable.functions[0].min, 
                                    variable.functions[variable.functions.Count - 1].max, 
                                    i / (float)samples);
                float y = 0f;
                
                foreach (var func in variable.functions)
                {
                    if (fuzzyValues.ContainsKey(func.name))
                    {
                        float membership = func.Evaluate(x);
                        float clipped = Mathf.Min(membership, fuzzyValues[func.name]);
                        y = Mathf.Max(y, clipped);
                    }
                }
                
                points.Add(new Vector2(x, y));
            }
            
            return Defuzzifier2D.Centroid(points);
        }
        
        // Визуализация функций принадлежности (для отладки)
        public void DrawMembershipFunctions()
        {
            // Можно реализовать отрисовку в OnDrawGizmos или отдельном UI
        }
    }
}