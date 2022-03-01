using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
	[SerializeField]
	private int vertices;

	public float getRotationSymmetryAngle()
	{
		return 360f / vertices;
	}

	public void transformToRoot() {
		Transform oldParent = transform.parent;
		if (oldParent != null)
		{
			transform.parent = oldParent.parent;
			oldParent.parent = transform;

			foreach (Transform child in oldParent)
			{
				child.parent = transform;
			}
		}
	}
}
