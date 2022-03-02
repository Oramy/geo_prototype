using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

// Tagging a class with the EditorTool attribute and no target type registers a global tool. Global tools are valid for any selection, and are accessible through the top left toolbar in the editor.
[EditorTool("Piece Tool", typeof(Piece))]
class PieceTool : EditorTool
{
	// Serialize this value to set a default value in the Inspector.
	[SerializeField]
	Texture2D m_ToolIcon;

	GUIContent m_IconContent;

	Quaternion oldRotation;
	void OnEnable()
	{
		m_IconContent = new GUIContent()
		{
			image = m_ToolIcon,
			text = "Piece Tool",
			tooltip = "Piece Tool"
		};

		oldRotation = Quaternion.identity;
		Selection.selectionChanged += OnSelectionChanged;
	}

	void OnDisable()
	{
		Selection.selectionChanged -= OnSelectionChanged;
	}

	public override GUIContent toolbarIcon
	{
		get { return m_IconContent; }
	}

	void TransformSelectionToRoot()
	{
		foreach (var transform in Selection.transforms)
		{
			Piece piece = transform.GetComponent<Piece>();
			piece.transformToRoot();
		}
	}

	void OnSelectionChanged()
	{
		if (ToolManager.IsActiveTool(this))
		{
			TransformSelectionToRoot();
		}
	}

	// This is called for each window that your tool is active in. Put the functionality of your tool here.
	public override void OnToolGUI(EditorWindow window)
	{
		EditorGUI.BeginChangeCheck();

		Vector3 position = Tools.handlePosition;
		Quaternion rotation = oldRotation;

		float angleSnap = 0f;
		if(Selection.transforms.Length > 0)
		{
			Piece piece = Selection.transforms[0].GetComponent<Piece>();
			piece.transformToRoot();
		}

		using (new Handles.DrawingScope(Color.green))
		{
			position = Handles.Slider(position, Vector3.right);
			rotation = Handles.Disc(Quaternion.identity, position, Vector3.forward, 1f, false, angleSnap);
		}

		if (EditorGUI.EndChangeCheck())
		{
			Vector3 delta = position - Tools.handlePosition;
			Quaternion deltaRotation = rotation * Quaternion.Inverse(oldRotation);
			Undo.RecordObjects(Selection.transforms, "Move Platform");


			int i = 0;
			foreach (var transform in Selection.transforms)
			{
				transform.position += delta;
				transform.rotation *= deltaRotation;
				i++;
			}

			oldRotation = rotation;
		}
	}
}
