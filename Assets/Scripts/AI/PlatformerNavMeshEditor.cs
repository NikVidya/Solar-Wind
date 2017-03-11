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
		serializedObject.Update ();
		PlatformerNavMesh t = (target as PlatformerNavMesh);

		GUILayout.Label ("Nav Mesh Area");
		EditorGUI.indentLevel++;
		t.bbMin = EditorGUILayout.Vector2Field ("Bounding Box Min", t.bbMin);
		t.bbMax = EditorGUILayout.Vector2Field ("Bounding Box Max", t.bbMax);
		EditorGUI.indentLevel--;

		EditorGUILayout.Space ();
		GUILayout.Label ("Nav Mesh Properties");
		EditorGUILayout.PropertyField (serializedObject.FindProperty("gridSize"));

		if (t.isGeneratingNavMesh) {
			GUI.enabled = false;
		} else {
			GUI.enabled = true;
		}
		if (GUILayout.Button ("Re-Build Nav Mesh")) {
			if ( !t.isGeneratingNavMesh ) {
				t.StartBuilding ();
			}
		}
		GUI.enabled = true;

		serializedObject.ApplyModifiedProperties ();
	}

	public void OnSceneGUI()
	{
		var t = (target as PlatformerNavMesh);

		EditorGUI.BeginChangeCheck ();
		Vector3 pos = Handles.PositionHandle (t.bbMin, Quaternion.identity);
		if (EditorGUI.EndChangeCheck ()) {
			Undo.RecordObject (target, "Move Nav Mesh Min Bounding Box");
			t.bbMin = pos;
		}

		EditorGUI.BeginChangeCheck ();
		pos = Handles.PositionHandle (t.bbMax, Quaternion.identity);
		if (EditorGUI.EndChangeCheck ()) {
			Undo.RecordObject (target, "Move Nav Mesh Max Bounding Box");
			t.bbMax = pos;
		}
	}
}

