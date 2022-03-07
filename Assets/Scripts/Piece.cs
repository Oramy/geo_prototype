using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Piece : MonoBehaviour
{
	private static int MAX_COLLIDER_TEST = 5;
	private static float attachDistance = 0.05f;
	private static ContactFilter2D NO_FILTER = new ContactFilter2D().NoFilter();

	[SerializeField]
	private int vertices;

	[Serializable]
	public class AttachedPoint
	{
		public Transform point;
		public bool enabled;
		public Piece piece;
	}

	[SerializeField]
	public AttachedPoint[] attachPoints;

	[SerializeField]
	private long id = 0;

	[SerializeField]
	public bool canAttach = true;

	Quaternion oldRotation;

	public void OnEnable()
	{
		oldRotation = Quaternion.identity;
	}

	public float GetRotationSymmetryAngle()
	{

		return 360f / vertices;
	}

	public static Piece GetRootPiece(Piece p)
	{
		Transform root = p.transform;
		while (root.parent != null && root.parent.GetComponent<Piece>() != null)
		{
			root = root.parent;
		}

		Piece piece = root.GetComponent<Piece>();

		return piece;
	}

	public void TransformToRoot() {
		Piece rootPiece = GetRootPiece(this);
		transform.SetParent(rootPiece.transform.parent, true);
		SetAsChild(rootPiece);
	}

	public void UpdateToNewID()
	{
		id = PieceManager.instance.GetNewID();
	}

	public void UnRoot() {
		Transform oldParent = transform.parent;
		if (oldParent == null)
			return;
		Piece piece = oldParent.GetComponent<Piece>();
		if (piece == null)
			return;
		if (oldParent != null)
		{
			transform.parent = oldParent.parent;
			UpdateToNewID();
		}
	}

	public void UpdateParenting() {
		this.UnRoot();

		Queue<Piece> pieces = new Queue<Piece>();
		List<Piece> seen = new List<Piece>();

		foreach (var ap in attachPoints)
		{
			if(ap.piece != null)
				pieces.Enqueue(ap.piece);
		}

		while (pieces.Count > 0)
		{
			Piece piece = pieces.Dequeue();
			piece.transform.SetParent(transform, true);
			piece.id = this.id;

			foreach (var ap in piece.attachPoints)
			{
				if (!seen.Contains(ap.piece) && ap.piece != null) {
					seen.Add(ap.piece);
					pieces.Enqueue(ap.piece);
				}
			}
		}
		
	}

	public void UnAttach(Piece piece) {
		foreach (var ap in attachPoints)
		{
			if (ap.piece == piece)
			{
				ap.piece = null;
			}
		}
	}

	public void UnAttach(AttachedPoint ap) {
		if (ap == null)
			return;
		if (ap.piece == null)
		{

			Debug.LogWarning($"Tried to unattach point {ap} but none were attached");
			return;
		}

		ap.piece.UnAttach(this);

		this.UnRoot();
		ap.piece.UnRoot();
		this.UpdateParenting();
		ap.piece.UpdateParenting();
		ap.piece = null;
	}

	public void SetAsChild(Piece piece)
	{
		Transform tr = piece.transform;
		tr.SetParent(transform, true);

		foreach (Transform child in tr)
		{
			if(child.GetComponent<Piece>() != null)
				child.SetParent(transform, true);
		}

		this.id = piece.id;
		SetChildIDs();
	}

	private void SetChildIDs()
	{
		foreach (Transform child in transform)
		{
			Piece childPiece = child.GetComponent<Piece>();
			if(childPiece != null)
				childPiece.id = this.id;
		}
	}

	public AttachedPoint GetAttachPoint(Transform tr)
	{
		foreach (AttachedPoint ap in attachPoints)
		{
			if (ap.point == tr)
				return ap;
		}
		return null;
	}

	public void Update()
	{
		if (id == 0 && !PrefabUtility.IsPartOfPrefabAsset(this))
			UpdateToNewID();
	}

	public void OnTransformChanged()
	{
		Collider2D[] colliders = new Collider2D[MAX_COLLIDER_TEST];
		foreach (AttachedPoint ap in attachPoints)
		{
			if (!ap.enabled && ap.point != null)
				continue;
			Transform point = ap.point;
			Vector2 pos2 = new Vector2(point.position.x, point.position.y);
			int resultCount = Physics2D.OverlapCircle(pos2, attachDistance, NO_FILTER, colliders);
			for (int i = 0; i < resultCount; i++)
			{
				Collider2D col = colliders[i];
				Rigidbody2D rb2D = col.attachedRigidbody;
				if(rb2D != null)
				{
					Piece piece = rb2D.GetComponent<Piece>();
					AttachedPoint otherAttachPoint = piece.GetAttachPoint(col.transform);
					float dot = Vector3.Dot(ap.point.up, otherAttachPoint.point.up);
					if (piece != null && piece != this
						&& this.canAttach && piece.canAttach
						&& otherAttachPoint.enabled && otherAttachPoint.piece == null
						&& dot < -0.95f)
					{
						Piece rootPiece = GetRootPiece(piece);

						if (rootPiece.id != this.id)
						{
							ap.piece = piece;
							otherAttachPoint.piece = this;
							Vector3 delta = point.position - col.bounds.center;
							rootPiece.transform.Translate(delta, Space.World);
							SetAsChild(rootPiece);
						}
					}
				}
			}
			
		}
	}

	public void OnDrawGizmos() {
		RotationHandle();
		foreach (AttachedPoint ap in attachPoints)
		{
			Transform point = ap.point;
			Gizmos.color = ap.enabled ? Color.green : Color.red;
			Gizmos.DrawSphere(point.position, attachDistance);
			Gizmos.DrawIcon(point.position, "link.png");
		}	
	}

	public void RotationHandle()
	{
		EditorGUI.BeginChangeCheck();
		Vector3 position = Tools.handlePosition;
		Quaternion rotation = oldRotation;

		float angleSnap = GetRotationSymmetryAngle();

		using (new Handles.DrawingScope(Color.red))
		{
			rotation = Handles.Disc(Quaternion.identity, position, Vector3.forward, 1f, false, angleSnap);
		}

		if (EditorGUI.EndChangeCheck())
		{
			this.TransformToRoot();
			Quaternion deltaRotation = rotation * Quaternion.Inverse(oldRotation);
			Undo.RecordObjects(Selection.transforms, "Rotate Piece");


			foreach (var transform in Selection.transforms)
			{
				transform.rotation *= deltaRotation;
				Piece piece = transform.GetComponent<Piece>();
				piece.OnTransformChanged();
			}

			oldRotation = rotation;
		}
	}
}
