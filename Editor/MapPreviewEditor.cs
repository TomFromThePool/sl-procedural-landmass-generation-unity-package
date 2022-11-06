using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (MapPreview))]
public class MapPreviewEditor : Editor {

	public override void OnInspectorGUI() {
		MapPreview mapPreview = (MapPreview)target;

		if (DrawDefaultInspector ()) {
			if (mapPreview.autoUpdate) {
				mapPreview.DrawMapInEditor ();
			}
		}

		if (GUILayout.Button ("Generate")) {
			mapPreview.DrawMapInEditor ();
		}

		EditorGUI.BeginDisabledGroup(mapPreview.drawMode != MapPreview.DrawMode.Live);
		if (GUILayout.Button("Clear"))
		{
			mapPreview.Generator.Clear();
		}
		EditorGUI.EndDisabledGroup();
	}
}
