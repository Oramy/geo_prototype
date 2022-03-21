using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsLayerUtils : MonoBehaviour
{
    private static PhysicsLayerUtils _instance;

    public static PhysicsLayerUtils Instance
    {
        get {
            return _instance;
        }
    }

    private int _PIECE_LAYER;
    public int PIECE_LAYER { get { return _PIECE_LAYER; } }

    private ContactFilter2D _ATTACH_POINT_FILTER;
	public ContactFilter2D ATTACH_POINT_FILTER
    {
        get
        {
            return _ATTACH_POINT_FILTER;
        }
    }

    public void Awake()
    {
        DontDestroyOnLoad(this);
        if (_instance != null)
            DestroyImmediate(this);
        else
            _instance = this;

        _ATTACH_POINT_FILTER.SetLayerMask(LayerMask.NameToLayer("AttachPoint"));
        _PIECE_LAYER = LayerMask.NameToLayer("Piece");
    }
}
