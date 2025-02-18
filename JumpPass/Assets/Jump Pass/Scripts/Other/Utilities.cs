using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

public static class Utilities
{
    //Play sound effect function;
    public static void PlaySFX(AudioSource source, AudioClip clip, float volume, bool loop = false)
    {
        if (source)
        {
            source.loop = loop;
            source.volume = volume;
            source.clip = clip;
            source.Play();
        }
    }
    //Draw lable gizmo function;
    public static void SceneLabel(Vector3 position, Vector2 Offset, float distance, float RectSizeX, float RectSizeY, string text, int TextSize, FontStyle fontStyle, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.BeginGUI();
        Vector3 newPos = new Vector3(position.x + Offset.x, position.y + Offset.y, position.z);

        Vector3 guiPoint = UnityEditor.HandleUtility.WorldToGUIPoint(newPos);
        GUIStyle skin = new GUIStyle();
        skin.fontStyle = fontStyle;
        skin.normal.textColor = color;
        skin.fontSize = TextSize;
        skin.alignment = TextAnchor.MiddleCenter;
        Rect rect = new Rect(guiPoint.x - RectSizeX / 2, guiPoint.y - RectSizeY / 2, RectSizeX, RectSizeY);

        Vector3 oncam = Camera.current.WorldToScreenPoint(newPos);
        if (oncam.x >= 0 && oncam.x <= Camera.current.pixelWidth && oncam.y >= 0 &&
            oncam.y <= Camera.current.pixelHeight && oncam.z > 0 && oncam.z < distance)
            GUI.Label(rect, text, skin);

        UnityEditor.Handles.EndGUI();
#endif
    }
    //Draw radius gizmo function;
    public static void DrawRadius(Vector3 position, float centerRadius, Color centerColor, float innerRadius, Color innerColor)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = centerColor;
        UnityEditor.Handles.DrawSolidDisc(position, Vector3.forward, centerRadius);
        UnityEditor.Handles.color = innerColor;
        UnityEditor.Handles.DrawWireDisc(position, Vector3.forward, innerRadius);
        UnityEditor.Handles.color = Color.white;
#endif
    }
    //Draw solid dot gizmo function;
    public static void DrawDot(Vector3 position, float radius, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawSolidDisc(position, Vector3.forward, radius);
        UnityEditor.Handles.color = Color.white;
#endif
    }
    //Shuffle list function;
    public static void Shuffle<T>(this IList<T> list)
    {
        RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
        int n = list.Count;
        while (n > 1)
        {
            byte[] box = new byte[1];
            do provider.GetBytes(box);
            while (!(box[0] < n * (Byte.MaxValue / n)));
            int k = (box[0] % n);
            n--;
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    //Player prefs bool implementation;
    public static void SetBool(string name, bool booleanValue)
    {
        PlayerPrefs.SetInt(name, booleanValue ? 1 : 0);
    }
    public static bool GetBool(string name)
    {
        return PlayerPrefs.GetInt(name) == 1 ? true : false;
    }
}
