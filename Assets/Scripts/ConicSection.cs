using System.Drawing;
using System.Security.Cryptography;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

/* Used for debugging only */
public struct PlanarLine
{
    public PlanarLine(Vector2 point1, Vector2 point2)
    {
        _p1 = new Vector3(point1.x, point1.y, 0f);
        _p2 = new Vector3(point2.x, point2.y, 0f);
    }

    public float Apply(Vector2 point)
    {
        return Vector3.Cross(new Vector3(point.x,point.y, 0f) - _p1, _p2 - _p1).z;
    }

    public Vector3 _p1, _p2;
}

/* Actually used for the computation */
public struct ConicUVCoordSys
{
    public ConicUVCoordSys(Vector2 _t, Vector2 _p1, Vector2 _p2)
    {
        TPoint = _t;
        P1Point = _p1;
        P2Point = _p2;

        Vector2 L1Vec = _p1 - _t;
        Vector2 L2Vec = _p2 - _t;

        Mat.Item1 = new Vector2(L1Vec.x, L2Vec.x);
        Mat.Item2 = new Vector2(L1Vec.y, L2Vec.y);

        float det = L1Vec.x * L2Vec.y - L2Vec.x * L1Vec.y;
        InvMat.Item1 = new Vector2(L2Vec.y, -L2Vec.x) / det;
        InvMat.Item2 = new Vector2(-L1Vec.y, L1Vec.x) / det;
    }

    public Vector2 XYToUV(Vector2 xy)
    {
        Vector2 fromT = xy - TPoint;
        return new Vector2(Vector2.Dot(InvMat.Item1, fromT), Vector2.Dot(InvMat.Item2, fromT));
    }

    public Vector2 UVToXY(Vector2 uv)
    {
        return TPoint + new Vector2(Vector2.Dot(Mat.Item1, uv), Vector2.Dot(Mat.Item2, uv));
    }

    private Vector2 TPoint, P1Point, P2Point;
    private (Vector2, Vector2) Mat, InvMat;
}

public class ConicSection
{
    public void Recompute()
    {
        UVCoverter = new ConicUVCoordSys(TPoint, P1Point, P2Point);

        Vector2 extraUV = UVCoverter.XYToUV(ExtraPoint);

        CC = -(extraUV.x * extraUV.y) / math.square(extraUV.x + extraUV.y - 1f);

        //Debug.LogFormat("u={0}, v={1} u+v={2} C={3}", extraUV.x, extraUV.y, extraUV.x + extraUV.y, CC);

        if (CC >= 0f)
        {
            w1 = 2.0f * math.sqrt(CC);
            w2 = -w1;
        }
        else
        {
            w1 = w2 = 2.0f * math.sqrt(-CC);
        }
    }

    public Vector2 Evaluate(float tt)
    {
        if (math.abs(MaxT - tt) < math.EPSILON)
        {
            return P2Point;
        }
        else if (math.abs(tt - MinT) < math.EPSILON)
        {
            return P1Point;
        }
        else
        {
            float r1 = w1 * (tt - MinT) / (MaxT - tt), r2 = w2 * (MaxT - tt) / (tt - MinT);

            Vector2 res = (r1 * P1Point + 2f * TPoint + r2 * P2Point) / (r1 + r2 + 2f);

            /* DEBUG: verify that the point satisfies the conic equation */
            /*
            {
                PlanarLine l1 = new PlanarLine(P1Point, TPoint);
                PlanarLine l2 = new PlanarLine(P2Point, TPoint);
                PlanarLine l3 = new PlanarLine(P2Point, P1Point);
                float check = l1.Apply(res) * l2.Apply(res) + CC * math.square(l3.Apply(res));
                if (math.abs(check) > math.EPSILON)
                {
                    Debug.LogWarningFormat("Check failed: {0}", check);
                }
            }
            */

            return res;
        }
    }

    public Vector2 EvaluateBasic(float tt)
    {
        float r1 = tt, r2 = -4f * CC / tt;

        Vector2 res = (r1 * P1Point + 2f * TPoint + r2 * P2Point) / (r1 + r2 + 2f);

        /* DEBUG: verify that the point satisfies the conic equation */
        /*
        PlanarLine l1 = new PlanarLine(P1Point, TPoint);
        PlanarLine l2 = new PlanarLine(P2Point, TPoint);
        PlanarLine l3 = new PlanarLine(P2Point, P1Point);
        float check = l1.Apply(res) * l2.Apply(res) + CC * math.square(l3.Apply(res));
        if (math.abs(check) > math.EPSILON)
        {
            Debug.LogWarningFormat("Check failed: {0}", check);
        }
        */

        return res;
    }

    public (Vector2, Vector2, Vector2) EvaluateWithRatioLine(float tt)
    {
        float r1 = tt, r2 = -4f * CC / tt;
        float u_p = r1 / (r1 + r2 + 2f), v_p = r2 / (r1 + r2 + 2f);

        Vector3 curvePoint = TPoint + u_p * (P1Point - TPoint) + v_p * (P2Point - TPoint);

        float u1, v2;

        if (math.abs(1f + r1) > math.EPSILON)
        {
            u1 = 1f - 1f / (r1 + 1f);
        }
        else
        {
            u1 = 0f;
        }

        if (math.abs(1f + r2) > math.EPSILON)
        {
            v2 = 1f - 1f / (r2 + 1f);
        }
        else
        {
            v2 = 0f;
        }

        Vector2 l1Point = u1 * P1Point + (1f - u1) * TPoint;
        Vector2 l2Point = v2 * P2Point + (1f - v2) * TPoint;

        return (curvePoint, l1Point, l2Point);
    }

    public Vector2 TPoint { get; set; }
    public Vector2 P1Point { get; set; }
    public Vector2 P2Point { get; set; }
    public Vector2 ExtraPoint { get; set; }

    private ConicUVCoordSys UVCoverter;

    public float CC { get; private set; }
    public float w1 { get; private set; }
    public float w2 { get; private set; }

    public static readonly float MinT = 0f, MaxT = 1f;
}
