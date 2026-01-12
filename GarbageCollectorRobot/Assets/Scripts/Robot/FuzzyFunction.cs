using UnityEngine;
using System.Collections.Generic; 

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
        if (d <= 0.7f)
        {
            k1=1f;
            part_funtion=1f;
        }
        else if ((d > 0.7f) && (d <0.9f))
        {
            k1=-5f*(d-0.7f) + 1f;
            k2=5f*(d-0.7f);
            part_funtion=1.5f;
        }
        else if ((d >= 0.9f) && (d <= 1.3f))
        {
            k1=1f;
            part_funtion=2f;
        }
        else if ((d > 1.3f) && (d < 1.7f))
        {
            k1=-2.5f*(d-1.3f) + 1f;
            k2=2.5f*(d-1.3f);
            part_funtion=2.5f;
        }
        else if ((d >= 1.7f) && (d <= 2.1f))
        {
            k1=1f;
            part_funtion=3f;
        }
        else if ((d > 2.1f) && (d < 2.5f))
        {
            k1=-2.5f*(d-2.1f) + 1f;
            k2=2.5f*(d-2.1f);
            part_funtion=3.5f;
        }
        else if (d >= 2.5f)
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
        if (d <= 0.3f)
        {
            k1=1f;
            part_funtion=1f;
        }
        else if ((d > 0.3f) && (d <0.5f))
        {
            k1=-5f*(d-0.3f) + 1f;
            k2=5f*(d-0.3f);
            part_funtion=1.5f;
        }
        else if ((d >= 0.5f) && (d <= 0.9f))
        {
            k1=1f;
            part_funtion=2f;
        }
        else if ((d > 0.9f) && (d < 1.3f))
        {
            k1=-2.5f*(d-0.9f) + 1f;
            k2=2.5f*(d-0.9f);
            part_funtion=2.5f;
        }
        else if ((d >= 1.3f) && (d <= 1.7f))
        {
            k1=1f;
            part_funtion=3f;
        }
        else if ((d > 1.7f) && (d < 2.1f))
        {
            k1=-2.5f*(d-1.7f) + 1f;
            k2=2.5f*(d-1.7f);
            part_funtion=3.5f;
        }
        else if (d >= 2.1f)
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
                q1=20f*k1+40f;
            }
            else if (k1 < k2)//есть ещё перемычка в середине между медленно и средне
            {
                q1=20f*k1+40f;//конец роста
                q2=40*k1+90;//начало роста
                q3=40*k2+90;//конец роста
            }
            else if (k1 > k2)
            {
                q1=20f*k1+40f;//конец роста
                q2=-40f*(k1-1f)+90f;//начало падения
                q3=-40f*(k2-1f)+90f;//конец падения
            }
        }
        else if (part_funtion == 2.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                q1=20*k1;//конец роста
                q2=-40f*(k2-1f)+90f;//начало падения
            }
            else if (k1 < k2)
            {
                //в начале
                q1=20*k1;//конец роста
                //в перемычке 
                q2=20f*k1+40f;//начинает возрастать
                q3=20f*k2+40f;//конец роста
                q4=-40f*(k2-1f)+90f;//начало падения
            }
            else if (k1 > k2)
            {
                //убывает до пересечения левого и правогл
                //в начале
                q1=20*k1;//конец роста
                //в перемычке 
                q2=-20f*(k1-1f)+40f;//начало падения
                q3=-20f*(k2-1f)+40f;//конец падения
                q4=-40f*(k2-1f)+90f;//начало падения
            }
        }
        else if (part_funtion == 3.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                q1=-20f*(k2-1f)+40f;//начало падения
            }
            else if (k1 < k2)
            {
                q1=20*k1;//начало роста
                q2=20*k2;//конец роста
                q3=-20f*(k2-1f)+40f;//начало падения
            }
            else if (k1 > k2)
            {
                q1=-20*(k1-1f);//начало падения
                q2=-20*(k2-1f);//конец падения
                q3=-20f*(k2-1f)+40f;//начало падения
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

    // Поворот по габаритным датчикам (возвращает угол поворота робота, в градусах).
    // В функцию передаётся расстояние с датчика, который БЛИЖЕ к препятствию,
    // и признак: левый датчик или правый (true = левый).
    public float Sentr_mass_rotate(float d, bool left, bool trash)
        {
        List<float> list = distans_angle(d);
        List<float> list1 = Suport_angle(list);
        float cof=1f;
        if (left)
            {
                cof=-1f;
            }
        float q1 = list1[0];
        float q2 = list1[1];
        float q3 = list1[2];
        float q4 = list1[3];
        float part_funtion = list1[4];
        float k1 = list1[5];
        float k2 = list1[6];
        if (part_funtion == 1f)// остановка
        {
            return cof*((Integrate(0.025f,-2.25f,90f,130f,true)+Integrate(0f,1f,130f,150f,true))/(Integrate(0.025f,-2.25f,90f,130f,false)+Integrate(0f,1f,130f,150f,false)));
        }
        //если левая меньше чем правая возрастает на пересечений функции
        //если правая больше чем левая убывает до пересесеыения функции
        else if (part_funtion == 1.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                return cof*((Integrate(0.05f,-2f,40f,q1,true)+Integrate(0f,0.5f,q1,150f,true))/(Integrate(0.05f,-2f,40f,q1,false)+Integrate(0f,0.5f,q1,150f,false)));
            }
            else if (k1 < k2)
            {
                return cof*((Integrate(0.05f,-2f,40f,q1,true)+Integrate(0f,k1,q1,q2,true)+Integrate(0.025f,-2.25f,q2,q3,true)+Integrate(0f,k2,q3,150f,true))/(Integrate(0.05f,-2f,40f,q1,false)+Integrate(0f,k1,q1,q2,false)+Integrate(0.025f,-2.25f,q2,q3,false)+Integrate(0f,k2,q3,150f,false)));
            }
            else if (k1 > k2)
            {
                return cof*((Integrate(0.05f,-2f,40f,q1,true)+Integrate(0f,k1,q1,q2,true)+Integrate(-0.025f,3.25f,q2,q3,true)+Integrate(0f,k2,q3,150f,true))/(Integrate(0.05f,-2f,40f,q1,false)+Integrate(0f,k1,q1,q2,false)+Integrate(-0.025f,3.25f,q2,q3,false)+Integrate(0f,k2,q3,150f,false)));
            }
        }
        else if (part_funtion == 2f)// средне зависит от средне
        {
            return cof*((Integrate(0.05f,-2f,40f,60f,true)+Integrate(0f,1f,60f,90f,true)+Integrate(-0.025f,3.25f,90f,130f,true))/(Integrate(0.05f,-2f,40f,60f,false)+Integrate(0f,1f,60f,90f,false)+Integrate(-0.025f,3.25f,90f,130f,false)));
        }
        else if (part_funtion == 2.5f)// быстро зависит от далеко 
        {

            if (k1 == k2)
            {
                //возрастание в начале дальше прямая линия
                return cof*((Integrate(0.05f,0f,0f,q1,true)+Integrate(0f,k1,q1,q2,true)+Integrate(-0.025f,3.25f,q2,130f,true))/(Integrate(0.05f,0f,0f,q1,false)+Integrate(0f,k1,q1,q2,false)+Integrate(-0.025f,3.25f,q2,130f,false)));
            }
            else if (k1 < k2)
            {
                return cof*((Integrate(0.05f,0f,0f,q1,true)+Integrate(0f,k1,q1,q2,true)+Integrate(0.05f,-2f,q2,q3,true)+Integrate(0f,k2,q3,q4,true)+Integrate(-0.025f,3.25f,q4,130f,true))/(Integrate(0.05f,0f,0f,q1,false)+Integrate(0f,k1,q1,q2,false)+Integrate(0.05f,-2f,q2,q3,false)+Integrate(0f,k2,q3,q4,false)+Integrate(-0.025f,3.25f,q4,130f,false)));
            }
            else if (k1 > k2)//НАДО СЧИТАТЬ ПЕРЕМЫЧКУ МЕЖДУ Медлено и средне
            {
                return cof*((Integrate(0.05f,0f,0f,q1,true)+Integrate(0f,k1,q1,q2,true)+Integrate(-0.05f,3f,q2,q3,true)+Integrate(0f,k2,q3,q4,true)+Integrate(-0.025f,3.25f,q4,130f,true))/(Integrate(0.05f,0f,0f,q1,false)+Integrate(0f,k1,q1,q2,false)+Integrate(-0.05f,3f,q2,q3,false)+Integrate(0f,k2,q3,q4,false)+Integrate(-0.025f,3.25f,q4,130f,false)));
            }
        }
        else if (part_funtion == 3f)//медлено зависи от очень близко
        {
            return cof*((Integrate(0.05f,0f,0f,20f,true)+Integrate(0f,1f,20f,40f,true)+Integrate(-0.05f,3f,40f,60f,true))/(Integrate(0.05f,0f,0f,20f,false)+Integrate(0f,1f,20f,40f,false)+Integrate(-0.05f,3f,40f,60f,false)));
        }
        else if (part_funtion == 3.5f)// быстро зависит от далеко 
        {
            if (k1 == k2)
            {
                //возрастание в начале дальше прямая линия
                return cof*((Integrate(0f,k1,0f,q1,true)+Integrate(-0.05f,3f,q1,60f,true))/(Integrate(0f,k1,0f,q1,false)+Integrate(-0.05f,3f,q1,60f,false)));
            }
            else if (k1 < k2)
            {
                return cof*((Integrate(0f,k1,0f,q1,true)+Integrate(0.05f,0f,q1,q2,true)+Integrate(0f,k2,q2,q3,true)+Integrate(-0.05f,3f,q3,60f,true))/(Integrate(0f,k1,0f,q1,false)+Integrate(0.05f,0f,q1,q2,false)+Integrate(0f,k2,q2,q3,false)+Integrate(-0.05f,3f,q3,60f,false)));
            }
            else if (k1 > k2)//НАДО СЧИТАТЬ ПЕРЕМЫЧКУ МЕЖДУ Медлено и средне
            {
                return cof*((Integrate(0f,k1,0f,q1,true)+Integrate(-0.05f,1f,q1,q2,true)+Integrate(0f,k2,q2,q3,true)+Integrate(-0.05f,3f,q3,60f,true))/(Integrate(0f,k1,0f,q1,false)+Integrate(-0.05f,1f,q1,q2,false)+Integrate(0f,k2,q2,q3,false)+Integrate(-0.05f,3f,q3,60f,false)));
            }
        }
        else if (part_funtion == 4f)// быстро зависит от далеко 
        {
            return cof*(Integrate(-0.05f,1f,0f,20f,true)/Integrate(-0.05f,1f,0f,20f,false));
        }
        return 0f;
        }
}
}