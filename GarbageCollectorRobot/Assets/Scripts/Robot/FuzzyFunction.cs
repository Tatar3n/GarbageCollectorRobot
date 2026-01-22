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
        if (x1>x2)
        {
            float x=x1;
            x1=x2;
            x2=x;
        }
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
    public float Sentr_mass_rotate(float dr, float dl, float trah)
        {
            float sign=0f;
            if (trah>0f)
                sign=1;
            else if (trah<0f)
                sign=-1;
        List<float> list = distans_angle(dr);
        List<float> list1 = Suport_angle(list);
        float q1r = list1[0];
        float q2r = list1[1];
        float q3r = list1[2];
        float q4r = list1[3];
        float part_funtion_r = list1[4];
        float k1r = list1[5];
        float k2r = list1[6];
        List<float> list2 = distans_angle(dl);
        List<float> list3 = Suport_angle(list2);
        float q1l = list3[0];
        float q2l = list3[1];
        float q3l = list3[2];
        float q4l = list3[3];
        float part_funtion_l = list3[4];
        float k1l = list3[5];
        float k2l = list3[6];

        float left=0f;
        float right=0f;


/////////////////////////////////////
/// 
        if ((sign==1f) || (sign==0f))
        {
        //нужно сделать ифы такиеже как и тут только и для право и для лево   для право до плюса после для лево
        if (part_funtion_r == 1f)// остановка
        {
            right=(Integrate(0.025f,-2.25f,90f,130f,true)+Integrate(0f,1f,130f,150f,true))/(Integrate(0.025f,-2.25f,90f,130f,false)+Integrate(0f,1f,130f,150f,false));
        }
        //если левая меньше чем правая возрастает на пересечений функции
        //если правая больше чем левая убывает до пересесеыения функции
        else if (part_funtion_r == 1.5f)// быстро зависит от далеко 
        {
            right=(Integrate(0.025f,-2.25f,90f,130f,true)+Integrate(0f,1f,130f,150f,true))/(Integrate(0.025f,-2.25f,90f,130f,false)+Integrate(0f,1f,130f,150f,false));

            
        }
        else if (part_funtion_r == 2f)// средне зависит от средне
        {
            right=(Integrate(0.025f,-2.25f,90f,130f,true)+Integrate(0f,1f,130f,150f,true))/(Integrate(0.025f,-2.25f,90f,130f,false)+Integrate(0f,1f,130f,150f,false));

            
        }
        else if (part_funtion_r == 2.5f)// быстро зависит от далеко 
        {

            

            if (k1r == k2r)
            {
                right=(Integrate(0.05f,-2f,40f,q1r,true)+Integrate(0f,0.5f,q1r,150f,true))/(Integrate(0.05f,-2f,40f,q1r,false)+Integrate(0f,0.5f,q1r,150f,false));
            }
            else if (k1r < k2r)
            {
                right=(Integrate(0.05f,-2f,40f,q1r,true)+Integrate(0f,k2r,q1r,q2r,true)+Integrate(0.025f,-2.25f,q2r,q3r,true)+Integrate(0f,k1r,q3r,150f,true))/(Integrate(0.05f,-2f,40f,q1r,false)+Integrate(0f,k1r,q1r,q2r,false)+Integrate(0.025f,-2.25f,q2r,q3r,false)+Integrate(0f,k1r,q3r,150f,false));
            }
            else if (k1r > k2r)
            {
                right=(Integrate(0.05f,-2f,40f,q1r,true)+Integrate(0f,k1r,q1r,q2r,true)+Integrate(-0.025f,3.25f,q2r,q3r,true)+Integrate(0f,k1r,q3r,150f,true))/(Integrate(0.05f,-2f,40f,q1r,false)+Integrate(0f,k1r,q1r,q2r,false)+Integrate(-0.025f,3.25f,q2r,q3r,false)+Integrate(0f,k1r,q3r,150f,false));
            }
                
        }
        else if (part_funtion_r == 3f)//медлено зависи от очень близко
        {
            right=(Integrate(0.05f,-2f,40f,60f,true)+Integrate(0f,1f,60f,90f,true)+Integrate(-0.025f,3.25f,90f,130f,true))/(Integrate(0.05f,-2f,40f,60f,false)+Integrate(0f,1f,60f,90f,false)+Integrate(-0.025f,3.25f,90f,130f,false));
            
        }
        else if (part_funtion_r == 3.5f)// быстро зависит от далеко 
        {
            if (k1r == k2r)
            {
                //возрастание в начале дальше прямая линия
                right=(Integrate(0.1f,-1f,10f,q1r,true)+Integrate(0f,k2r,q1r,q2r,true)+Integrate(-0.025f,3.25f,q2r,130f,true))/(Integrate(0.1f,-1f,10f,q1r,false)+Integrate(0f,k2r,q1r,q2r,false)+Integrate(-0.025f,3.25f,q2r,130f,false));
            }
            else if (k1r < k2r)
            {
                right=(Integrate(0.1f,-1f,10f,q1r,true)+Integrate(0f,k2r,q1r,q2r,true)+Integrate(0.05f,-2f,q2r,q3r,true)+Integrate(0f,k1r,q3r,q4r,true)+Integrate(-0.025f,3.25f,q4r,130f,true))/(Integrate(0.1f,-1f,10f,q1r,false)+Integrate(0f,k2r,q1r,q2r,false)+Integrate(0.05f,-2f,q2r,q3r,false)+Integrate(0f,k1r,q3r,q4r,false)+Integrate(-0.025f,3.25f,q4r,130f,false));
            }
            else if (k1r > k2r)//НАДО СЧИТАТЬ ПЕРЕМЫЧКУ МЕЖДУ Медлено и средне
            {
                right=(Integrate(0.1f,-1f,10f,q1r,true)+Integrate(0f,k2r,q1r,q2r,true)+Integrate(-0.05f,3f,q2r,q3r,true)+Integrate(0f,k1r,q3r,q4r,true)+Integrate(-0.025f,3.25f,q4r,130f,true))/(Integrate(0.1f,-1f,10f,q1r,false)+Integrate(0f,k2r,q1r,q2r,false)+Integrate(-0.05f,3f,q2r,q3r,false)+Integrate(0f,k1r,q3r,q4r,false)+Integrate(-0.025f,3.25f,q4r,130f,false));
        }
        }
        else if (part_funtion_r == 4f)// быстро зависит от далеко 
        {
            right=(Integrate(0.1f,-1f,10f,20f,true)+Integrate(0f,1f,20f,40f,true)+Integrate(-0.05f,3f,40f,60f,true))/(Integrate(0.1f,-1f,10f,20f,false)+Integrate(0f,1f,20f,40f,false)+Integrate(-0.05f,3f,40f,60f,false));
        }
        


        if (part_funtion_l == 1f)// остановка
        {
            left= (Integrate(-0.025f,-2.25f,-90f,-130f,true)+Integrate(0f,1f,-130f,-150f,true))/(Integrate(-0.025f,-2.25f,-90f,-130f,false)+Integrate(0f,1f,-130f,-150f,false));
        }
        //если левая меньше чем правая возрастает на пересечений функции
        //если правая больше чем левая убывает до пересесеыения функции
        else if (part_funtion_l == 1.5f)// быстро зависит от далеко 
        {

            if (k1l == k2l)
            {
                left= (Integrate(-0.05f,-2f,-40f,-q1l,true)+Integrate(0f,0.5f,-q1l,-150f,true))/(Integrate(-0.05f,-2f,-40f,-q1l,false)+Integrate(0f,0.5f,-q1l,-150f,false));
            }
            else if (k1l < k2l)
            {
                left= (Integrate(-0.05f,-2f,-40f,-q1l,true)+Integrate(0f,k2l,-q1l,-q2l,true)+Integrate(-0.025f,-2.25f,-q2l,-q3l,true)+Integrate(0f,k1l,-q3l,-150f,true))/(Integrate(-0.05f,-2f,-40f,-q1l,false)+Integrate(0f,k1l,-q1l,-q2l,false)+Integrate(-0.025f,-2.25f,-q2l,-q3l,false)+Integrate(0f,k1l,-q3l,-150f,false));
            }
            else if (k1l > k2l)
            {
                left= (Integrate(-0.05f,-2f,-40f,-q1l,true)+Integrate(0f,k1l,-q1l,-q2l,true)+Integrate(0.025f,3.25f,-q2l,-q3l,true)+Integrate(0f,k1l,-q3l,-150f,true))/(Integrate(-0.05f,-2f,-40f,-q1l,false)+Integrate(0f,k1l,-q1l,-q2l,false)+Integrate(0.025f,3.25f,-q2l,-q3l,false)+Integrate(0f,k1l,-q3l,-150f,false));
            }
        }
        else if (part_funtion_l == 2f)// средне зависит от средне
        {
            left= (Integrate(-0.05f,-2f,-40f,-60f,true)+Integrate(0f,1f,-60f,-90f,true)+Integrate(0.025f,3.25f,-90f,-130f,true))/(Integrate(-0.05f,-2f,-40f,-60f,false)+Integrate(0f,1f,-60f,-90f,false)+Integrate(0.025f,3.25f,-90f,-130f,false));
        }
        else if (part_funtion_l == 2.5f)// быстро зависит от далеко 
        {

            if (k1l == k2l)
            {
                //возрастание в начале дальше прямая линия
                left= (Integrate(-0.1f,-1f,-10f,-q1l,true)+Integrate(0f,k2l,-q1l,-q2l,true)+Integrate(0.025f,3.25f,-q2l,-130f,true))/(Integrate(-0.1f,-1f,-10f,-q1l,false)+Integrate(0f,k2l,-q1l,-q2l,false)+Integrate(0.025f,3.25f,-q2l,-130f,false));
            }
            else if (k1l < k2l)
            {
                left= (Integrate(-0.1f,-1f,-10f,-q1l,true)+Integrate(0f,k2l,-q1l,-q2l,true)+Integrate(-0.05f,-2f,-q2l,-q3l,true)+Integrate(0f,k1l,-q3l,-q4l,true)+Integrate(0.025f,3.25f,-q4l,-130f,true))/(Integrate(-0.1f,-1f,-10f,-q1l,false)+Integrate(0f,k2l,-q1l,-q2l,false)+Integrate(-0.05f,-2f,-q2l,-q3l,false)+Integrate(0f,k1l,-q3l,-q4l,false)+Integrate(0.025f,3.25f,-q4l,-130f,false));
            }
            else if (k1l > k2l)//НАДО СЧИТАТЬ ПЕРЕМЫЧКУ МЕЖДУ Медлено и средне
            {
                left= (Integrate(-0.1f,-1f,-10f,-q1l,true)+Integrate(0f,k2l,-q1l,-q2l,true)+Integrate(0.05f,3f,-q2l,-q3l,true)+Integrate(0f,k1l,-q3l,-q4l,true)+Integrate(0.025f,3.25f,-q4l,-130f,true))/(Integrate(-0.1f,-1f,-10f,-q1l,false)+Integrate(0f,k2l,-q1l,-q2l,false)+Integrate(0.05f,3f,-q2l,-q3l,false)+Integrate(0f,k1l,-q3l,-q4l,false)+Integrate(0.025f,3.25f,-q4l,-130f,false));
            }
        }
        else if (part_funtion_l == 3f)//медлено зависи от очень близко
        {
            left= (Integrate(-0.1f,-1f,-10f,-20f,true)+Integrate(0f,1f,-20f,-40f,true)+Integrate(0.05f,3f,-40f,-60f,true))/(Integrate(-0.1f,-1f,-10f,-20f,false)+Integrate(0f,1f,-20f,-40f,false)+Integrate(0.05f,3f,-40f,-60f,false));
        }
        else if (part_funtion_l == 3.5f)// быстро зависит от далеко 
        {
            if (k1l == k2l)
            {
                //возрастание в начале дальше прямая линия
                left= (Integrate(0f,k2l,0f,-50f,true)+Integrate(0.05f,3f,-50f,-60f,true))/(Integrate(0f,k2l,0f,-50f,false)+Integrate(0.05f,3f,-50f,-60f,false));
            }
            else if (k1l < k2l)
            {
                left= (Integrate(0f,k2l,0f,-q1l,true)+Integrate(-0.1f,-1f,-q1l,-15f,true)+Integrate(0.1f,2f,-15f,-q2l,true)+Integrate(0f,k2l,-q2l,-q3l,true)+Integrate(-0.05f,-2f,-q3l,-60f,true))/(Integrate(0f,k2l,0f,-q1l,false)+Integrate(-0.1f,-1f,-q1l,-15f,false)+Integrate(0.1f,2f,-15f,-q2l,false)+Integrate(0f,k2l,-q2l,-q3l,false)+Integrate(-0.05f,-2f,-q3l,-60f,false));
            }
            else if (k1l > k2l)//НАДО СЧИТАТЬ ПЕРЕМЫЧКУ МЕЖДУ Медлено и средне
            {
                left= (Integrate(0f,k2l,0f,-q1l,true)+Integrate(0.1f,2f,-q1l,-q2l,true)+Integrate(0f,k1l,-q2l,-q3l,true)+Integrate(0.05f,3f,-q3l,-60f,true))/(Integrate(0f,k2l,-10f,-q1l,false)+Integrate(0.1f,2f,-q1l,-q2l,false)+Integrate(0f,k1l,-q2l,-q3l,false)+Integrate(0.05f,3f,-q3l,-60f,false));
            }
        }
        else if (part_funtion_l == 4f)// быстро зависит от далеко 
        {
            left= (Integrate(0f,1f,0f,-10f,true)+Integrate(0.1f,2f,-10f,-20f,true))/(Integrate(0f,1f,0f,-10f,true)+Integrate(0.1f,2f,-10f,-20f,false));
        }
        }

///////////////////////////////////////////////////////
        else
        {
        //нужно сделать ифы такиеже как и тут только и для право и для лево   для право до плюса после для лево
        if (part_funtion_l == 1f)// остановка
        {
            left=(Integrate(-0.025f,-2.25f,-90f,-130f,true)+Integrate(0f,1f,-130f,-150f,true))/(Integrate(-0.025f,-2.25f,-90f,-130f,false)+Integrate(0f,1f,-130f,-150f,false));
        }
        //если левая меньше чем правая возрастает на пересечений функции
        //если правая больше чем левая убывает до пересесеыения функции
        else if (part_funtion_l == 1.5f)// быстро зависит от далеко 
        {
            left=(Integrate(-0.025f,-2.25f,-90f,-130f,true)+Integrate(0f,1f,-130f,-150f,true))/(Integrate(-0.025f,-2.25f,-90f,-130f,false)+Integrate(0f,1f,-130f,-150f,false));

            
        }
        else if (part_funtion_l == 2f)// средне зависит от средне
        {
            left=(Integrate(-0.025f,-2.25f,-90f,-130f,true)+Integrate(0f,1f,-130f,-150f,true))/(Integrate(-0.025f,-2.25f,-90f,-130f,false)+Integrate(0f,1f,-130f,-150f,false));

            
        }
        else if (part_funtion_l == 2.5f)// быстро зависит от далеко 
        {

            

            if (k1l == k2l)
            {
                left=(Integrate(-0.05f,-2f,-40f,-q1l,true)+Integrate(0f,0.5f,-q1l,-150f,true))/(Integrate(-0.05f,-2f,-40f,-q1l,false)+Integrate(0f,0.5f,-q1l,-150f,false));
            }
            else if (k1l < k2l)
            {
                left=(Integrate(-0.05f,-2f,-40f,-q1l,true)+Integrate(0f,k2l,-q1l,-q2l,true)+Integrate(-0.025f,-2.25f,-q2l,-q3l,true)+Integrate(0f,k1l,-q3l,-150f,true))/(Integrate(-0.05f,-2f,-40f,-q1l,false)+Integrate(0f,k1l,-q1l,-q2l,false)+Integrate(-0.025f,-2.25f,-q2l,-q3l,false)+Integrate(0f,k1l,-q3l,-150f,false));
            }
            else if (k1l > k2l)
            {
                left=(Integrate(-0.05f,-2f,-40f,-q1l,true)+Integrate(0f,k1l,-q1l,-q2l,true)+Integrate(0.025f,3.25f,-q2l,-q3l,true)+Integrate(0f,k1l,-q3l,-150f,true))/(Integrate(-0.05f,-2f,-40f,-q1l,false)+Integrate(0f,k1l,-q1l,-q2l,false)+Integrate(0.025f,3.25f,-q2l,-q3l,false)+Integrate(0f,k1l,-q3l,-150f,false));
            }
                
        }
        else if (part_funtion_l == 3f)//медлено зависи от очень близко
        {
            left=(Integrate(-0.05f,-2f,-40f,-60f,true)+Integrate(0f,1f,-60f,-90f,true)+Integrate(0.025f,3.25f,-90f,-130f,true))/(Integrate(-0.05f,-2f,-40f,-60f,false)+Integrate(0f,1f,-60f,-90f,false)+Integrate(0.025f,3.25f,-90f,-130f,false));
            
        }
        else if (part_funtion_l == 3.5f)// быстро зависит от далеко 
        {
            if (k1l == k2l)
            {
                //возрастание в начале дальше прямая линия
                left=(Integrate(-0.1f,-1f,-10f,-q1l,true)+Integrate(0f,k2l,-q1l,-q2l,true)+Integrate(0.025f,3.25f,-q2l,-130f,true))/(Integrate(-0.1f,-1f,-10f,-q1l,false)+Integrate(0f,k2l,-q1l,-q2l,false)+Integrate(0.025f,3.25f,-q2l,-130f,false));
            }
            else if (k1l < k2l)
            {
                left=(Integrate(-0.1f,-1f,-10f,-q1l,true)+Integrate(0f,k2l,-q1l,-q2l,true)+Integrate(-0.05f,-2f,-q2l,-q3l,true)+Integrate(0f,k1l,-q3l,-q4l,true)+Integrate(0.025f,3.25f,-q4l,-130f,true))/(Integrate(-0.1f,-1f,-10f,-q1l,false)+Integrate(0f,k2l,-q1l,-q2l,false)+Integrate(-0.05f,-2f,-q2l,-q3l,false)+Integrate(0f,k1l,-q3l,-q4l,false)+Integrate(0.025f,3.25f,-q4l,-130f,false));
            }
            else if (k1l > k2l)//НАДО СЧИТАТЬ ПЕРЕМЫЧКУ МЕЖДУ Медлено и средне
            {
                left=(Integrate(-0.1f,-1f,-10f,-q1l,true)+Integrate(0f,k2l,-q1l,-q2l,true)+Integrate(0.05f,3f,-q2l,-q3l,true)+Integrate(0f,k1l,-q3l,-q4l,true)+Integrate(0.025f,3.25f,-q4l,-130f,true))/(Integrate(-0.1f,-1f,-10f,-q1l,false)+Integrate(0f,k2l,-q1l,-q2l,false)+Integrate(0.05f,3f,-q2l,-q3l,false)+Integrate(0f,k1l,-q3l,-q4l,false)+Integrate(0.025f,3.25f,-q4l,-130f,false));
        }
        }
        else if (part_funtion_l == 4f)// быстро зависит от далеко 
        {
            left=(Integrate(-0.1f,-1f,-10f,-20f,true)+Integrate(0f,1f,-20f,-40f,true)+Integrate(0.05f,3f,-40f,-60f,true))/(Integrate(-0.1f,-1f,-10f,-20f,false)+Integrate(0f,1f,-20f,-40f,false)+Integrate(0.05f,3f,-40f,-60f,false));
        }
        


        if (part_funtion_r == 1f)// остановка
        {
            right= (Integrate(0.025f,-2.25f,90f,130f,true)+Integrate(0f,1f,130f,150f,true))/(Integrate(0.025f,-2.25f,90f,130f,false)+Integrate(0f,1f,130f,150f,false));
        }
        //если левая меньше чем правая возрастает на пересечений функции
        //если правая больше чем левая убывает до пересесеыения функции
        else if (part_funtion_r == 1.5f)// быстро зависит от далеко 
        {

            if (k1r == k2r)
            {
                right= (Integrate(0.05f,-2f,40f,q1r,true)+Integrate(0f,0.5f,q1r,150f,true))/(Integrate(0.05f,-2f,40f,q1r,false)+Integrate(0f,0.5f,q1r,150f,false));
            }
            else if (k1r < k2r)
            {
                right= (Integrate(0.05f,-2f,40f,q1r,true)+Integrate(0f,k2r,q1r,q2r,true)+Integrate(0.025f,-2.25f,q2r,q3r,true)+Integrate(0f,k1r,q3r,150f,true))/(Integrate(0.05f,-2f,40f,q1r,false)+Integrate(0f,k1r,q1r,q2r,false)+Integrate(0.025f,-2.25f,q2r,q3r,false)+Integrate(0f,k1r,q3r,150f,false));
            }
            else if (k1r > k2r)
            {
                right= (Integrate(0.05f,-2f,40f,q1r,true)+Integrate(0f,k1r,q1r,q2r,true)+Integrate(-0.025f,3.25f,q2r,q3r,true)+Integrate(0f,k1r,q3r,150f,true))/(Integrate(0.05f,-2f,40f,q1r,false)+Integrate(0f,k1r,q1r,q2r,false)+Integrate(-0.025f,3.25f,q2r,q3r,false)+Integrate(0f,k1r,q3r,150f,false));
            }
        }
        else if (part_funtion_r == 2f)// средне зависит от средне
        {
            right= (Integrate(0.05f,-2f,40f,60f,true)+Integrate(0f,1f,60f,90f,true)+Integrate(-0.025f,3.25f,90f,130f,true))/(Integrate(0.05f,-2f,40f,60f,false)+Integrate(0f,1f,60f,90f,false)+Integrate(-0.025f,3.25f,90f,130f,false));
        }
        else if (part_funtion_r == 2.5f)// быстро зависит от далеко 
        {

            if (k1r == k2r)
            {
                //возрастание в начале дальше прямая линия
                right= (Integrate(0.1f,-1f,10f,q1r,true)+Integrate(0f,k2r,q1r,q2r,true)+Integrate(-0.025f,3.25f,q2r,130f,true))/(Integrate(0.1f,-1f,10f,q1r,false)+Integrate(0f,k2r,q1r,q2r,false)+Integrate(-0.025f,3.25f,q2r,130f,false));
            }
            else if (k1r < k2r)
            {
                right= (Integrate(0.1f,-1f,10f,q1r,true)+Integrate(0f,k2r,q1r,q2r,true)+Integrate(0.05f,-2f,q2r,q3r,true)+Integrate(0f,k1r,q3r,q4r,true)+Integrate(-0.025f,3.25f,q4r,130f,true))/(Integrate(0.1f,-1f,10f,q1r,false)+Integrate(0f,k2r,q1r,q2r,false)+Integrate(0.05f,-2f,q2r,q3r,false)+Integrate(0f,k1r,q3r,q4r,false)+Integrate(-0.025f,3.25f,q4r,130f,false));
            }
            else if (k1r > k2r)//НАДО СЧИТАТЬ ПЕРЕМЫЧКУ МЕЖДУ Медлено и средне
            {
                right= (Integrate(0.1f,-1f,10f,q1r,true)+Integrate(0f,k2r,q1r,q2r,true)+Integrate(-0.05f,3f,q2r,q3r,true)+Integrate(0f,k1r,q3r,q4r,true)+Integrate(-0.025f,3.25f,q4r,130f,true))/(Integrate(0.1f,-1f,10f,q1r,false)+Integrate(0f,k2r,q1r,q2r,false)+Integrate(-0.05f,3f,q2r,q3r,false)+Integrate(0f,k1r,q3r,q4r,false)+Integrate(-0.025f,3.25f,q4r,130f,false));
            }
        }
        else if (part_funtion_r == 3f)//медлено зависи от очень близко
        {
            right= (Integrate(0.1f,-1f,10f,20f,true)+Integrate(0f,1f,20f,40f,true)+Integrate(-0.05f,3f,40f,60f,true))/(Integrate(0.1f,-1f,10f,20f,false)+Integrate(0f,1f,20f,40f,false)+Integrate(-0.05f,3f,40f,60f,false));
        }
        else if (part_funtion_r == 3.5f)// быстро зависит от далеко 
        {
            if (k1r == k2r)
            {
                //возрастание в начале дальше прямая линия
                right= (Integrate(0f,k2r,0f,50f,true)+Integrate(-0.05f,3f,50f,60f,true))/(Integrate(0f,k2r,0f,50f,false)+Integrate(-0.05f,3f,50f,60f,false));
            }
            else if (k1r < k2r)
            {
                right= (Integrate(0f,k2r,0f,q1r,true)+Integrate(0.1f,-1f,q1r,15f,true)+Integrate(-0.1f,2f,15f,q2r,true)+Integrate(0f,k2r,q2r,q3r,true)+Integrate(0.05f,-2f,q3r,60f,true))/(Integrate(0f,k2r,0f,q1r,false)+Integrate(0.1f,-1f,q1r,15f,false)+Integrate(-0.1f,2f,15f,q2r,false)+Integrate(0f,k2r,q2r,q3r,false)+Integrate(0.05f,-2f,q3r,60f,false));
            }
            else if (k1r > k2r)//НАДО СЧИТАТЬ ПЕРЕМЫЧКУ МЕЖДУ Медлено и средне
            {
                right= (Integrate(0f,k2r,0f,q1r,true)+Integrate(-0.1f,2f,q1r,q2r,true)+Integrate(0f,k1r,q2r,q3r,true)+Integrate(-0.05f,3f,q3r,60f,true))/(Integrate(0f,k2r,10f,q1r,false)+Integrate(-0.1f,2f,q1r,q2r,false)+Integrate(0f,k1r,q2r,q3r,false)+Integrate(-0.05f,3f,q3r,60f,false));
            }
        }
        else if (part_funtion_r == 4f)// быстро зависит от далеко 
        {
            right= (Integrate(0f,1f,0f,10f,true)+Integrate(-0.1f,2f,10f,20f,true))/(Integrate(0f,1f,0f,10f,true)+Integrate(-0.1f,2f,10f,20f,false));
        }
        }

        

        return right+left;
        }
}
}