using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
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

    private static ContactFilter2D _ATTACH_POINT_FILTER;
	public static ContactFilter2D ATTACH_POINT_FILTER
    {
        get
        {
            if (_ATTACH_POINT_FILTER.layerMask.value != 0)
            {
                return _ATTACH_POINT_FILTER;
            }
            _ATTACH_POINT_FILTER.SetLayerMask(1 << LayerMask.NameToLayer("AttachPoint"));
            return _ATTACH_POINT_FILTER;
        }
    }

    public void Awake()
    {
#if !UNITY_EDITOR
        DontDestroyOnLoad(this);
#endif
        if (_instance != null)
            DestroyImmediate(this);
        else
            _instance = this;

        _PIECE_LAYER = LayerMask.NameToLayer("Piece");
    }
}
