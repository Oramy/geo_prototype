﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Piece))]
[CanEditMultipleObjects]
public class PieceEditor : Editor
{

	void OnEnable()
	{
	}

	public override void OnInspectorGUI()
	{
		Piece piece = (Piece)target;
		DrawDefaultInspector();

		if (GUILayout.Button("SetToRoot"))
		{
			piece.transformToRoot();
		}
		if (GUILayout.Button("UnRoot"))
		{
			piece.unRoot();
		}
		if (GUILayout.Button("Create New ID"))
		{
			piece.updateToNewID();
		}
	}
}
