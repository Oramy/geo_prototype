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

	public float getRotationSymmetryAngle()
	{

		return 360f / vertices;
	}

	public static Piece getRootPiece(Piece p)
	{
		Transform root = p.transform;
		while (root.parent != null && root.parent.GetComponent<Piece>() != null)
		{
			root = root.parent;
		}

		Piece piece = root.GetComponent<Piece>();

		return piece;
	}

	public void transformToRoot() {
		Piece rootPiece = getRootPiece(this);
		transform.SetParent(rootPiece.transform.parent, true);
		setAsChild(rootPiece);
	}

	public void updateToNewID()
	{
		id = PieceManager.instance.getNewID();
	}

	public void unRoot() {
		Transform oldParent = transform.parent;
		if (oldParent == null)
			return;
		Piece piece = oldParent.GetComponent<Piece>();
		if (piece == null)
			return;
		if (oldParent != null)
		{
			transform.parent = oldParent.parent;
			updateToNewID();
		}
	}

	public void updateParenting() {
		this.unRoot();

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

	public void unAttach(Piece piece) {
		foreach (var ap in attachPoints)
		{
			if (ap.piece == piece)
			{
				ap.piece = null;
			}
		}
	}

	public void unAttach(AttachedPoint ap) {
		if (ap == null)
			return;
		if (ap.piece == null)
		{

			Debug.LogWarning($"Tried to unattach point {ap} but none were attached");
			return;
		}

		ap.piece.unAttach(this);

		this.unRoot();
		ap.piece.unRoot();
		this.updateParenting();
		ap.piece.updateParenting();
		ap.piece = null;
	}

	public void setAsChild(Piece piece)
	{
		Transform tr = piece.transform;
		tr.SetParent(transform, true);

		foreach (Transform child in tr)
		{
			if(child.GetComponent<Piece>() != null)
				child.SetParent(transform, true);
		}

		this.id = piece.id;
		setChildIDs();
	}

	private void setChildIDs()
	{
		foreach (Transform child in transform)
		{
			Piece childPiece = child.GetComponent<Piece>();
			if(childPiece != null)
				childPiece.id = this.id;
		}
	}

	public AttachedPoint getAttachPoint(Transform tr)
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
			updateToNewID();
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
					AttachedPoint otherAttachPoint = piece.getAttachPoint(col.transform);
					float dot = Vector3.Dot(ap.point.up, otherAttachPoint.point.up);
					if (piece != null && piece != this
						&& this.canAttach && piece.canAttach
						&& otherAttachPoint.enabled && otherAttachPoint.piece == null
						&& dot < -0.95f)
					{
						Piece rootPiece = getRootPiece(piece);

						if (rootPiece.id != this.id)
						{
							ap.piece = piece;
							otherAttachPoint.piece = this;
							Vector3 delta = point.position - col.bounds.center;
							rootPiece.transform.Translate(delta, Space.World);
							setAsChild(rootPiece);
						}
					}
				}
			}
			
		}
	}

	public void checkAttachmentWithPiece() {

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

		float angleSnap = getRotationSymmetryAngle();

		using (new Handles.DrawingScope(Color.red))
		{
			rotation = Handles.Disc(Quaternion.identity, position, Vector3.forward, 1f, false, angleSnap);
		}

		if (EditorGUI.EndChangeCheck())
		{
			this.transformToRoot();
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
