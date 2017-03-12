using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(DynamicNavElement))]
public class DynamicNavElementEditor : Editor
{
	public void OnSceneGUI()
	{
		var t = (target as DynamicNavElement);

		EditorGUI.BeginChangeCheck ();
		Vector3 pos = Handles.PositionHandle (t.min, Quaternion.identity);
		if (EditorGUI.EndChangeCheck ()) {
			Undo.RecordObject (target, "Move Dynamic region min");
			t.min = pos;
		}

		EditorGUI.BeginChangeCheck ();
		pos = Handles.PositionHandle (t.max, Quaternion.identity);
		if (EditorGUI.EndChangeCheck ()) {
			Undo.RecordObject (target, "Move Dynamic region max");
			t.max = pos;
		}
	}
}

