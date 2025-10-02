using UnityEngine;

/// <summary>
/// Represents a cubic Bezier curve in 3D space.
/// </summary>
[System.Serializable]
public class BezierCurve {
    /// <summary>The starting point of the curve.</summary>
    public Vector3 startPoint;

    /// <summary>The ending point of the curve.</summary>
    public Vector3 endPoint;

    /// <summary>The tangent at the start point, influencing the curve's initial direction.</summary>
    public Vector3 startTangent;

    /// <summary>The tangent at the end point, influencing the curve's final direction.</summary>
    public Vector3 endTangent;

    /// <summary>
    /// Calculates a point on the Bezier curve at the given parameter t.
    /// </summary>
    /// <param name="t">The parameter t, ranging from 0 to 1.</param>
    /// <returns>A Vector3 representing the point on the curve at parameter t.</returns>
    public Vector3 GetPoint(float t) {
        float u   = 1 - t;
        float tt  = t * t;
        float uu  = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * startPoint;
        p += 3 * uu * t * startTangent;
        p += 3 * u * tt * endTangent;
        p += ttt * endPoint;

        return p;
    }

    /// <summary>
    /// Finds the parameter t of the closest point on the curve to a given point in 3D space.
    /// </summary>
    /// <param name="point">The point to find the closest point to.</param>
    /// <param name="steps">The number of steps to use in the approximation (default: 100).</param>
    /// <returns>The parameter t of the closest point on the curve.</returns>
    public float GetClosestPointOnCurve(Vector3 point, int steps = 100) {
        float closestT    = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i <= steps; i++) {
            float   t          = i / (float)steps;
            Vector3 curvePoint = GetPoint(t);
            float   distance   = Vector3.Distance(point, curvePoint);

            if (distance < minDistance) {
                minDistance = distance;
                closestT    = t;
            }
        }

        return closestT;
    }

    /// <summary>
    /// Calculates the tangent vector at a given point on the curve.
    /// </summary>
    /// <param name="t">The parameter t, ranging from 0 to 1.</param>
    /// <returns>A normalized Vector3 representing the tangent at the specified point.</returns>
    public Vector3 GetTangent(float t) {
        float   u       = 1 - t;
        Vector3 tangent = -3 * u * u * startPoint;
        tangent += 3 * u * u * startTangent - 6 * u * t * startTangent;
        tangent += -3 * t * t * endTangent + 6 * u * t * endTangent;
        tangent += 3 * t * t * endPoint;
        return tangent.normalized;
    }

    /// <summary>
    /// Calculates an approximation of the curve's length.
    /// </summary>
    /// <param name="segments">The number of segments to use in the approximation (default: 100).</param>
    /// <returns>The approximate length of the curve.</returns>
    public float GetApproximateLength(int segments = 100) {
        float   length        = 0;
        Vector3 previousPoint = GetPoint(0);
        for (int i = 1; i <= segments; i++) {
            float   t            = i / (float)segments;
            Vector3 currentPoint = GetPoint(t);
            length        += Vector3.Distance(previousPoint, currentPoint);
            previousPoint =  currentPoint;
        }

        return length;
    }

    /// <summary>
    /// Calculates a curviness value for the Bezier curve.
    /// </summary>
    /// <param name="samples">The number of samples to use in the calculation (default: 100).</param>
    /// <returns>An integer from 0 to 10 representing the curve's curviness.</returns>
    public int GetCurviness(int samples = 100) {
        float   maxCurvature = 0f;
        Vector3 prevTangent  = GetTangent(0);

        for (int i = 1; i <= samples; i++) {
            float   t              = i / (float)samples;
            Vector3 currentTangent = GetTangent(t);
            float   curvature      = Vector3.Angle(prevTangent, currentTangent) / (1f / samples);
            maxCurvature = Mathf.Max(maxCurvature, curvature);
            prevTangent  = currentTangent;
        }

        int curviness = Mathf.RoundToInt(maxCurvature / 18f);
        return Mathf.Clamp(curviness, 0, 10);
    }
}