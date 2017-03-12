using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(PlatformerNavMesh))]
public class PlatformerNavMeshEditor : Editor
{

	void OnEnable()
	{
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector ();
	}

	public void OnSceneGUI()
	{
		var t = (target as PlatformerNavMesh);

		EditorGUI.BeginChangeCheck ();
		Vector3 pos = Handles.PositionHandle (t.bbMin, Quaternion.identity);
		if (EditorGUI.EndChangeCheck ()) {
			Undo.RecordObject (target, "Move Nav Mesh Min Bounding Box");
			t.bbMin = pos;
			t.ClearNav ();
		}

		EditorGUI.BeginChangeCheck ();
		pos = Handles.PositionHandle (t.bbMax, Quaternion.identity);
		if (EditorGUI.EndChangeCheck ()) {
			Undo.RecordObject (target, "Move Nav Mesh Max Bounding Box");
			t.bbMax = pos;
			t.ClearNav ();
		}
	}
}

