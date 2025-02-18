using UnityEngine;
using UnityEditor;
using System.Collections;


[CustomEditor(typeof(Platform))]
[CanEditMultipleObjects]
public class PlatformEditor : Editor
{
    private Platform platform
    {
        get
        {
            return (Platform)target;
        }
    }

    public override void OnInspectorGUI()
    {
        Undo.RecordObject(target, "Undo");
        platform.moveType = (Platform.MoveType)EditorGUILayout.EnumPopup("Movement Type", platform.moveType);

        switch (platform.moveType)
        {
            case Platform.MoveType.AroundPivot:
                platform.moveRadius = EditorGUILayout.FloatField("Move Radius", platform.moveRadius);
                platform.moveSpeed = EditorGUILayout.FloatField("Move Speed", platform.moveSpeed);
                break;
            case Platform.MoveType.PointsBased:
                platform.pointA = EditorGUILayout.Vector2Field("Point A", platform.pointA);
                platform.pointB = EditorGUILayout.Vector2Field("Point B", platform.pointB);
                platform.moveSpeed = EditorGUILayout.FloatField("Move Speed", platform.moveSpeed);
                platform.stopTime = EditorGUILayout.FloatField("Stop Time", platform.stopTime);
                break;
            case Platform.MoveType.None:
                break;
        }

        platform.Coin = (Transform)EditorGUILayout.ObjectField("Coin", platform.Coin, typeof(Transform), true);

        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }

}

