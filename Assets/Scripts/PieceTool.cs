using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

// Tagging a class with the EditorTool attribute and no target type registers a global tool. Global tools are valid for any selection, and are accessible through the top left toolbar in the editor.
[EditorTool("Piece Tool")]
class PieceTool : EditorTool
{
	// Serialize this value to set a default value in the Inspector.
	[SerializeField]
	Texture2D m_ToolIcon;

	GUIContent m_IconContent;

	void OnEnable()
	{
		m_IconContent = new GUIContent()
		{
			image = m_ToolIcon,
			text = "Piece Tool",
			tooltip = "Piece Tool"
		};

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

	public void AttachmentUI()
	{
		Handles.BeginGUI();

		Camera sceneCam = SceneView.currentDrawingSceneView.camera;
		foreach (var transform in Selection.transforms)
		{
			Piece piece = transform.GetComponent<Piece>();

			foreach (Piece.AttachedPoint ap in piece.attachPoints)
			{
				Transform attachPoint = ap.point;
				Vector3 screenPos = sceneCam.WorldToScreenPoint(attachPoint.position);
				screenPos.y = sceneCam.pixelHeight - screenPos.y;
				screenPos /= Utils.GetPixelsPerPoint();
				Rect r = new Rect(screenPos.x - 10, screenPos.y - 10, 20, 20);

				GUILayout.BeginArea(r);
				ap.enabled = GUILayout.Toggle(ap.enabled, "");
				GUILayout.EndArea();

				if (!ap.enabled && ap.piece != null)
				{
					piece.unAttach(ap);
				}
			}
		}

		Handles.EndGUI();
	}

	public void MoveHandle() {
		EditorGUI.BeginChangeCheck();

		Vector3 position = Tools.handlePosition;
		using (new Handles.DrawingScope(Color.green))
		{
			position = Handles.Slider2D(position, Vector3.forward, Vector3.up, Vector3.right, 0.1f, Handles.RectangleHandleCap, 0f);
		}

		if (EditorGUI.EndChangeCheck())
		{
			Vector3 delta = position - Tools.handlePosition;
			Undo.RecordObjects(Selection.transforms, "Move Piece");

			foreach (var transform in Selection.transforms)
			{
				transform.position += delta;
				Piece piece = transform.GetComponent<Piece>();
				piece.OnTransformChanged();
			}
		}

	}


	// This is called for each window that your tool is active in. Put the functionality of your tool here.
	public override void OnToolGUI(EditorWindow window)
	{
		foreach (var transform in Selection.transforms)
		{
			if (transform.GetComponent<Piece>() == null)
				return;
		}

		AttachmentUI();
		MoveHandle();
		foreach (var transform in Selection.transforms)
		{

			Piece piece = transform.GetComponent<Piece>();
			if (piece != null)
				piece.RotationHandle();
		}
	}
}
