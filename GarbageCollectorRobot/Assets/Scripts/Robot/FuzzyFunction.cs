using UnityEngine;
using System.Collections.Generic;
using System.Transactions;
using System.Runtime.InteropServices;

namespace Fuzzy
{
public class FuzzyFunction
{
    public List<float> Suport_Speed(List<float> inputList)//функция для нахождения отрезков где функция возрастает и убывает
    {
        float k1 = inputList[0];//правая
        float k2 = inputList[1];//левая
        float part_funtion = inputList[2];

        float q1 = 0.0f;
        float q2 = 0.0f;
        float q3 = 0.0f;//начало убывания в конце

        //если левая меньше чем правая возрастает на пересечений функции
        //если правая больше чем левая убывает до пересесеыения функции
        if (part_funtion == 1.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                //прямая линия и убывание только в конце правого
                q3=(3f-k2)/2f;
            }
            else if (k1 < k2)//есть ещё перемычка в середине между медленно и средне
            {
                //в перемычке
                q1=k1/2f;//начинает возрастать
                q2=k2/2f;//закончивает возрастать
                //в коные
                q3=(6f-k2)/2f;//начинает убывать
            }
            else if (k1 > k2)
            {
                //убывает до пересечения левого и правогл
                //в перемычке
                q1=(1f-k1)/2f;//начинает убывать
                q2=(1f-k2)/2f;//закончивает убывать
                //в коные
                q3=(6f-k2)/2f;//начинает убывать
            }
        }
        else if (part_funtion == 2.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                //возрастание в начале дальше прямая линия
                q1=k1/2f;
            }
            else if (k1 < k2)
            {
                //в начале
                q1=k1/2f;//заканчивает возрастать
                //в перемычке 
                q2=(k1+5f)/2f;//начинает возрастать
                q3=(k2+5f)/2f;//заканчивает возрастать
            }
            else if (k1 > k2)
            {
                //убывает до пересечения левого и правогл
                //в начале
                q1=k1/2f;//заканчивает возрастать
                //в перемычке 
                q2=(6f-k1)/2f;//начинает убывать
                q3=(6f-k2)/2f;//заканчивает убывать
            }
        }
        List<float> resultList = new List<float>();
        resultList.Add(q1);
        resultList.Add(q2);
        resultList.Add(q3);
        resultList.Add(part_funtion);
        resultList.Add(k1);
        resultList.Add(k2);
        return resultList;

    }
    public List<float> distans(float d)//функция нахождения дистанций
    {
        float k1 = 0f;
        float k2 = 0f;
        float part_funtion = 0f;
        if (d <= 0.6f)
        {
            k1=1f;
            part_funtion=1f;
        }
        else if ((d > 0.6f) && (d <0.8f))
        {
            k1=-5f*(d-0.6f) + 1f;
            k2=5f*(d-0.6f);
            part_funtion=1.5f;
        }
        else if ((d >= 0.8f) && (d <= 1.2f))
        {
            k1=1f;
            part_funtion=2f;
        }
        else if ((d > 1.2f) && (d < 1.6f))
        {
            k1=-2.5f*(d-1.2f) + 1f;
            k2=2.5f*(d-1.2f);
            part_funtion=2.5f;
        }
        else if ((d >= 1.6f) && (d <= 2.0f))
        {
            k1=1f;
            part_funtion=3f;
        }
        else if ((d > 2.0f) && (d < 2.4f))
        {
            k1=-2.5f*(d-2.0f) + 1f;
            k2=2.5f*(d-2.0f);
            part_funtion=3.5f;
        }
        else if (d >= 2.4f)
        {
            k1=1f;
            part_funtion=4f;
        }
        List<float> list = new List<float>();
        list.Add(k1);
        list.Add(k2);
        list.Add(part_funtion);
        return list;
    }
    public List<float> distans_angle(float d)//функция нахождения дистанций
    {
        float k1 = 0f;
        float k2 = 0f;
        float part_funtion = 0f;
        if (d <= 0.6f)//osen silno
        {
            k1=1f;
            part_funtion=1f;
        }
        else if ((d > 0.6f) && (d <0.8f))
        {
            k1=-5f*(d-0.6f) + 1f;
            k2=5f*(d-0.6f);
            part_funtion=1.5f;
        }
        else if ((d >= 0.8f) && (d <= 1.2f))//cilno
        {
            k1=1f;
            part_funtion=2f;
        }
        else if ((d > 1.2f) && (d < 1.6f))
        {
            k1=-2.5f*(d-1.2f) + 1f;
            k2=2.5f*(d-1.2f);
            part_funtion=2.5f;
        }
        else if ((d >= 1.6f) && (d <= 2.0f))//credne
        {
            k1=1f;
            part_funtion=3f;
        }
        else if ((d > 2.0f) && (d < 2.4f))
        {
            k1=-2.5f*(d-2.0f) + 1f;
            k2=2.5f*(d-2.0f);
            part_funtion=3.5f;
        }
        else if ((d >= 2.4f) && (d <= 2.6f))//медлено
        {
            k1=1f;
            part_funtion=4f;
        }
        List<float> list = new List<float>();
        list.Add(k1);
        list.Add(k2);
        list.Add(part_funtion);
        return list;
    }

    /// <summary>
    /// Функции принадлежности для угла до цели.
    /// Возвращает степени принадлежности к категориям: маленький, средний, большой
    /// </summary>
    public void GetTargetAngleMembership(float angle, out float muSmall, out float muMedium, out float muLarge)
    {
        float absAngle = Mathf.Abs(angle);
        
        // Маленький угол: 0-30° полная принадлежность, 30-60° спад
        if (absAngle <= 30f)
            muSmall = 1f;
        else if (absAngle < 60f)
            muSmall = (60f - absAngle) / 30f;
        else
            muSmall = 0f;
        
        // Средний угол: 30-60° рост, 60-90° полная, 90-120° спад
        if (absAngle <= 30f)
            muMedium = 0f;
        else if (absAngle < 60f)
            muMedium = (absAngle - 30f) / 30f;
        else if (absAngle <= 90f)
            muMedium = 1f;
        else if (absAngle < 120f)
            muMedium = (120f - absAngle) / 30f;
        else
            muMedium = 0f;
        
        // Большой угол: 90-120° рост, >120° полная принадлежность
        if (absAngle <= 90f)
            muLarge = 0f;
        else if (absAngle < 120f)
            muLarge = (absAngle - 90f) / 30f;
        else
            muLarge = 1f;
    }

    /// <summary>
    /// Функции принадлежности для дистанции.
    /// Возвращает степени принадлежности к категориям: близко, средне, далеко
    /// </summary>
    public void GetDistanceMembership(float dist, out float muClose, out float muMedium, out float muFar)
    {
        // Близко: 0-0.8 полная, 0.8-1.2 спад
        if (dist <= 0.8f)
            muClose = 1f;
        else if (dist < 1.2f)
            muClose = (1.2f - dist) / 0.4f;
        else
            muClose = 0f;
        
        // Средне: 0.8-1.2 рост, 1.2-1.8 полная, 1.8-2.2 спад
        if (dist <= 0.8f)
            muMedium = 0f;
        else if (dist < 1.2f)
            muMedium = (dist - 0.8f) / 0.4f;
        else if (dist <= 1.8f)
            muMedium = 1f;
        else if (dist < 2.2f)
            muMedium = (2.2f - dist) / 0.4f;
        else
            muMedium = 0f;
        
        // Далеко: 1.8-2.2 рост, >2.2 полная
        if (dist <= 1.8f)
            muFar = 0f;
        else if (dist < 2.2f)
            muFar = (dist - 1.8f) / 0.4f;
        else
            muFar = 1f;
    }

    public List<float> Suport_angle(List<float> inputList)//функция для нахождения отрезков где функция возрастает и убывает
    {
        float k1 = inputList[0];//правая
        float k2 = inputList[1];//левая
        float part_funtion = inputList[2];

        float q1 = 0.0f;
        float q2 = 0.0f;
        float q3 = 0.0f;//начало убывания в конце
        float q4 = 0.0f;

        //если левая меньше чем правая возрастает на пересечений функции
        //если правая больше чем левая убывает до пересесеыения функции
        if (part_funtion == 1.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                //прямая линия и убывание только в конце правого
                q1=20f*k2+40f;
            }
            else if (k1 < k2)//есть ещё перемычка в середине между медленно и средне
            {
                q1=20f*k2+40f;//конец роста
                q2=40*k2+90;//начало роста
                q3=40*k1+90;//конец роста
            }
            else if (k1 > k2)
            {
                q1=20f*k2+40f;//конец роста
                q2=-40f*(k2-1f)+90f;//начало падения
                q3=-40f*(k1-1f)+90f;//конец падения
            }
        }
        else if (part_funtion == 2.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                q1=10*k2+10;//конец роста
                q2=-40f*(k1-1f)+90f;//начало падения
            }
            else if (k1 < k2)
            {
                //в начале
                q1=10*k2+10;//конец роста
                //в перемычке 
                q2=20f*k2+40f;//начинает возрастать
                q3=20f*k1+40f;//конец роста
                q4=-40f*(k1-1f)+90f;//начало падения
            }
            else if (k1 > k2)
            {
                //убывает до пересечения левого и правогл
                //в начале
                q1=10*k2+10;//конец роста
                //в перемычке 
                q2=-20f*(k2-1f)+40f;//начало падения
                q3=-20f*(k1-1f)+40f;//конец падения
                q4=-40f*(k1-1f)+90f;//начало падения
            }
        }
        else if (part_funtion == 3.5f)//изменить
        {

            if (k1 == k2)
            {
                q1=-20f*(k1-1f)+40f;//начало падения
            }
            else if (k1 < k2)
            {
                q1=10*k2+10;//начало роста
                q2=10*k1+10;//конец роста
                q3=-20f*(k1-1f)+40f;//начало падения
            }
            else if (k1 > k2)
            {
                q1=-10*(k2-1f)+10;//начало падения
                q2=-10*(k1-1f)+10;//конец падения
                q3=-20f*(k1-1f)+40f;//начало падения
            }
        }
        List<float> resultList = new List<float>();
        resultList.Add(q1);
        resultList.Add(q2);
        resultList.Add(q3);
        resultList.Add(q4);
        resultList.Add(part_funtion);
        resultList.Add(k1);
        resultList.Add(k2);
        return resultList;

    }
    public List<float> Suport_angle_left(List<float> inputList)//функция для нахождения отрезков где функция возрастает и убывает
    {
        float k1 = inputList[0];//правая
        float k2 = inputList[1];//левая
        float part_funtion = inputList[2];

        float q1 = 0.0f;
        float q2 = 0.0f;
        float q3 = 0.0f;//начало убывания в конце
        float q4 = 0.0f;

        //если левая меньше чем правая возрастает на пересечений функции
        //если правая больше чем левая убывает до пересесеыения функции
        if (part_funtion == 1.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                //прямая линия и убывание только в конце правого
                q1=20f*k2+40f;
            }
            else if (k1 < k2)//есть ещё перемычка в середине между медленно и средне
            {
                q1=20f*k2+40f;//конец роста
                q2=40*k2+90;//начало роста
                q3=40*k1+90;//конец роста
            }
            else if (k1 > k2)
            {
                q1=20f*k2+40f;//конец роста
                q2=-40f*(k2-1f)+90f;//начало падения
                q3=-40f*(k1-1f)+90f;//конец падения
            }
        }
        else if (part_funtion == 2.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                q1=20*k2;//конец роста
                q2=-40f*(k1-1f)+90f;//начало падения
            }
            else if (k1 < k2)
            {
                //в начале
                q1=20*k2;//конец роста
                //в перемычке 
                q2=20f*k2+40f;//начинает возрастать
                q3=20f*k1+40f;//конец роста
                q4=-40f*(k1-1f)+90f;//начало падения
            }
            else if (k1 > k2)
            {
                //убывает до пересечения левого и правогл
                //в начале
                q1=20*k2;//конец роста
                //в перемычке 
                q2=-20f*(k2-1f)+40f;//начало падения
                q3=-20f*(k1-1f)+40f;//конец падения
                q4=-40f*(k1-1f)+90f;//начало падения
            }
        }
        else if (part_funtion == 3.5f)//изменить
        {

            if (k1 == k2)
            {
                q1=-20f*(k1-1f)+40f;//начало падения
            }
            else if (k1 < k2)
            {
                q1=10*k2+10;//начало роста
                q2=10*k1+10;//конец роста
                q3=-20f*(k1-1f)+40f;//начало падения
            }
            else if (k1 > k2)
            {
                q1=-10*(k2-1f)+10;//начало падения
                q2=15;//конец падения начало роста
                q3=10*k1+10;//начало падения
                q4=-20f*(k2-1f)+40f;//начало падения
            }
        }
        List<float> resultList = new List<float>();
        resultList.Add(q1);
        resultList.Add(q2);
        resultList.Add(q3);
        resultList.Add(q4);
        resultList.Add(part_funtion);
        resultList.Add(k1);
        resultList.Add(k2);
        return resultList;

    }
    public float Integrate(float a, float b, float x1, float x2, bool isNumerator)
    {
        
        if (isNumerator)
        {
            // ∫(ax² + bx)dx от x1 до x2 = (a/3)x³ + (b/2)x² | от x1 до x2
            float term1 = (a / 3.0f) * (Mathf.Pow(x2, 3) - Mathf.Pow(x1, 3));
            float term2 = (b / 2.0f) * (Mathf.Pow(x2, 2) - Mathf.Pow(x1, 2));
            return term1 + term2;
        }
        else
        {
            // ∫(ax + b)dx от x1 до x2 = (a/2)x² + bx | от x1 до x2
            float term1 = (a / 2.0f) * (Mathf.Pow(x2, 2) - Mathf.Pow(x1, 2));
            float term2 = b * (x2 - x1);
            return term1 + term2;
        }
    }

    /// <summary>
    /// Интеграл для взвешенной функции принадлежности.
    /// Вычисляет ∫ weight * f(x) dx и ∫ weight * x * f(x) dx
    /// </summary>
    public void IntegrateWeighted(float a, float b, float x1, float x2, float weight, 
        ref float numerator, ref float denominator)
    {
        if (weight < 0.001f) return;
        
        // Числитель: ∫ weight * x * (ax + b) dx = weight * ∫(ax² + bx)dx
        float num = Integrate(a, b, x1, x2, true);
        // Знаменатель: ∫ weight * (ax + b) dx = weight * ∫(ax + b)dx
        float den = Integrate(a, b, x1, x2, false);
        
        numerator += weight * num;
        denominator += weight * den;
    }

    public float Sentr_mass(float d)//функция для нахождения центра масс (Возвращает скорость робота)
    {
        List<float> list = distans(d);
        List<float> list1 = Suport_Speed(list);
        float q1 = list1[0];
        float q2 = list1[1];
        float q3 = list1[2];
        float part_funtion = list1[3];
        float k1 = list1[4];
        float k2 = list1[5];
        if (part_funtion == 1f)// остановка
        {
            return 0f;
        }
        //если левая меньше чем правая возрастает на пересечений функции
        //если правая больше чем левая убывает до пересесеыения функции
        else if (part_funtion == 1.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                return (Integrate(0f,0.5f,0f,0.25f,true)+Integrate(-2f,1f,0.25f,0.5f,true))/(Integrate(0f,0.5f,0f,0.25f,false)+Integrate(-2f,1f,0.25f,0.5f,false));
            }
            else if (k1 < k2)
            {
                float q4=(3f-k2)/2f;
                float q5=(k2+2f)/2f;
                return (Integrate(0f,k1,0f,q1,true)+Integrate(2f,0f,q1,q2,true)+Integrate(0f,k2,q2,q4,true)+Integrate(-2f,3f,q4,1.25f,true)+Integrate(2f,-2f,1.25f,q5,true)+Integrate(0f,k2,q5,q3,true)+Integrate(-2f,6f,q3,3f,true))/(Integrate(0f,k1,0f,q1,false)+Integrate(2f,0f,q1,q2,false)+Integrate(0f,k2,q2,q4,false)+Integrate(-2f,3f,q4,1.25f,false)+Integrate(2f,-2f,1.25f,q5,false)+Integrate(0f,k2,q5,q3,false)+Integrate(-2f,6f,q3,3f,false));
            }
            else if (k1 > k2)
            {
                return (Integrate(0f,k1,0f,q1,true)+Integrate(-2f,1f,q1,q2,true)+Integrate(0f,k2,q2,q3,true)+Integrate(-2f,6f,q3,3f,true))/(Integrate(0f,k1,0f,q1,false)+Integrate(-2f,1f,q1,q2,false)+Integrate(0f,k2,q2,q3,false)+Integrate(-2f,6f,q3,3f,false));
            }
        }
        else if (part_funtion == 2f)// средне зависит от средне
        {
            return (Integrate(2f,0,0f,0.5f,true)+Integrate(0f,1f,0.5f,1f,true)+Integrate(-2f,3f,1f,1.25f,true)+Integrate(2f,-2f,1.25f,1.5f,true)+Integrate(0f,1f,1.5f,2.5f,true)+Integrate(-2f,6f,2.5f,3f,true))/(Integrate(2f,0,0f,0.5f,false)+Integrate(0f,1f,0.5f,1f,false)+Integrate(-2f,3f,1f,1.25f,false)+Integrate(2f,-2f,1.25f,1.5f,false)+Integrate(0f,1f,1.5f,2.5f,false)+Integrate(-2f,6f,2.5f,3f,false));
        }
        else if (part_funtion == 2.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                //возрастание в начале дальше прямая линия
                return (Integrate(2f,0f,0f,q1,true)+Integrate(0f,k1,q1,3f,true))/(Integrate(2f,0f,0f,q1,false)+Integrate(0f,k1,q1,3f,false));
            }
            else if (k1 < k2)
            {
                return (Integrate(2f,0f,0f,q1,true)+Integrate(0f,k1,q1,q2,true)+Integrate(2f,-5f,q2,q3,true)+Integrate(0f,k2,q3,3f,true))/(Integrate(2f,0f,0f,q1,false)+Integrate(0f,k1,q1,q2,false)+Integrate(2f,-5f,q2,q3,false)+Integrate(0f,k2,q3,3f,false));
            }
            else if (k1 > k2)//НАДО СЧИТАТЬ ПЕРЕМЫЧКУ МЕЖДУ Медлено и средне
            {
                float q4=(3f-k1)/2f;
                float q5=(k1+2f)/2f;
                return (Integrate(2f,0f,0f,q1,true)+Integrate(0f,k1,q1,q4,true)+Integrate(-2f,3f,q4,1.25f,true)+Integrate(2f,-2f,1.25f,q5,true)+Integrate(0f,k1,q5,q2,true)+Integrate(-2f,6f,q2,q3,true)+Integrate(0f,k2,q3,3f,true))/(Integrate(2f,0f,0f,q1,false)+Integrate(0f,k1,q1,q4,false)+Integrate(-2f,3f,q4,1.25f,false)+Integrate(2f,-2f,1.25f,q5,false)+Integrate(0f,k1,q5,q2,false)+Integrate(-2f,6f,q2,q3,false)+Integrate(0f,k2,q3,3f,false));
            }
        }
        else if (part_funtion == 3f)//медлено зависи от очень близко
        {
            return 3f-((3f-Mathf.Sqrt(5))/4f);
            //центр масс равен 3-((3-v5)/4)
        }
        else if (part_funtion == 3.5f)// быстро зависит от далеко 
        {
            //центр масс равен 3-((3-v5)/4)
            return 3f-((3f-Mathf.Sqrt(5))/4f);
        }
        else if (part_funtion == 4f)// быстро зависит от далеко 
        {
            //центр масс равен 3-((3-v5)/4)
            return 3f-((3f-Mathf.Sqrt(5))/4f);
        }
        return 3f-((3f-Mathf.Sqrt(5))/4f);
    }

    // ============================================================================
    // НЕЧЁТКАЯ ЛОГИКА ПОВОРОТА С 3 ВХОДАМИ
    // Входы: левый датчик (dl), правый датчик (dr), угол до цели (trah)
    // Выход: угол поворота в градусах [-150, 150]
    // ============================================================================
    
    /// <summary>
    /// Вычисляет выходной угол для одного правила нечёткой логики.
    /// Использует метод центра масс с интегралами.
    /// </summary>
    /// <param name="outputCenter">Центр выходной функции (угол)</param>
    /// <param name="outputWidth">Ширина выходной функции</param>
    /// <param name="weight">Степень активации правила (min от входов)</param>
    private void AddRuleOutput(float outputCenter, float outputWidth, float weight,
        ref float numerator, ref float denominator)
    {
        if (weight < 0.001f) return;
        
        // Треугольная функция принадлежности для выхода
        float x1 = outputCenter - outputWidth;
        float x2 = outputCenter;
        float x3 = outputCenter + outputWidth;
        
        // Левая часть треугольника (возрастание): y = (x - x1) / (x2 - x1) * weight
        // a = weight / (x2 - x1), b = -x1 * weight / (x2 - x1)
        if (Mathf.Abs(x2 - x1) > 0.01f)
        {
            float slope = weight / (x2 - x1);
            IntegrateWeighted(slope, -x1 * slope, x1, x2, 1f, ref numerator, ref denominator);
        }
        
        // Правая часть треугольника (убывание): y = (x3 - x) / (x3 - x2) * weight
        // a = -weight / (x3 - x2), b = x3 * weight / (x3 - x2)
        if (Mathf.Abs(x3 - x2) > 0.01f)
        {
            float slope = -weight / (x3 - x2);
            IntegrateWeighted(slope, x3 * (-slope), x2, x3, 1f, ref numerator, ref denominator);
        }
    }

    /// <summary>
    /// Поворот по габаритным датчикам с учётом угла до цели.
    /// Использует полную нечёткую логику с 3 входами и всеми комбинациями правил.
    /// </summary>
    /// <param name="dr">Дистанция правого датчика</param>
    /// <param name="dl">Дистанция левого датчика</param>
    /// <param name="trah">Угол до цели (0 если цели нет)</param>
    /// <returns>Угол поворота в градусах [-150, 150]</returns>
    public float Sentr_mass_rotate(float dr, float dl, float trah)
    {
        // Получаем степени принадлежности для дистанций
        float dlClose, dlMedium, dlFar;
        float drClose, drMedium, drFar;
        GetDistanceMembership(dl, out dlClose, out dlMedium, out dlFar);
        GetDistanceMembership(dr, out drClose, out drMedium, out drFar);
        
        // Получаем степени принадлежности для угла до цели
        float angleSmall, angleMedium, angleLarge;
        GetTargetAngleMembership(trah, out angleSmall, out angleMedium, out angleLarge);
        
        // Знак угла до цели (1 = вправо, -1 = влево, 0 = нет цели)
        float angleSign = 0f;
        if (trah > 1f) angleSign = 1f;
        else if (trah < -1f) angleSign = -1f;
        
        // Если нет цели, используем упрощённую логику только с датчиками
        bool hasTarget = Mathf.Abs(trah) > 1f;
        
        float numerator = 0f;
        float denominator = 0f;
        
        // Ширина выходных функций
        float widthSmall = 15f;   // для слабых поворотов
        float widthMedium = 25f;  // для средних поворотов
        float widthLarge = 35f;   // для сильных поворотов
        
        // ====================================================================
        // ПРАВИЛА НЕЧЁТКОЙ ЛОГИКИ: все комбинации (27 правил с целью + 9 без)
        // Формат: ЕСЛИ левый=X И правый=Y И угол=Z ТО поворот=W
        // ====================================================================
        
        float w; // вес правила (минимум от входов)
        
        // --- БЕЗ ЦЕЛИ (trah ≈ 0): 9 правил только по датчикам ---
        if (!hasTarget)
        {
            // 1. Левый близко, Правый близко -> сильно вправо (уходим от обоих)
            w = Mathf.Min(dlClose, drClose);
            AddRuleOutput(90f, widthLarge, w, ref numerator, ref denominator);
            
            // 2. Левый близко, Правый средне -> средне вправо
            w = Mathf.Min(dlClose, drMedium);
            AddRuleOutput(60f, widthMedium, w, ref numerator, ref denominator);
            
            // 3. Левый близко, Правый далеко -> сильно вправо
            w = Mathf.Min(dlClose, drFar);
            AddRuleOutput(90f, widthLarge, w, ref numerator, ref denominator);
            
            // 4. Левый средне, Правый близко -> средне влево
            w = Mathf.Min(dlMedium, drClose);
            AddRuleOutput(-60f, widthMedium, w, ref numerator, ref denominator);
            
            // 5. Левый средне, Правый средне -> прямо
            w = Mathf.Min(dlMedium, drMedium);
            AddRuleOutput(0f, widthSmall, w, ref numerator, ref denominator);
            
            // 6. Левый средне, Правый далеко -> слабо вправо
            w = Mathf.Min(dlMedium, drFar);
            AddRuleOutput(20f, widthSmall, w, ref numerator, ref denominator);
            
            // 7. Левый далеко, Правый близко -> сильно влево
            w = Mathf.Min(dlFar, drClose);
            AddRuleOutput(-90f, widthLarge, w, ref numerator, ref denominator);
            
            // 8. Левый далеко, Правый средне -> слабо влево
            w = Mathf.Min(dlFar, drMedium);
            AddRuleOutput(-20f, widthSmall, w, ref numerator, ref denominator);
            
            // 9. Левый далеко, Правый далеко -> прямо
            w = Mathf.Min(dlFar, drFar);
            AddRuleOutput(0f, widthSmall, w, ref numerator, ref denominator);
        }
        else
        {
            // --- С ЦЕЛЬЮ: 27 правил (3x3x3) ---
            // Направление к цели учитывается через angleSign
            
            // ========== ЛЕВЫЙ БЛИЗКО ==========
            
            // Левый близко, Правый близко, Угол маленький -> сильно вправо (обход важнее)
            w = Mathf.Min(dlClose, Mathf.Min(drClose, angleSmall));
            AddRuleOutput(100f, widthLarge, w, ref numerator, ref denominator);
            
            // Левый близко, Правый близко, Угол средний -> сильно в сторону цели (но обход)
            w = Mathf.Min(dlClose, Mathf.Min(drClose, angleMedium));
            AddRuleOutput(90f * angleSign, widthLarge, w, ref numerator, ref denominator);
            
            // Левый близко, Правый близко, Угол большой -> очень сильно в сторону цели
            w = Mathf.Min(dlClose, Mathf.Min(drClose, angleLarge));
            AddRuleOutput(110f * angleSign, widthLarge, w, ref numerator, ref denominator);
            
            // Левый близко, Правый средне, Угол маленький -> средне вправо
            w = Mathf.Min(dlClose, Mathf.Min(drMedium, angleSmall));
            AddRuleOutput(50f, widthMedium, w, ref numerator, ref denominator);
            
            // Левый близко, Правый средне, Угол средний -> вправо с учётом цели
            w = Mathf.Min(dlClose, Mathf.Min(drMedium, angleMedium));
            if (angleSign > 0) // цель справа - совпадает с обходом
                AddRuleOutput(70f, widthMedium, w, ref numerator, ref denominator);
            else // цель слева - конфликт, приоритет обходу
                AddRuleOutput(40f, widthMedium, w, ref numerator, ref denominator);
            
            // Левый близко, Правый средне, Угол большой -> сильно в сторону цели
            w = Mathf.Min(dlClose, Mathf.Min(drMedium, angleLarge));
            if (angleSign > 0)
                AddRuleOutput(80f, widthLarge, w, ref numerator, ref denominator);
            else
                AddRuleOutput(30f, widthMedium, w, ref numerator, ref denominator);
            
            // Левый близко, Правый далеко, Угол маленький -> сильно вправо
            w = Mathf.Min(dlClose, Mathf.Min(drFar, angleSmall));
            AddRuleOutput(80f, widthLarge, w, ref numerator, ref denominator);
            
            // Левый близко, Правый далеко, Угол средний -> вправо + к цели
            w = Mathf.Min(dlClose, Mathf.Min(drFar, angleMedium));
            if (angleSign > 0)
                AddRuleOutput(90f, widthLarge, w, ref numerator, ref denominator);
            else
                AddRuleOutput(50f, widthMedium, w, ref numerator, ref denominator);
            
            // Левый близко, Правый далеко, Угол большой -> к цели (путь справа свободен)
            w = Mathf.Min(dlClose, Mathf.Min(drFar, angleLarge));
            if (angleSign > 0)
                AddRuleOutput(100f, widthLarge, w, ref numerator, ref denominator);
            else
                AddRuleOutput(40f, widthMedium, w, ref numerator, ref denominator);
            
            // ========== ЛЕВЫЙ СРЕДНЕ ==========
            
            // Левый средне, Правый близко, Угол маленький -> средне влево
            w = Mathf.Min(dlMedium, Mathf.Min(drClose, angleSmall));
            AddRuleOutput(-50f, widthMedium, w, ref numerator, ref denominator);
            
            // Левый средне, Правый близко, Угол средний -> влево с учётом цели
            w = Mathf.Min(dlMedium, Mathf.Min(drClose, angleMedium));
            if (angleSign < 0) // цель слева - совпадает
                AddRuleOutput(-70f, widthMedium, w, ref numerator, ref denominator);
            else
                AddRuleOutput(-40f, widthMedium, w, ref numerator, ref denominator);
            
            // Левый средне, Правый близко, Угол большой -> сильно к цели
            w = Mathf.Min(dlMedium, Mathf.Min(drClose, angleLarge));
            if (angleSign < 0)
                AddRuleOutput(-80f, widthLarge, w, ref numerator, ref denominator);
            else
                AddRuleOutput(-30f, widthMedium, w, ref numerator, ref denominator);
            
            // Левый средне, Правый средне, Угол маленький -> слабо к цели
            w = Mathf.Min(dlMedium, Mathf.Min(drMedium, angleSmall));
            AddRuleOutput(15f * angleSign, widthSmall, w, ref numerator, ref denominator);
            
            // Левый средне, Правый средне, Угол средний -> средне к цели
            w = Mathf.Min(dlMedium, Mathf.Min(drMedium, angleMedium));
            AddRuleOutput(45f * angleSign, widthMedium, w, ref numerator, ref denominator);
            
            // Левый средне, Правый средне, Угол большой -> сильно к цели
            w = Mathf.Min(dlMedium, Mathf.Min(drMedium, angleLarge));
            AddRuleOutput(70f * angleSign, widthLarge, w, ref numerator, ref denominator);
            
            // Левый средне, Правый далеко, Угол маленький -> слабо вправо
            w = Mathf.Min(dlMedium, Mathf.Min(drFar, angleSmall));
            AddRuleOutput(15f, widthSmall, w, ref numerator, ref denominator);
            
            // Левый средне, Правый далеко, Угол средний -> к цели
            w = Mathf.Min(dlMedium, Mathf.Min(drFar, angleMedium));
            AddRuleOutput(40f * angleSign, widthMedium, w, ref numerator, ref denominator);
            
            // Левый средне, Правый далеко, Угол большой -> сильно к цели
            w = Mathf.Min(dlMedium, Mathf.Min(drFar, angleLarge));
            AddRuleOutput(65f * angleSign, widthMedium, w, ref numerator, ref denominator);
            
            // ========== ЛЕВЫЙ ДАЛЕКО ==========
            
            // Левый далеко, Правый близко, Угол маленький -> сильно влево
            w = Mathf.Min(dlFar, Mathf.Min(drClose, angleSmall));
            AddRuleOutput(-80f, widthLarge, w, ref numerator, ref denominator);
            
            // Левый далеко, Правый близко, Угол средний -> влево + к цели
            w = Mathf.Min(dlFar, Mathf.Min(drClose, angleMedium));
            if (angleSign < 0)
                AddRuleOutput(-90f, widthLarge, w, ref numerator, ref denominator);
            else
                AddRuleOutput(-50f, widthMedium, w, ref numerator, ref denominator);
            
            // Левый далеко, Правый близко, Угол большой -> к цели (путь слева свободен)
            w = Mathf.Min(dlFar, Mathf.Min(drClose, angleLarge));
            if (angleSign < 0)
                AddRuleOutput(-100f, widthLarge, w, ref numerator, ref denominator);
            else
                AddRuleOutput(-40f, widthMedium, w, ref numerator, ref denominator);
            
            // Левый далеко, Правый средне, Угол маленький -> слабо влево
            w = Mathf.Min(dlFar, Mathf.Min(drMedium, angleSmall));
            AddRuleOutput(-15f, widthSmall, w, ref numerator, ref denominator);
            
            // Левый далеко, Правый средне, Угол средний -> к цели
            w = Mathf.Min(dlFar, Mathf.Min(drMedium, angleMedium));
            AddRuleOutput(40f * angleSign, widthMedium, w, ref numerator, ref denominator);
            
            // Левый далеко, Правый средне, Угол большой -> сильно к цели
            w = Mathf.Min(dlFar, Mathf.Min(drMedium, angleLarge));
            AddRuleOutput(65f * angleSign, widthMedium, w, ref numerator, ref denominator);
            
            // Левый далеко, Правый далеко, Угол маленький -> слабо к цели
            w = Mathf.Min(dlFar, Mathf.Min(drFar, angleSmall));
            AddRuleOutput(20f * angleSign, widthSmall, w, ref numerator, ref denominator);
            
            // Левый далеко, Правый далеко, Угол средний -> средне к цели
            w = Mathf.Min(dlFar, Mathf.Min(drFar, angleMedium));
            AddRuleOutput(50f * angleSign, widthMedium, w, ref numerator, ref denominator);
            
            // Левый далеко, Правый далеко, Угол большой -> сильно к цели
            w = Mathf.Min(dlFar, Mathf.Min(drFar, angleLarge));
            AddRuleOutput(80f * angleSign, widthLarge, w, ref numerator, ref denominator);
        }
        
        // Дефаззификация: центр масс
        if (Mathf.Abs(denominator) < 0.0001f)
        {
            return 0f;
        }
        
        float result = numerator / denominator;
        
        // Защита от некорректных значений
        if (float.IsNaN(result) || float.IsInfinity(result))
        {
            return 0f;
        }
        
        return Mathf.Clamp(result, -150f, 150f);
    }
}
}
