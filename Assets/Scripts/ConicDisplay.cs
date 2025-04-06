using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ConicDisplay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.LogFormat("In start");

        L1_Line.positionCount = 2;
        L2_Line.positionCount = 2;
        L3_Line.positionCount = 2;

        if (Ratio_Line != null)
        {
            Ratio_Line.positionCount = 2;
        }

        Conic_Curve1.positionCount = _longPosBuffer.Length;
        Conic_Curve2.positionCount = _longPosBuffer.Length;

        if (Ratio_Line != null)
        {
            Ratio_Line.positionCount = 2;
            Ratio_Line.enabled = false;
        }

        MeshRenderer dbgPointMesh = DbgPoint.GetComponent<MeshRenderer>();
        if (dbgPointMesh != null)
        {
            dbgPointMesh.enabled = false;
        }

        UpdateControlLines();
        UpdateConic();
    }

    void Awake()
    {
        _controlPointLayer = LayerMask.GetMask("ControlPoints");
        _planeLayer = LayerMask.GetMask("Plane");
        _defaultCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !_inStringsAnim)
        {
            Transform obj = FindHitObject();
            if (obj != null)
            {
                StartDrag(obj);
            }
        }
        else
        {
            if (_draggedObject != null && !_inStringsAnim)
            {
                Drag();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishDrag();
        }

        if (_inStringsAnim)
        {
            AdvanceAnim(false);
        }
    }

    private Transform FindHitObject()
    {
        Ray r = _defaultCamera.ScreenPointToRay(Input.mousePosition);
        int numHits = Physics.RaycastNonAlloc(r, _raycastHits, 1000f, _controlPointLayer);
        for (int ii = 0; ii < numHits; ++ii)
        {
            if (_raycastHits[ii].collider.transform == T_Point)
            {
                return T_Point;
            }
            else if (_raycastHits[ii].collider.transform == P1_Point)
            {
                return P1_Point;
            }
            else if (_raycastHits[ii].collider.transform == P2_Point)
            {
                return P2_Point;
            }
            else if (_raycastHits[ii].collider.transform == ExtraPoint)
            {
                return ExtraPoint;
            }
        }

        return null;
    }

    private void StartDrag(Transform draggedObject)
    {
        _draggedObject = draggedObject;
    }

    private void Drag()
    {
        Ray r = _defaultCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(r, out hit, 1000f, _planeLayer))
        {
            _draggedObject.position = new Vector3(hit.point.x, 0f, hit .point.z);
            UpdateControlLines();
            UpdateConic();
        }
    }

    private void FinishDrag()
    {
        _draggedObject = null;
    }

    private void UpdateControlLines()
    {
        _posBuffer[0] = T_Point.position;
        _posBuffer[1] = P1_Point.position;
        L1_Line.SetPositions(_posBuffer);

        _posBuffer[0] = T_Point.position;
        _posBuffer[1] = P2_Point.position;
        L2_Line.SetPositions(_posBuffer);

        _posBuffer[0] = P1_Point.position;
        _posBuffer[1] = P2_Point.position;
        L3_Line.SetPositions(_posBuffer);
    }

    private void UpdateConic()
    {
        _innerConic.TPoint = new Vector2(T_Point.position.x, T_Point.position.z);
        _innerConic.P1Point = new Vector2(P1_Point.position.x, P1_Point.position.z);
        _innerConic.P2Point = new Vector2(P2_Point.position.x, P2_Point.position.z);
        _innerConic.ExtraPoint = new Vector2(ExtraPoint.position.x, ExtraPoint.position.z);
         _innerConic.Recompute();

        float step = 0.1f;

        for (int ii = 0; ii < _longPosBuffer.Length; ++ii)
        {
            float tt = ii * step + math.EPSILON;
            Vector2 pnt = _innerConic.EvaluateBasic(tt);
            _longPosBuffer[ii] = new Vector3(pnt.x, 0f, pnt.y);
        }
        Conic_Curve1.SetPositions(_longPosBuffer);

        for (int ii = 0; ii < _longPosBuffer.Length; ++ii)
        {
            float tt = -(ii * step + math.EPSILON);
            Vector2 pnt = _innerConic.EvaluateBasic(tt);
            _longPosBuffer[ii] = new Vector3(pnt.x, 0f, pnt.y);
        }

        Conic_Curve2.SetPositions(_longPosBuffer);
    }

    public void AnimateStrings()
    {
        if (_inStringsAnim == false)
        {
            _inStringsAnim = true;
            Ratio_Line.enabled = true;
            MeshRenderer dbgPointMesh = DbgPoint.GetComponent<MeshRenderer>();
            if (dbgPointMesh != null)
            {
                dbgPointMesh.enabled = true;
            }
            _animStepsLeft = _maxAnimSteps;
            AdvanceAnim(true);
        }
    }

    private void AdvanceAnim(bool init)
    {
        if (!init && (Time.time - _lastAnimStep < _animStepTime))
        {
            return;
        }

        _lastAnimStep = Time.time;

        int ii = _maxAnimSteps - _animStepsLeft;
        float tt = (float)ii + math.EPSILON;
        (Vector2, Vector2, Vector2) pnts = _innerConic.EvaluateWithRatioLine(tt);
        _posBuffer[0] = new Vector3(pnts.Item2.x, 0f, pnts.Item2.y);
        _posBuffer[1] = new Vector3(pnts.Item3.x, 0f, pnts.Item3.y);
        Ratio_Line.SetPositions(_posBuffer);

        DbgPoint.position = new Vector3(pnts.Item1.x, 0f, pnts.Item1.y);

        if (!init)
        {
            --_animStepsLeft;
            if (_animStepsLeft < 0)
            {
                _inStringsAnim = false;
                Ratio_Line.enabled = false;
                MeshRenderer dbgPointMesh = DbgPoint.GetComponent<MeshRenderer>();
                if (dbgPointMesh != null)
                {
                    dbgPointMesh.enabled = false;
                }
            }
        }
    }

    public Transform T_Point, P1_Point, P2_Point, ExtraPoint, DbgPoint;

    public LineRenderer L1_Line, L2_Line, L3_Line, Conic_Curve1, Conic_Curve2, Ratio_Line;

    private ConicSection _innerConic = new ConicSection();

    private Transform _draggedObject = null;

    private bool _inStringsAnim = false;
    private int _animStepsLeft = 100;
    private float _lastAnimStep;
    private readonly int _maxAnimSteps = 100;
    private readonly float _animStepTime = 0.1f;

    private RaycastHit[] _raycastHits = new RaycastHit[256];

    private Vector3[] _posBuffer = new Vector3[2];
    private Vector3[] _longPosBuffer = new Vector3[1001];

    private Camera _defaultCamera = null;

    private static int _controlPointLayer;
    private static int _planeLayer;
}
