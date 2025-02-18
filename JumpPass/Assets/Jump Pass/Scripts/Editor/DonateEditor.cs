using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(Donate))]
public class DonateEditor : Editor {

    Donate donate
    {
        get
        {
            return (Donate)target;
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Label("Coins: " + PlayerPrefs.GetInt("Coins"));
        EditorGUILayout.BeginHorizontal();
        donate.Coins = EditorGUILayout.IntField("Coins To Add", donate.Coins);
        if (GUILayout.Button("DONATE", EditorStyles.toolbarButton))
            donate.DonateCoins();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);
        if (GUILayout.Button("CLEAN PLAYER PREFS", EditorStyles.toolbarButton))
            PlayerPrefs.DeleteAll();
        EditorGUILayout.EndVertical();
    }
}
