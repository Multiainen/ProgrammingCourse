using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.InteropServices;

public static class BaseOps
{
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    public static int RoundFloat(float f)
    {
        if (f > 0) return (int)(f + .50001f);
        else return (int)(f - .50001f);
    }

    public static int Pot(int num, int power)
    {
        int ret = 1;
        for (int i = 0; i < power; i++) ret *= num;
        return ret;
    }

    public static int Pot(int num, int multi, int power)
    {
        int ret = num;
        for (int i = 1; i < power; i++) ret *= multi;
        return ret;
    }

    public static float MagSqr(float2 a, float2 b)
    {
        return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y);
    }

    public static float3[] CombineArray(float3[] a1, float3[] a2, int overlap, int underlap)
    {
        float3[] returnArray = new float3[a1.Length + a2.Length - overlap - underlap];
        for (int i = 0; i < a1.Length - underlap; i++)
        {
            returnArray[i] = a1[i];
        }
        for (int i = overlap; i < a2.Length; i++)
        {
            returnArray[a1.Length - overlap + i - underlap] = a2[i];
        }

        return returnArray;
    }

    public static int[] CombineArray(int[] a1, int[] a2, int overlap, int underlap)
    {
        int[] returnArray = new int[a1.Length + a2.Length - overlap - underlap];
        for (int i = 0; i < a1.Length - underlap; i++)
        {
            returnArray[i] = a1[i];
        }
        for (int i = overlap; i < a2.Length; i++)
        {
            returnArray[a1.Length - overlap + i - underlap] = a2[i];
        }

        return returnArray;
    }

    public static T[] CombineArray<T>(T[] a, T[] b)
    {
        T[] ret = new T[a.Length + b.Length];
        for (int i = 0; i < a.Length; i++) ret[i] = a[i];
        for (int i = a.Length; i < ret.Length; i++) ret[i] = b[i - a.Length];
        return ret;
    }

    public static T[] CutArray<T>(T[] a, int index, int length)
    {
        T[] ret = new T[a.Length - length];
        for (int i = 0; i < index; i++) ret[i] = a[i];
        for (int i = index; i < ret.Length; i++) ret[i] = a[i + length];
        return ret;
    }

    public static T[] CutArray<T>(T[] a, int index)
    {
        T[] ret = new T[index];
        for (int i = 0; i < index; i++) ret[i] = a[i];
        return ret;
    }

    public static int[] ExtractXArray(int2[] a)
    {
        int[] ret = new int[a.Length];
        for (int i = 0; i < ret.Length; i++) ret[i] = a[i].x;
        return ret;
    }

    public static int[] ExtractYArray(int2[] a)
    {
        int[] ret = new int[a.Length];
        for (int i = 0; i < ret.Length; i++) ret[i] = a[i].y;
        return ret;
    }

    public static T[] Reverse<T>(T[] a)
    {
        T[] ret = new T[a.Length];
        for (int i = 0; i < a.Length; i++) ret[ret.Length - 1 - i] = a[i];
        return ret;
    }

    public static float3[] CropArray(float3[] array, int index, int pre, int post)
    {
        float3[] returnArray = new float3[0];

        if (index >= pre && index < array.Length - post)
        {
            returnArray = new float3[pre + post + 1];
            for (int i = index - pre, j = 0; i < index + post + 1; i++, j++)
                returnArray[j] = array[i];
            return returnArray;
        }
        if (index >= pre)
        {
            returnArray = new float3[pre + array.Length - index];
            for (int i = index - pre, j = 0; i < array.Length; i++, j++)
                returnArray[j] = array[i];
            return returnArray;
        }
        if (index < array.Length - post)
        {
            returnArray = new float3[index + post + 1];
            for (int i = 0; i < index + post + 1; i++)
                returnArray[i] = array[i];
            return returnArray;
        }

        return array;
    }

    public static Vector3[] CropArray(Vector3[] array, int index, int pre, int post)
    {
        Vector3[] returnArray = new Vector3[0];

        if (index >= pre && index < array.Length - post)
        {
            returnArray = new Vector3[pre + post + 1];
            for (int i = index - pre, j = 0; i < index + post + 1; i++, j++)
                returnArray[j] = array[i];
            return returnArray;
        }
        if (index >= pre)
        {
            returnArray = new Vector3[pre + array.Length - index];
            for (int i = index - pre, j = 0; i < array.Length; i++, j++)
                returnArray[j] = array[i];
            return returnArray;
        }
        if (index < array.Length - post)
        {
            returnArray = new Vector3[index + post + 1];
            for (int i = 0; i < index + post + 1; i++)
                returnArray[i] = array[i];
            return returnArray;
        }

        return array;
    }

    public static float3[] SlotArray(float3[] outer, float3[] inner, int index)
    {
        if (inner.Length > outer.Length && index < 1)
        {
            return inner;
        }
        else if (index + inner.Length > outer.Length)
        {
            float3[] newArray = new float3[index + inner.Length];
            for (int i = 0; i < index; i++)
            {
                newArray[i] = outer[i];
            }
            for (int i = index, j = 0; i < newArray.Length; i++, j++)
            {
                newArray[i] = inner[j];
            }

            return newArray;
        }
        else
        {
            for (int i = index, j = 0; i < index + inner.Length; i++, j++)
            {
                outer[i] = inner[j];
            }
        }

        return outer;
    }

    public static Vector3[] SlotArray(Vector3[] outer, Vector3[] inner, int index)
    {
        if (inner.Length > outer.Length && index < 1)
        {
            return inner;
        }
        else if (index + inner.Length > outer.Length)
        {
            Vector3[] newArray = new Vector3[index + inner.Length];
            for (int i = 0; i < index; i++)
            {
                newArray[i] = outer[i];
            }
            for (int i = index, j = 0; i < newArray.Length; i++, j++)
            {
                newArray[i] = inner[j];
            }

            return newArray;
        }
        else
        {
            for (int i = index, j = 0; i < index + inner.Length; i++, j++)
            {
                outer[i] = inner[j];
            }
        }

        return outer;
    }

    public static float3[] SlotArray(float3[] outer, float3[] inner, int index, int post) // crop out "post" number of elements from outer array after insertion of inner array at index
    {
        float3[] returnArray;
        if (outer.Length - post > index + inner.Length) returnArray = new float3[outer.Length - post];
        else returnArray = new float3[index + inner.Length];

        for (int i = 0; i < index; i++)
        {
            returnArray[i] = outer[i];
        }
        for (int i = index, j = 0; i < index + inner.Length; i++, j++)
        {
            returnArray[i] = inner[j];
        }
        for (int i = index + inner.Length, j = index + inner.Length + post; j < outer.Length; i++, j++)
        {
            returnArray[i] = outer[j];
        }

        return returnArray;
    }

    public static Vector3[] SlotArray(Vector3[] outer, Vector3[] inner, int index, int post) // crop out "post" number of elements from outer array after insertion of inner array at index
    {
        Vector3[] returnArray = new Vector3[outer.Length - post];

        for (int i = 0; i < index; i++)
        {
            returnArray[i] = outer[i];
        }
        for (int i = index, j = 0; i < index + inner.Length; i++, j++)
        {
            returnArray[i] = inner[j];
        }
        for (int i = index + inner.Length, j = index + inner.Length + post; j < outer.Length; i++, j++)
        {
            returnArray[i] = outer[j];
        }

        return returnArray;
    }

    public static Vector3[] F3V3(float3[] fArray)
    {
        Vector3[] vArray = new Vector3[fArray.Length];
        for (int i = 0; i < fArray.Length; i++) vArray[i] = fArray[i];
        return vArray;
    }

    public static Vector3[] F2V3(float2[] fArray)
    {
        Vector3[] vArray = new Vector3[fArray.Length];
        for (int i = 0; i < fArray.Length; i++) vArray[i] = new Vector3(fArray[i].x, 0, fArray[i].y);
        return vArray;
    }

    public static float3[] V3F3(Vector3[] fArray)
    {
        float3[] vArray = new float3[fArray.Length];
        for (int i = 0; i < fArray.Length; i++) vArray[i] = fArray[i];
        return vArray;
    }

    public static float2[] V3F2(Vector3[] fArray)
    {
        float2[] vArray = new float2[fArray.Length];
        for (int i = 0; i < fArray.Length; i++) vArray[i] = new float2(fArray[i].x, fArray[i].z);
        return vArray;
    }

    public static float2[] QueueToArray(Queue<float2> queue)
    {
        float2[] array = new float2[queue.Count];
        for (int i = 0; i < array.Length; i++) array[i] = queue.Dequeue();
        return array;
    }

    public static T[] ListToArray<T>(List<T> list)
    {
        T[] array = new T[list.Count];
        for (int i = 0; i < list.Count; i++) array[i] = list[i];
        return array;
    }

    public static HashSet<T> ListToHash<T>(List<T> list)
    {
        HashSet<T> hash = new HashSet<T>();
        for (int i = 0; i < list.Count; i++) hash.Add(list[i]);
        return hash;
    }

    public static List<T> RandomOrder<T>(List<T> list)
    {
        if (list.Count < 2) return list;
        List<T> ret = new List<T>();
        int index;
        while (list.Count > 1) { index = UnityEngine.Random.Range(0, list.Count); ret.Add(list[index]); list.RemoveAt(index); }
        ret.Add(list[0]);
        return ret;
    }

    public static HashSet<T> ArrayToHash<T>(T[] array)
    {
        HashSet<T> hash = new HashSet<T>();
        for (int i = 0; i < array.Length; i++) hash.Add(array[i]);
        return hash;
    }

    public static List<int> ArrayToList(int[] array)
    {
        List<int> list = new List<int>();

        for (int i = 0; i < array.Length; i++) list.Add(array[i]);
        return list;
    }

    public static List<float2> HashToList(HashSet<float2> hash)
    {
        List<float2> list = new List<float2>();
        foreach (float2 f in hash) list.Add(f);
        return list;
    }

    public static T[] HashToArray<T>(HashSet<T> hash)
    {
        int counter = 0;
        T[] ret = new T[hash.Count];
        foreach (T f in hash) { ret[counter] = f; counter++; }
        return ret;
    }

    public static HashSet<T> StackTohash<T>(Stack<T> stack)
    {
        HashSet<T> ret = new HashSet<T>();
        while (stack.Count > 0) ret.Add(stack.Pop());
        return ret;
    }

    public static T[] StackToArray<T>(Stack<T> stack)
    {
        T[] ret = new T[stack.Count];
        int counter = 0;
        while (stack.Count > 0) { ret[counter] = stack.Pop(); counter++; }
        return ret;
    }

    public static T[] AddSlot<T>(T[] array)
    {
        T[] ret = new T[array.Length + 1];
        for (int i = 0; i < array.Length; i++) ret[i] = array[i];
        return ret;
    }

    public static T[] RemoveSlot<T>(T[] array)
    {
        T[] ret = new T[array.Length - 1];
        for (int i = 0; i < ret.Length; i++) ret[i] = array[i];
        return ret;
    }

    public static T[] RemoveAt<T>(T[] array, int index)
    {
        T[] ret = new T[array.Length - 1];
        for (int i = 0; i < index; i++) ret[i] = array[i];
        for (int i = index; i < ret.Length; i++) ret[i] = array[i + 1];
        return ret;
    }

    public static T[] AddSlot<T>(T[] array, T addition)
    {
        T[] ret = new T[array.Length + 1];
        for (int i = 0; i < array.Length; i++) ret[i] = array[i];
        ret[array.Length] = addition;
        return ret;
    }

    public static T[] Insert<T>(T[] array, T insert, int index)
    {
        for (int i = array.Length - 1; i > index; i--)
            array[i] = array[i - 1];
        array[index] = insert;
        return array;
    }

    public static float F3Sqr(float3 f)
    {
        return f.x * f.x + f.y * f.y + f.z * f.z;
    }

    public static float V3Sqr(Vector3 f)
    {
        return f.x * f.x + f.y * f.y + f.z * f.z;
    }

    public static float F2Sqr(float3 f)
    {
        return f.x * f.x + f.z * f.z;
    }

    public static float F2Sqr(float2 f)
    {
        return f.x * f.x + f.y * f.y;
    }

    public static int I2Sqr(int2 f)
    {
        return f.x * f.x + f.y * f.y;
    }

    public static float3 NormalizeF2(float3 f)
    {
        return f / math.sqrt(F2Sqr(f));
    }

    public static float2 CrdKey(float3 f)
    {
        return new float2(Curves.RoundFloat(f.x), Curves.RoundFloat(f.z));
    }

    public static int Steps(float f) // default: return multiplier 2 if over .5f, 1 if over 0, same for negatives
    {
        if (f > 0)
        {
            if (f > .5f) return 2;
            return 1;
        }
        if (f < -.5f) return -2;
        return -1;
    }

    public static float Clamp(float f)
    {
        if (f > 1) return 1;
        else if (f < 0) return 0;
        return f;
    }

    public static float Clamp(float f, float min, float max)
    {
        if (f > max) return max;
        else if (f < min) return min;
        return f;
    }

    public static float ClampP(float f)
    {
        if (f > 1) return 1;
        return f;
    }

    public static bool Adj(int2 a, int2 b)
    {
        if ((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) == 1) return true;
        return false;
    }

    public static bool Within(int2 a, int2 b, float dist)
    {
        if ((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) <= dist * dist) return true;
        return false;
    }

    public static int AdjI(int2 a, int2 b)
    {
        if ((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) == 1) return 1;
        return 0;
    }

    public static float TruncF(float f, int places)
    {
        int multi = 1;
        for (int i = 0; i < places; i++) multi *= 10;
        return RoundFloat(f * multi) / (float)multi;
    }

    public static float Mgtd(float3 f)
    {
        float precision = 0.0001f;
        float min = 0;
        float num = (f.x * f.x) + (f.y * f.y) + (f.z * f.z);
        float max;
        float result = 0;

        if (num < 1) max = 1;
        else max = num;
        while (max - min > precision)
        {
            result = (min + max) / 2;
            if ((result * result) >= num)
            {
                max = result;
            }
            else
            {
                min = result;
            }
        }
        return result;
    }

    public static float Mgtd(float2 f)
    {
        float precision = .002f;
        float min = 0;
        float num = (f.x * f.x) + (f.y * f.y);
        float max;
        float result = 0;

        if (num < 1) max = 1;
        else max = num;
        while (max - min > precision)
        {
            result = (min + max) / 2;
            if ((result * result) >= num)
            {
                max = result;
            }
            else
            {
                min = result;
            }
        }
        return result;
    }

    public static float Mgtd2(float2 f)
    {
        float precision = .002f;
        float min = 0;
        float num = (f.x * f.x) + (f.y * f.y);
        float max;
        float result = 0;

        if (num < 1) max = 1;
        else max = num;
        while (max - min > precision)
        {
            result = (min + max) / 2;
            if ((result * result) >= num)
            {
                max = result;
            }
            else
            {
                min = result;
            }
        }
        return result;
    }

    public static float Mgtd2(float3 f)
    {
        float precision = .002f;
        float min = 0;
        float num = (f.x * f.x) + (f.z * f.z);
        float max;
        float result = 0;

        if (num < 1) max = 1;
        else max = num;
        while (max - min > precision)
        {
            result = (min + max) / 2;
            if ((result * result) >= num)
            {
                max = result;
            }
            else
            {
                min = result;
            }
        }
        return result;
    }

    public static float Mgtd(float2 f, float prec)
    {
        float precision = prec;
        float min = 0;
        float num = (f.x * f.x) + (f.y * f.y);
        float max;
        float result = 0;

        if (num < 1) max = 1;
        else max = num;
        while (max - min > precision)
        {
            result = (min + max) / 2;
            if ((result * result) >= num)
            {
                max = result;
            }
            else
            {
                min = result;
            }
        }
        return result;
    }

    public static bool F2Eq(float3 a, float3 b)
    {
        if (a.x == b.x && a.z == b.z && a.y == b.y) return true;
        else return false;
    }

    public static bool F2Eq(float2 a, float2 b)
    {
        if (a.x == b.x && a.y == b.y) return true;
        else return false;
    }

    public static bool F3Eq(float3 a, float3 b)
    {
        if (a.x == b.x && a.z == b.z && a.y == b.y) return true;
        else return false;
    }

    public static float2[] F3F2(float3[] f3)
    {
        float2[] f2 = new float2[f3.Length];
        for (int i = 0; i < f2.Length; i++)
            f2[i] = new float2(f3[i].x, f3[i].z);
        return f2;
    }

    public static Vector3[] SortByDistance(Vector3[] vectors, Vector3 from)
    {
        float curDist;
        float[] distances = new float[vectors.Length];
        Vector3[] final = new Vector3[vectors.Length];
        int step;
        int cur;
        bool up;

        if (vectors.Length < 2) return vectors;

        distances[0] = F2Sqr(vectors[0] - from);
        final[0] = vectors[0];

        for (int i = 1; i < vectors.Length; i++)
        {
            curDist = F2Sqr(vectors[i] - from);
            cur = i / 2;
            step = (cur + 1) / 2;
            up = true;

            if (curDist < distances[0]) cur = 0;
            else if (curDist > distances[i - 1])
            {
                final[i] = vectors[i];
                continue;
            }
            else
            {
                for (int j = 0; j < vectors.Length; j++)
                {
                    if (up)
                    {
                        if (curDist <= distances[cur])
                        {
                            up = false;
                            cur -= step;
                        }
                        else cur += step;
                    }
                    else
                    {
                        if (curDist >= distances[cur])
                        {
                            up = true;
                            cur += step;
                        }
                        else cur -= step;
                    }

                    if (cur < 1) cur = 1;
                    else if (cur > i - 2 && i > 2) cur = i - 2;

                    if (step < 1)
                    {
                        if (up) cur++;
                        break;
                    }

                    step = (step + 1) / 2;
                }
            }

            for (int j = i - 1; j >= cur; j--)
            {
                final[j + 1] = final[j];
            }

            final[cur] = vectors[i];
        }

        return final;
    }

    public static int Rot90(float x, float y, int rot)
    {
        if (rot < 4)
        {
            if (rot < 2)
            {
                if (rot == 0) return (int)x;
                else return (int)y;
            }
            else
            {
                if (rot == 2) return (int)-x;
                else return (int)-y;
            }
        }
        else
        {
            if (rot < 6)
            {
                if (rot == 4) return (int)y;
                else return (int)-x;
            }
            else
            {
                if (rot == 6) return (int)-y;
                else return (int)x;
            }
        }
    }

    public static int AmtIn(int[,] array, int element)
    {
        int ret = 0;
        for (int i = 0; i < array.GetLength(0); i++)
            for (int j = 0; j < array.GetLength(1); j++)
                if (array[i, j] == element)
                    ret++;
        return ret;
    }

    public static T[] Shuffle<T>(T[] array)
    {
        T[] ret = new T[array.Length];
        List<int> order = new List<int>();
        int rdm;
        for (int i = 0; i < ret.Length; i++) order.Add(i);
        for (int i = 0; i < ret.Length; i++)
        {
            rdm = UnityEngine.Random.Range(0, order.Count);
            ret[i] = array[order[rdm]];
            order.RemoveAt(rdm);
        }
        return ret;
    }

    public static List<T> Shuffle<T>(List<T> list)
    {
        List<T> ret = new List<T>();
        List<int> order = new List<int>();
        int rdm;
        for (int i = 0; i < list.Count; i++) order.Add(i);
        for (int i = 0; i < list.Count; i++)
        {
            rdm = UnityEngine.Random.Range(0, order.Count);
            ret.Add(list[order[rdm]]);
            order.RemoveAt(rdm);
        }
        return ret;
    }

    public static Stack<T> Copy<T>(Stack<T> stack)
    {
        return new Stack<T>(new Stack<T>(stack));
    }

    public static T[] Copy<T>(T[] array)
    {
        T[] ret = new T[array.Length];
        for (int i = 0; i < ret.Length; i++) ret[i] = array[i];
        return ret;
    }

    public static HashSet<T> Copy<T>(HashSet<T> array)
    {
        HashSet<T> ret = new HashSet<T>();
        foreach (T i in array) ret.Add(i);
        return ret;
    }

    public static T[] AddArray<T>(T[] a1, T[] a2)
    {
        T[] ret = new T[a1.Length + a2.Length];
        for (int i = 0; i < a1.Length; i++) ret[i] = a1[i];
        for (int i = a1.Length; i < ret.Length; i++) ret[i] = a2[i - a1.Length];
        return ret;
    }

    public static T[] AppendArray<T>(T[] a1, T add)
    {
        T[] ret = new T[a1.Length + 1];
        for (int i = 0; i < a1.Length; i++) ret[i] = a1[i];
        ret[ret.Length - 1] = add;
        return ret;
    }

    public static Stack<T> Shuffle<T>(Stack<T> stack)
    {
        T[] temp = new T[stack.Count];
        Stack<T> ret = new Stack<T>();
        List<int> order = new List<int>();
        int rdm;
        for (int i = 0; i < temp.Length; i++)
        {
            order.Add(i);
            temp[i] = stack.Pop();
        }
        for (int i = 0; i < temp.Length; i++)
        {
            rdm = UnityEngine.Random.Range(0, order.Count);
            ret.Push(temp[order[rdm]]);
            order.RemoveAt(rdm);
        }
        return ret;
    }

    public static Queue<T> Shuffle<T>(Queue<T> stack)
    {
        T[] temp = new T[stack.Count];
        Queue<T> ret = new Queue<T>();
        List<int> order = new List<int>();
        int rdm;
        for (int i = 0; i < temp.Length; i++)
        {
            order.Add(i);
            temp[i] = stack.Dequeue();
        }
        for (int i = 0; i < temp.Length; i++)
        {
            rdm = UnityEngine.Random.Range(0, order.Count);
            ret.Enqueue(temp[order[rdm]]);
            order.RemoveAt(rdm);
        }
        return ret;
    }

    public static string FormatNumText(float f)
    {
        f = TruncF(f, 2);
        if (f < 0) return "<color=#FF3333>" + RoundFloat(f * 100) + "%</color>";
        else if (f > 0) return "<color=green>+" + RoundFloat(f * 100) + "%</color>";
        else return "0%";
    }

    public static string FormatNumTextFlat(float f)
    {
        return RoundFloat(f * 100) + "%";
    }

    public static string FormatNumText2Float(float f)
    {
        f = TruncF(f, 2);
        if (f < 0) return "<color=#FF3333>" + f + "</color>";
        else if (f > 0) return "<color=green>+" + f + "</color>";
        else return (f).ToString();
    }

    public static float FormatNumFlat(float f)
    {
        return RoundFloat(f * 100) * .01f;
    }

    public static float FormatNumFlat1(float f)
    {
        return RoundFloat(f * 10) * .1f;
    }

    public static string Romanize(int i)
    {
        if (i < 32)
        {
            if (i < 16)
            {
                if (i < 8)
                {
                    if (i < 4)
                    {
                        if (i < 2)
                        {
                            if (i == 0) return "0";
                            return "I";
                        }
                        else
                        {
                            if (i == 2) return "II";
                            return "III";
                        }
                    }
                    else
                    {
                        if (i < 6)
                        {
                            if (i == 4) return "IV";
                            return "V";
                        }
                        else
                        {
                            if (i == 6) return "VI";
                            return "VII";
                        }
                    }
                }
                else
                {
                    if (i < 12)
                    {
                        if (i < 10)
                        {
                            if (i == 8) return "VIII";
                            return "IX";
                        }
                        else
                        {
                            if (i == 10) return "X";
                            return "XI";
                        }
                    }
                    else
                    {
                        if (i < 14)
                        {
                            if (i == 12) return "XII";
                            return "XIII";
                        }
                        else
                        {
                            if (i == 14) return "XIV";
                            return "XV";
                        }
                    }
                }
            }
            else
            {
                if (i < 24)
                {
                    if (i < 20)
                    {
                        if (i < 18)
                        {
                            if (i == 16) return "XVI";
                            return "XVII";
                        }
                        else
                        {
                            if (i == 18) return "XVIII";
                            return "XIX";
                        }
                    }
                    else
                    {
                        if (i < 22)
                        {
                            if (i == 20) return "XX";
                            return "XXI";
                        }
                        else
                        {
                            if (i == 22) return "XXII";
                            return "XXIII";
                        }
                    }
                }
                else
                {
                    if (i < 28)
                    {
                        if (i < 26)
                        {
                            if (i == 24) return "XXIV";
                            return "XXV";
                        }
                        else
                        {
                            if (i == 26) return "XXVI";
                            return "XXVII";
                        }
                    }
                    else
                    {
                        if (i < 30)
                        {
                            if (i == 28) return "XXVIII";
                            return "XXIX";
                        }
                        else
                        {
                            if (i == 30) return "XXX";
                            return "XXXI";
                        }
                    }
                }
            }
        }
        else
        {
            return "NaN";
        }
    }

    public static int SubFaceToRoadLayout(int subFace)
    {
        if (subFace % 4 < 2)
        {
            if (subFace % 4 == 0) return subFace / 16 * 2 + subFace / 4 % 4 * 10;
            else return subFace / 16 * 2 + subFace / 4 % 4 * 10 + 11;
        }
        if (subFace % 4 == 2) return subFace / 16 * 2 + subFace / 4 % 4 * 10 + 2;
        return subFace / 16 * 2 + subFace / 4 % 4 * 10 + 1;
    }

    public static int2[] PresentInTiles(float2 botLeft, float2 topRight, float rot)
    {
        List<int2> tiles = new List<int2>();
        float2 centre = (botLeft + topRight) / 2;



        int2[] ret = new int2[tiles.Count];
        for (int i = 0; i < ret.Length; i++) ret[i] = tiles[i];
        return ret;
    }

    public static string FormatLargeNumber(float f)
    {
        if (f < 10000) return f.ToString();
        if (f < 1000000) return (int)(f / 1000) + "K";
        if (f < 10000000) return (int)(f / 100000) / 10f + "M";
        if (f < 1000000000) return (int)(f / 1000000) + "M";
        return f / 1000000000 + "B";
    }
}
