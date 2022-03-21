using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PuzzleController : MonoBehaviour
{
    private enum ControllerState
    {
        Idle,
        Translation,
        Zoom,
        InitRotatePiece,
        RotatePiece
    }

    private ControllerState controllerState;

    [SerializeField] private InputActions inputActions;

    private Vector2 startPinchPos1;
    private Vector2 startPinchPos2;
    private float startPinchDistance;
    private float startOrthographicSize;

    private Vector2 startTouchPos;
    private Vector3 startCameraPos;

    [SerializeField] private Camera orthoCamera;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2f;
    [SerializeField] private float mouseScrollSensivity = 0.01f;

    private float minOrthoSize;
    private float maxOrthoSize;

    private Vector3 translateTargetPosition;
    [SerializeField] private float translateOneSecondDecay = 0.1f;
    [SerializeField] private float translateMinDistance = 0.01f;

    private Piece selectedPiece;
    private Vector3 startRotatePiecePosition;
    private float rotatePieceDeltaAngle;
    private float startRotatePieceRotation;
    [SerializeField] private float rotationSnapAngle = 10;
    [SerializeField] private float minRotatePieceDistance = 0.1f;

#if UNITY_EDITOR
    private Vector3 debugFirstTouchWorldPosition;
    private Vector3 debugRotatePieceWorldPosition;
#endif

    private void Awake()
    {
        inputActions = new InputActions();
        controllerState = ControllerState.Idle;
    }

    void Start()
    {
        GetComponent<PlayerInput>().actions = inputActions.asset;
        if (orthoCamera == null)
        {
            orthoCamera = Camera.main;
        }
        startCameraPos = orthoCamera.transform.position;
        minOrthoSize = orthoCamera.orthographicSize * minZoom;
        maxOrthoSize = orthoCamera.orthographicSize * maxZoom;
    }

    // Update is called once per frame
    void Update()
    {
        switch(controllerState)
        {
            case ControllerState.Translation:
                UpdateTranslate();
                break;
            case ControllerState.InitRotatePiece:
                InitRotatePiece();
                break;
            case ControllerState.RotatePiece:
                UpdateRotatePiece();
                break;
        }
        
    }


    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Grab.started += GrabStarted;
        inputActions.Player.Grab.canceled += GrabCanceled;
    }


    private void OnDisable()
    {
        inputActions.Disable();
        inputActions.Player.Grab.started -= GrabStarted;
        inputActions.Player.Grab.canceled -= GrabCanceled;
    }

    private void OnGrab()
    {
#if DEBUG_LOG
        Debug.Log("OnGrab");
#endif
    }

    private void GrabStarted(InputAction.CallbackContext obj)
    {
        InitGrab();
    }

    private void GrabCanceled(InputAction.CallbackContext obj)
    {
        if (controllerState == ControllerState.RotatePiece)
        {
            EndRotatePiece();
        }
        controllerState = ControllerState.Idle;
    }

#if UNITY_EDITOR
    private void GrabPieceGizmos()
    {
        if(debugFirstTouchWorldPosition != null)
            Gizmos.DrawSphere(debugFirstTouchWorldPosition, 0.1f);
    }
#endif 
    private bool InitGrabPiece()
    {
        Vector2 touchWorldPosition = orthoCamera.ScreenToWorldPoint(startTouchPos);
#if UNITY_EDITOR
        debugFirstTouchWorldPosition = touchWorldPosition;
#endif
        Collider2D touchedCollider = Physics2D.OverlapPoint(touchWorldPosition,
            ~PhysicsLayerUtils.Instance.PIECE_LAYER);
        if (touchedCollider != null)
        {
#if DEBUG_LOG
                Debug.Log("Touched piece collider.");
#endif
            Transform touchedTransform = touchedCollider.transform;
            Piece piece = null;
            if (touchedTransform.TryGetComponent<Piece>(out piece))
            {
#if DEBUG_LOG
                Debug.Log("Touched piece.");
#endif
                selectedPiece = piece;
                selectedPiece.TransformToRoot();
                controllerState = ControllerState.InitRotatePiece;
                return true;
            }
        }
        
        return false;
    }

    private void InitRotatePiece()
    {
        Vector2 touchPos = inputActions.Player.FirstTouchPosition.ReadValue<Vector2>();
        Vector3 touchPosWorld = orthoCamera.ScreenToWorldPoint(touchPos);
        touchPosWorld.z = 0;
        if (Vector2.Distance(touchPosWorld, selectedPiece.transform.position) > minRotatePieceDistance)
        {
            startRotatePieceRotation = selectedPiece.transform.rotation.eulerAngles.z;
            startRotatePiecePosition = touchPosWorld;
            controllerState = ControllerState.RotatePiece;
        }
    }

    private void UpdateRotatePiece()
    {
        Vector2 touchPos = inputActions.Player.FirstTouchPosition.ReadValue<Vector2>();
        Vector3 touchPosWorld = orthoCamera.ScreenToWorldPoint(touchPos);
        touchPosWorld.z = 0;
#if UNITY_EDITOR
        debugRotatePieceWorldPosition = touchPosWorld;
#endif

        rotatePieceDeltaAngle = Vector3.SignedAngle((startRotatePiecePosition - selectedPiece.transform.position).normalized,
            (touchPosWorld - selectedPiece.transform.position).normalized, Vector3.forward);

        float rotation = startRotatePieceRotation + rotatePieceDeltaAngle;
        int validRotIndex = Mathf.RoundToInt(rotatePieceDeltaAngle / selectedPiece.GetRotationSymmetryAngle());
        float deltaValidRotation = validRotIndex * selectedPiece.GetRotationSymmetryAngle();
        float angleToValidRotation = deltaValidRotation - rotatePieceDeltaAngle;

        if (Mathf.Abs(angleToValidRotation) < rotationSnapAngle)
        {
            rotation += angleToValidRotation;
            selectedPiece.OnTransformChanged();
        }
        
        selectedPiece.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.forward);
    }

    private void EndRotatePiece()
    {
        int validRotIndex = Mathf.RoundToInt(rotatePieceDeltaAngle / selectedPiece.GetRotationSymmetryAngle());
        float validRotation = validRotIndex * selectedPiece.GetRotationSymmetryAngle();
        selectedPiece.transform.rotation = Quaternion.AngleAxis(startRotatePieceRotation + validRotation, Vector3.forward);
        selectedPiece.OnTransformChanged();
    }

#if UNITY_EDITOR
    private void RotatePieceGizmos()
    {
        if (selectedPiece == null || (controllerState != ControllerState.InitRotatePiece && controllerState != ControllerState.RotatePiece))
            return;
        if (debugRotatePieceWorldPosition != null)
        {
            Gizmos.color = Color.red; 
            Gizmos.DrawLine(selectedPiece.transform.position, debugRotatePieceWorldPosition);
        }
        if (startRotatePiecePosition != null)
        {
            Gizmos.color = Color.green; 
            Gizmos.DrawLine(selectedPiece.transform.position, startRotatePiecePosition);
        }
    }
#endif 

    private void InitGrab()
    {
        startTouchPos = inputActions.Player.FirstTouchPosition.ReadValue<Vector2>();
        startCameraPos = orthoCamera.transform.position;

        if (InitGrabPiece())
            return;

        controllerState = ControllerState.Translation;
    }

#region CameraTranslation
    private void UpdateTranslate()
    {
        if (translateTargetPosition == null || controllerState != ControllerState.Translation)
            return;
        if (Vector3.Distance(translateTargetPosition, orthoCamera.transform.position)
            > translateMinDistance)
        {
            float t = 1f - Mathf.Pow(translateOneSecondDecay, Time.deltaTime);
            Transform camTransform = orthoCamera.transform;
            camTransform.position = Vector3.Lerp(camTransform.position, translateTargetPosition, t);
        }
    }

    private void OnDrag()
    {
        if (!inputActions.Player.SecondTouch.IsPressed()
            && controllerState == ControllerState.Translation)
        {
            Vector2 touchPos = inputActions.Player.FirstTouchPosition.ReadValue<Vector2>();
            Vector3 worldStartTouchPos = orthoCamera.ScreenToWorldPoint(startTouchPos);
            Vector3 worldTouchPos = orthoCamera.ScreenToWorldPoint(touchPos);
            Vector3 deltaPos = worldTouchPos - worldStartTouchPos;
            translateTargetPosition = startCameraPos - deltaPos;
        }
        else if (controllerState == ControllerState.Idle){
            InitGrab();
        }
    }
#endregion
#region CameraZoom

    private void OnMouseScroll()
    {
#if DEBUG_LOG
        Debug.Log("OnMouseScroll");
#endif
        Vector2 mousePosition = inputActions.Player.MousePosition.ReadValue<Vector2>();
        Vector3 mousePositionInWorld = orthoCamera.ScreenToWorldPoint(mousePosition);
        float deltaZoom = inputActions.Player.MouseScroll.ReadValue<float>() * mouseScrollSensivity;

        Vector3 zoomTowards = new Vector3(mousePosition.x, mousePosition.y, 0);
        ZoomOrthoCamera(mousePositionInWorld, -deltaZoom);
    }

    private void StartPinch()
    {
        startPinchPos1 = inputActions.Player.FirstTouchPosition.ReadValue<Vector2>();
        startPinchPos2 = inputActions.Player.SecondTouchPosition.ReadValue<Vector2>();
        startPinchDistance = Vector2.Distance(startPinchPos1, startPinchPos2);
        startOrthographicSize = orthoCamera.orthographicSize;
    }

    private void OnSecondTouch()
    {
#if DEBUG_LOG
        Debug.Log("OnSecondTouch");
#endif
        StartPinch();
    }

    private void ZoomOrthoCamera(Vector3 zoomTowards, float amount)
    {
        if (!orthoCamera.orthographic)
        {
            Debug.LogError("Cannot use ZoomOrthoCamera on a perspective camera.");
            return;
        }

        float oldOrthoSize = orthoCamera.orthographicSize;

        // Zoom camera
        orthoCamera.orthographicSize += amount;

        // Limit zoom
        orthoCamera.orthographicSize = Mathf.Clamp(orthoCamera.orthographicSize, minOrthoSize, maxOrthoSize);

        float realAmount = oldOrthoSize - orthoCamera.orthographicSize;
        // Calculate how much we will have to move towards the zoomTowards position
        float multiplier = (1.0f / oldOrthoSize * realAmount);

        // Move camera
        orthoCamera.transform.position += (zoomTowards - orthoCamera.transform.position) * multiplier;
    }

    private void OnTwoTouchDrag()
    {
        if (!inputActions.Player.SecondTouch.IsPressed())
            return;
#if DEBUG_LOG
        Debug.Log("OnTwoTouchDrag");
#endif
        if (startPinchPos1 == null)
            StartPinch();

        Vector2 firstPosition = inputActions.Player.FirstTouchPosition.ReadValue<Vector2>();
        Vector2 secondPosition = inputActions.Player.SecondTouchPosition.ReadValue<Vector2>();
        Vector2 middlePosition = (firstPosition + secondPosition) / 2f;
        float distance = Vector2.Distance(firstPosition, secondPosition);
        float targetOrthoSize = startPinchDistance / distance * startOrthographicSize;
        float zoom = (targetOrthoSize - orthoCamera.orthographicSize) * Time.deltaTime;

        Vector3 zoomTowards = new Vector3(middlePosition.x, middlePosition.y, 0);
        Vector3 zoomTowardsInWorld = orthoCamera.ScreenToWorldPoint(zoomTowards);
        ZoomOrthoCamera(zoomTowardsInWorld, zoom);
    }

#endregion


    private void OnDoubleTap()
    { 
#if DEBUG_LOG
        Debug.Log("OnDoubleTap");
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        GrabPieceGizmos();
        RotatePieceGizmos();
    }
#endif
}
