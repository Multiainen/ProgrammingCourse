using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class Curves
{
    public const float sq2 = 1.4142136f;
    public const float sq2h = sq2 / 2;
    public const float gravity = -9.81f;
    public static readonly float3 gravityF3 = new float3(0, -9.81f, 0);

    public static int RoundFloat(float f)
    {
        return (int)(f + .50001f);
    }

    public static int RoundSigned(float f)
    {
        if (f >= 0) return (int)(f + .50001f);
        else return (int)(f - .50001f);
    }

    public static float FloatLerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        return a + (b - a) * t;
    }

    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
    {
        return a + (b - a) * t;
    }

    public static float2 Lerp(float2 a, float2 b, float t)
    {
        return a + (b - a) * t;
    }

    public static Vector3 QuadCurve(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 p0 = Lerp(a, b, t);
        Vector3 p1 = Lerp(b, c, t);
        return Lerp(p0, p1, t);
    }

    public static Vector2 QuadCurve(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        Vector2 p0 = Lerp(a, b, t);
        Vector2 p1 = Lerp(b, c, t);
        return Lerp(p0, p1, t);
    }

    public static float2 QuadCurve(float2 a, float2 b, float2 c, float t)
    {
        float2 p0 = Lerp(a, b, t);
        float2 p1 = Lerp(b, c, t);
        return Lerp(p0, p1, t);
    }

    public static Vector2 CubicCurve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        Vector2 p0 = QuadCurve(a, b, c, t);
        Vector2 p1 = QuadCurve(b, c, d, t);
        return Lerp(p0, p1, t);
    }

    public static Vector3 CubicCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        Vector3 p0 = QuadCurve(a, b, c, t);
        Vector3 p1 = QuadCurve(b, c, d, t);
        return Lerp(p0, p1, t);
    }

    public static float2 CubicCurve(float2 a, float2 b, float2 c, float2 d, float t)
    {
        float2 p0 = QuadCurve(a, b, c, t);
        float2 p1 = QuadCurve(b, c, d, t);
        return Lerp(p0, p1, t);
    }

    public static float2 CubicCurveHeight(float2 a, float2 b, float2 c, float2 d, float h, float prec)
    {
        float min = .3333333f;
        float max = .6666667f;
        float t = .5f;
        float2 p0 = QuadCurve(a, b, c, t);
        float2 p1 = QuadCurve(b, c, d, t);
        float2 final = Lerp(p0, p1, t);
        prec *= prec;

        while ((final.y - h) * (final.y - h) > prec && (min - max) * (min - max) > .000001f)
        {
            if (final.y < h && b.y < c.y || final.y > h && b.y > c.y) { min = t; t += (max - t) / 2f; }
            else { max = t; t += (min - t) / 2f; }
            p0 = QuadCurve(a, b, c, t);
            p1 = QuadCurve(b, c, d, t);
            final = Lerp(p0, p1, t);
        }
        return final;
    }

    public static float3 CalculateLaunchDirection(float3 start, float3 target, float velocity)
    {
        float3 displacement = target - start;
        float horizontalDistance = math.length(new float3(displacement.x, 0, displacement.z));
        float verticalDistance = displacement.y;

        float velocitySquared = velocity * velocity;
        float determinant = velocitySquared * velocitySquared - gravity * (gravity * horizontalDistance * horizontalDistance + 2 * verticalDistance * velocitySquared);

        if (determinant < 0)
            return float3.zero; // No valid launch direction

        float sqrtDet = math.sqrt(determinant);
        float angle = math.atan2(velocitySquared - sqrtDet, gravity * horizontalDistance); // Using the lower angle

        float3 horizontalDirection = math.normalize(new float3(displacement.x, 0, displacement.z));
        float horizontalSpeed = math.cos(angle) * velocity;
        float verticalSpeed = math.sin(angle) * velocity;

        return math.normalize(horizontalDirection * horizontalSpeed + new float3(0, 1, 0) * verticalSpeed);
    }

    public static float3 PredictiveLaunchDirection(float3 start, float3 target, float launchVelocity, float targetVelocity, float3 targetHeading)
    {
        float3 displacement = target - start;
        float horizontalDistance = math.length(new float3(displacement.x, 0, displacement.z));
        float verticalDistance = displacement.y;

        float velocitySquared = launchVelocity * launchVelocity;
        float determinant = velocitySquared * velocitySquared - gravity * (gravity * horizontalDistance * horizontalDistance + 2 * verticalDistance * velocitySquared);

        if (determinant < 0)
            return float3.zero; // No valid launch direction

        float sqrtDet = math.sqrt(determinant);
        float angle = math.atan2(velocitySquared - sqrtDet, gravity * horizontalDistance); // Using the lower angle

        float3 horizontalDirection = math.normalize(new float3(displacement.x, 0, displacement.z));
        float horizontalSpeed = math.cos(angle) * launchVelocity;
        float verticalSpeed = math.sin(angle) * launchVelocity;

        return math.normalize(horizontalDirection * horizontalSpeed + new float3(0, 1, 0) * verticalSpeed);
    }

    // assumes the target Y value will remain unchanged (curve control points represent X,Z)
    public static float3 PredictiveLaunchDirection(float3 start, float3 target, float launchVelocity, float targetVelocity, float targetCurvePos, float2 curveA, float2 curveB, float2 curveC, float2 curveD)
    {
        float3 displacement = target - start;
        float horizontalDistance = math.length(new float3(displacement.x, 0, displacement.z));
        float verticalDistance = displacement.y;

        float velocitySquared = launchVelocity * launchVelocity;
        float determinant = velocitySquared * velocitySquared - gravity * (gravity * horizontalDistance * horizontalDistance + 2 * verticalDistance * velocitySquared);

        if (determinant < 0)
            return float3.zero; // No valid launch direction

        float sqrtDet = math.sqrt(determinant);
        float angle = math.atan2(velocitySquared - sqrtDet, gravity * horizontalDistance); // Using the lower angle

        float3 horizontalDirection = math.normalize(new float3(displacement.x, 0, displacement.z));
        float horizontalSpeed = math.cos(angle) * launchVelocity;
        float verticalSpeed = math.sin(angle) * launchVelocity;

        return math.normalize(horizontalDirection * horizontalSpeed + new float3(0, 1, 0) * verticalSpeed);
    }

    public static Vector3[] SimplePoints(Vector3 start, Vector3 mid, Vector3 end, int count)
    {
        Vector3[] ret = new Vector3[count];

        for (int i = 0; i < count; i++) ret[i] = QuadCurve(start, mid, end, i / (float)count);

        return ret;
    }

    public static float2[] SpacedPoints(float2 min, float2 max, float space, int amount, float cutSpaceScale)
    {
        float2[] points = new float2[amount];
        int counter = 0;
        bool breaker = false;

        while (true)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new float2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y));
                for (int j = 0; j < i; j++) if (BaseOps.F2Sqr(points[j] - points[i]) < space * space) { breaker = true; break; }
                if (breaker) break;
            }
            if (breaker) { counter++; if (counter >= 30) { space *= cutSpaceScale; counter = 0; } breaker = false; continue; }
            return points;
        }
    }

    public static int[] AreaLimits(Vector2 a, Vector2 b, Vector2 c, float accuracy, bool xAxis) // draw (approximated) curve on integer lines to cordon off area in- and outside of curve on a grid
    {
        int[] ret;
        int start, end, counter = 0;
        float cur = 0;
        if (xAxis)
        {
            start = BaseOps.RoundFloat(a.x); end = BaseOps.RoundFloat(c.x); ret = new int[end - start + 1];
            while (counter < ret.Length)
            {
                cur += accuracy;
                if (QuadCurve(a, b, c, cur).x >= start + counter)
                {
                    ret[counter] = BaseOps.RoundFloat(QuadCurve(a, b, c, cur).y); counter++;
                }
            }
        }
        else
        {
            start = BaseOps.RoundFloat(a.y); end = BaseOps.RoundFloat(c.y); ret = new int[end - start + 1];
            while (counter < ret.Length)
            {
                cur += accuracy;
                if (QuadCurve(a, b, c, cur).y >= start + counter)
                {
                    ret[counter] = BaseOps.RoundFloat(QuadCurve(a, b, c, cur).x); counter++;
                }
            }
        }
        return ret;
    }

    public static int[] AreaLimits(int2 a, int2 b, int2 c, float accuracy, bool xAxis) // draw (approximated) curve on integer lines to cordon off area in- and outside of curve on a grid
    {
        int[] ret;
        int counter = 0;
        float cur = 0;
        if (xAxis)
        {
            ret = new int[c.x - a.x];
            while (counter < ret.Length)
            {
                cur += accuracy;
                if (QuadCurve(a, b, c, cur).x >= a.x + counter + .5f)
                {
                    ret[counter] = BaseOps.RoundFloat(QuadCurve(a, b, c, cur).y); counter++;
                }
            }
        }
        else
        {
            ret = new int[c.y - a.y];
            while (counter < ret.Length)
            {
                cur += accuracy;
                if (QuadCurve(a, b, c, cur).y >= a.y + counter + .5f)
                {
                    ret[counter] = BaseOps.RoundFloat(QuadCurve(a, b, c, cur).x); counter++;
                }
            }
        }
        return ret;
    }

    public static Vector3 FlatAngle(Vector3 v, int angle)
    {
        return new Vector3(0, v.y, 0) + Quaternion.Euler(0, angle, 0) * new Vector3(v.x, 0, v.z);
    }

    public static float2[] SpacedPoints(float2 min, float2 max, float space, int amount, float cutSpaceScale, float[] corners) // rounded corners; corner float value = distance of corner from edge, indexed clockwise (topleft, topright etc)
    {
        float2[] points = new float2[amount];
        int counter = 0;
        bool breaker = false;

        if (corners == null)
            while (true)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new float2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y));
                    for (int j = 0; j < i; j++) if (BaseOps.F2Sqr(points[j] - points[i]) < space * space) { breaker = true; break; }
                    if (breaker) break;
                }
                if (breaker) { counter++; if (counter >= 10) { space *= cutSpaceScale; counter = 0; } breaker = false; continue; }
                return points;
            }
        while (true)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new float2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y));
                while (true)
                {
                    if (corners[0] > 0) if (points[i].x < min.x + corners[0] || points[i].y > max.y - corners[0]) if (BaseOps.F2Sqr(new float2(min.x + corners[0], max.y - corners[0]) - points[i]) > corners[0] * corners[0]) { points[i] = new float2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y)); continue; }
                    if (corners[1] > 0) if (points[i].x < max.x - corners[1] || points[i].y > max.y - corners[1]) if (BaseOps.F2Sqr(new float2(max.x - corners[1], max.y - corners[1]) - points[i]) > corners[1] * corners[1]) { points[i] = new float2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y)); continue; }
                    if (corners[2] > 0) if (points[i].x < max.x - corners[2] || points[i].y > min.y + corners[2]) if (BaseOps.F2Sqr(new float2(max.x - corners[2], min.y + corners[2]) - points[i]) > corners[2] * corners[2]) { points[i] = new float2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y)); continue; }
                    if (corners[3] > 0) if (points[i].x < min.x + corners[3] || points[i].y > min.y + corners[3]) if (BaseOps.F2Sqr(new float2(min.x + corners[3], min.y + corners[3]) - points[i]) > corners[3] * corners[3]) { points[i] = new float2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y)); continue; }
                    break;
                }
                for (int j = 0; j < i; j++) if (BaseOps.F2Sqr(points[j] - points[i]) < space * space) { breaker = true; break; }
                if (breaker) break;
            }
            if (breaker) { counter++; if (counter >= 10) { space *= cutSpaceScale; counter = 0; } breaker = false; continue; }
            return points;
        }
    }

    public static Vector2[] DetailCurveFull(Vector2[] initPoints, int points, int octaves, float octaveWeight, float noise, float4 limit, Vector2 center, float lacunarity) // initPoints needs start, 3 midpoints (2 for cubic curve, 1 for quad) and end
    {
        Vector2[] final = new Vector2[points];

        Vector2[][] curves = new Vector2[octaves][];
        Vector2[] detCurve;
        float[] weights = new float[octaves];
        float[] step = new float[octaves];
        float fullWeight = 1;
        float curDist = 0;
        float s;
        Vector2 refPoint;

        refPoint = LineIntersectSigned(CubicCurve(initPoints[0], initPoints[1], initPoints[2], initPoints[3], .001f), initPoints[0], CubicCurve(initPoints[0], initPoints[1], initPoints[2], initPoints[3], .999f), initPoints[3]);
        curves[0] = new Vector2[4];
        curves[0][0] = initPoints[0]; curves[0][3] = initPoints[3];
        curves[0][1] = CubicCurve(initPoints[0], initPoints[1], initPoints[2], initPoints[3], 1 / 3f);
        curves[0][2] = CubicCurve(initPoints[0], initPoints[1], initPoints[2], initPoints[3], 2 / 3f);
        weights[0] = 1;
        for (int i = 1; i < octaves; i++)
        {
            detCurve = new Vector2[4 + i * 3];
            curves[i] = new Vector2[4 + i * 9];
            for (int j = 1; j < detCurve.Length - 3; j++)
                detCurve[j] = new Vector2(j, UnityEngine.Random.Range(-lacunarity * .8f, lacunarity));
            detCurve[0] = new Vector2(0, detCurve[detCurve.Length - 4].y); detCurve[detCurve.Length - 3] = new Vector2(detCurve.Length - 3, detCurve[1].y); detCurve[detCurve.Length - 2] = new Vector2(detCurve.Length - 2, detCurve[2].y); detCurve[detCurve.Length - 1] = new Vector2(detCurve.Length - 1, detCurve[3].y);
            for (int j = 0, curStep; j < curves[i].Length; j++)
            {
                curDist = UnityEngine.Random.Range(j / (float)curves[i].Length, (j + .9f) / curves[i].Length);
                s = j / (float)curves[i].Length * (detCurve.Length - 4);
                curStep = (int)(s - s % 3);
                if (curDist < .75f)
                    curves[i][j] = center + (CubicCurve(initPoints[0], initPoints[1], initPoints[2], initPoints[3], curDist * 4 / 3f) - center) * (1 + CubicCurve(detCurve[(int)s], detCurve[(int)s + 1], detCurve[(int)s + 2], detCurve[(int)s + 3], ((s - (int)s) + 1) / 3f).y);
                else
                    curves[i][j] = center + (QuadCurve(initPoints[3], refPoint, initPoints[0], (curDist - .75f) * 4) - center) * (1 + CubicCurve(detCurve[(int)s], detCurve[(int)s + 1], detCurve[(int)s + 2], detCurve[(int)s + 3], ((s - (int)s) + 1) / 3f).y);
            }
            weights[i] = weights[i - 1] * octaveWeight;
            fullWeight += weights[i];
        }

        for (int i = 0; i < final.Length; i++)
        {
            for (int j = 0; j < octaves; j++)
            {
                curDist = (curves[j].Length - 1) / (float)(final.Length - 1) * i;
                if (curDist < curves[j].Length - 1)
                    final[i] += Lerp(curves[j][(int)curDist], curves[j][(int)curDist + 1], curDist - (int)curDist) * weights[j];
                else
                    final[i] += Lerp(curves[j][curves[j].Length - 1], curves[j][0], curDist - (int)curDist) * weights[j];
            }
            final[i] /= fullWeight;
            while (final[i].x < limit.x || final[i].x > limit.y || final[i].y < limit.z || final[i].y > limit.w)
                final[i] = center + (final[i] - center) * .9f;
        }

        return final;
    }

    public static Vector2[] DetailCurve(Vector2[] initPoints, int points, int octaves, float octaveWeight, float noise, float4 limit, Vector2 center, float lacunarity) // centered curve (deformation calculated from a pivot point)
    {
        Vector2[] final = new Vector2[points];

        Vector2[][] curves = new Vector2[octaves][];
        Vector2[] detCurve;
        float[] weights = new float[octaves];
        float[] step = new float[octaves];
        float fullWeight = 1;
        float curDist = 0;
        float s;
        int counter = 0;

        curves[0] = new Vector2[4];
        curves[0][0] = initPoints[0]; curves[0][3] = initPoints[3];
        curves[0][1] = CubicCurve(initPoints[0], initPoints[1], initPoints[2], initPoints[3], 1 / 3f);
        curves[0][2] = CubicCurve(initPoints[0], initPoints[1], initPoints[2], initPoints[3], 2 / 3f);
        weights[0] = 1;
        for (int i = 1; i < octaves; i++)
        {
            detCurve = new Vector2[4 + i * 3];
            curves[i] = new Vector2[4 + i * 9];
            for (int j = 1; j < detCurve.Length - 4; j++)
                detCurve[j] = new Vector2(j, UnityEngine.Random.Range(-lacunarity * .5f, lacunarity * .5f));
            detCurve[0] = new Vector2(0, 0); detCurve[detCurve.Length - 4] = detCurve[0]; detCurve[detCurve.Length - 3] = new Vector2(detCurve.Length - 3, detCurve[1].y); detCurve[detCurve.Length - 2] = new Vector2(detCurve.Length - 2, detCurve[2].y); detCurve[detCurve.Length - 1] = new Vector2(detCurve.Length - 1, detCurve[3].y);
            for (int j = 1; j < curves[i].Length - 1; j++)
            {
                curDist = UnityEngine.Random.Range(j / (float)curves[i].Length, (j + .9f) / curves[i].Length);
                s = j / (float)curves[i].Length * (detCurve.Length - 4);
                curves[i][j] = center + (CubicCurve(initPoints[0], initPoints[1], initPoints[2], initPoints[3], curDist) - center) * (1 + CubicCurve(detCurve[(int)s], detCurve[(int)s + 1], detCurve[(int)s + 2], detCurve[(int)s + 3], ((s - (int)s) + 1) / 3f).y);
            }
            curves[i][0] = initPoints[0];
            curves[i][curves[i].Length - 1] = initPoints[3];
            weights[i] = weights[i - 1] * octaveWeight;
            fullWeight += weights[i];
        }

        for (int i = 1; i < final.Length - 1; i++)
        {
            for (int j = 0; j < octaves; j++)
            {
                curDist = (curves[j].Length - 1) / (float)(final.Length - 1) * i;
                final[i] += Lerp(curves[j][(int)curDist], curves[j][(int)curDist + 1], curDist - (int)curDist) * weights[j];
            }
            final[i] /= fullWeight;
            while (final[i].x < limit.x || final[i].x > limit.y || final[i].y < limit.z || final[i].y > limit.w)
            {
                counter++;
                final[i] = center + (final[i] - center) * .9f;
                if (counter > 20)
                {
                    if (final[i].x < limit.x) final[i].x = limit.x;
                    if (final[i].x > limit.y) final[i].x = limit.y;
                    if (final[i].y < limit.z) final[i].y = limit.z;
                    if (final[i].y > limit.w) final[i].y = limit.w;
                    counter = 0;
                    break;
                }
            }
            counter = 0;
        }
        final[final.Length - 1] = initPoints[3];
        final[0] = initPoints[0];

        return final;
    }

    public static Vector2[] DetailCurve(Vector2[] initPoints, int points, int octaves, float octaveWeight, float noise, float4 limit, bool vertical, float persistence) // centered curve (deformation calculated from a pivot point)
    {
        Vector2[] final = new Vector2[points];

        Vector2[][] curves = new Vector2[octaves][];
        Vector2[] detCurve;
        float[] weights = new float[octaves];
        float[] step = new float[octaves];
        float fullWeight = 1;
        float curDist = 0;
        float s;

        octaveWeight = persistence;
        if (vertical) noise *= limit.w - limit.z;
        else noise *= limit.y - limit.x;
        curves[0] = new Vector2[4];
        for (int i = 0; i < 4; i++) curves[0][i] = Lerp(initPoints[0], initPoints[1], i / 3f);
        weights[0] = 1;
        for (int i = 1; i < octaves; i++)
        {
            detCurve = new Vector2[4 + i * 3];
            curves[i] = new Vector2[4 + i * 9];
            for (int j = 1; j < detCurve.Length - 4; j++)
                detCurve[j] = new Vector2(j, UnityEngine.Random.Range(-noise, noise));
            detCurve[0] = new Vector2(0, 0); detCurve[detCurve.Length - 4] = detCurve[0]; detCurve[detCurve.Length - 3] = new Vector2(detCurve.Length - 3, detCurve[1].y); detCurve[detCurve.Length - 2] = new Vector2(detCurve.Length - 2, detCurve[2].y); detCurve[detCurve.Length - 1] = new Vector2(detCurve.Length - 1, detCurve[3].y);
            if (vertical)
                for (int j = 1; j < curves[i].Length - 1; j++)
                {
                    curDist = UnityEngine.Random.Range(j / (float)curves[i].Length, (j + .9f) / curves[i].Length);
                    s = j / (float)curves[i].Length * (detCurve.Length - 4);
                    curves[i][j] = Lerp(initPoints[0], initPoints[1], curDist) + new Vector2(CubicCurve(detCurve[(int)s], detCurve[(int)s + 1], detCurve[(int)s + 2], detCurve[(int)s + 3], ((s - (int)s) + 1) / 3f).y, 0);
                }
            else
                for (int j = 1; j < curves[i].Length - 1; j++)
                {
                    curDist = UnityEngine.Random.Range(j / (float)curves[i].Length, (j + .9f) / curves[i].Length);
                    s = j / (float)curves[i].Length * (detCurve.Length - 4);
                    curves[i][j] = Lerp(initPoints[0], initPoints[1], curDist) + new Vector2(0, CubicCurve(detCurve[(int)s], detCurve[(int)s + 1], detCurve[(int)s + 2], detCurve[(int)s + 3], ((s - (int)s) + 1) / 3f).y);
                }
            curves[i][0] = initPoints[0];
            curves[i][curves[i].Length - 1] = initPoints[1];
            weights[i] = weights[i - 1] * octaveWeight;
            fullWeight += weights[i];
        }

        for (int i = 1; i < final.Length - 1; i++)
        {
            for (int j = 0; j < octaves; j++)
            {
                curDist = (curves[j].Length - 1) / (float)(final.Length - 1) * i;
                final[i] += Lerp(curves[j][(int)curDist], curves[j][(int)curDist + 1], curDist - (int)curDist) * weights[j];
            }
            final[i] /= fullWeight;
            if (vertical)
            {
                if (final[i].x < limit.x)
                    final[i] = new Vector2(UnityEngine.Random.Range(limit.x, limit.x + (limit.y - limit.x) * .2f), final[i].y);
                else if (final[i].x > limit.y)
                    final[i] = new Vector2(UnityEngine.Random.Range(limit.x + (limit.y - limit.x) * .8f, limit.y), final[i].y);
            }
            else
            {
                if (final[i].y < limit.z)
                    final[i] = new Vector2(final[i].x, UnityEngine.Random.Range(limit.z, limit.z + (limit.w - limit.z) * .2f));
                else if (final[i].y > limit.w)
                    final[i] = new Vector2(final[i].x, UnityEngine.Random.Range(limit.z + (limit.w - limit.z) * .8f, limit.w));
            }
        }
        final[final.Length - 1] = initPoints[1];
        final[0] = initPoints[0];

        return final;
    }

    public static Vector2 Rotate(Vector2 v, float delta)
    {
        delta *= Mathf.Deg2Rad;
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }

    public static float2 Rotate(float2 v, float delta)
    {
        while (delta < 0) delta += 360;
        while (delta > 360) delta -= 360;
        delta *= math.TORADIANS;
        return new float2(
    v.x * math.cos(delta) - v.y * math.sin(delta),
    v.x * math.sin(delta) + v.y * math.cos(delta)
);
    }

    public static Vector2 Clamp(Vector2 input, float xMin, float xMax, float yMin, float yMax)
    {
        if (input.x > xMax) input = new Vector2(xMax, input.y);
        else if (input.x < xMin) input = new Vector2(xMin, input.y);
        if (input.y > yMax) input = new Vector2(input.x, yMax);
        else if (input.y < yMin) input = new Vector2(input.x, yMin);

        return input;
    }

    public static Vector2 RdmClamp(Vector2 input, float xMin, float xMax, float yMin, float yMax, float rdmRange)
    {
        if (input.x > xMax) input = new Vector2(UnityEngine.Random.Range(xMax - (xMax - xMin) * rdmRange, xMax), input.y);
        else if (input.x < xMin) input = new Vector2(UnityEngine.Random.Range(xMin, xMin + (xMax - xMin) * rdmRange), input.y);
        if (input.y > yMax) input = new Vector2(input.x, UnityEngine.Random.Range(yMax - (yMax - yMin) * rdmRange, yMax));
        else if (input.y < yMin) input = new Vector2(input.x, UnityEngine.Random.Range(yMin, yMin + (yMax - yMin) * rdmRange));

        return input;
    }

    public static Vector2 RdmClamp(Vector2 input, float4 limit, float rdmRange)
    {
        if (input.x > limit.y) input = new Vector2(UnityEngine.Random.Range(limit.y - (limit.y - limit.x) * rdmRange, limit.y), input.y);
        else if (input.x < limit.x) input = new Vector2(UnityEngine.Random.Range(limit.x, limit.x + (limit.y - limit.x) * rdmRange), input.y);
        if (input.y > limit.w) input = new Vector2(input.x, UnityEngine.Random.Range(limit.w - (limit.w - limit.z) * rdmRange, limit.w));
        else if (input.y < limit.z) input = new Vector2(input.x, UnityEngine.Random.Range(limit.z, limit.z + (limit.w - limit.z) * rdmRange));

        return input;
    }

    public static float ClampDist(Vector2 input, float limitX, float limitY) // returns max multiplier of input that's at/within both limits
    {
        if (limitX / input.x < limitY / input.y) return limitX / input.x;
        return limitY / input.y;
    }

    // For clamping vector max length in curve calculation. 
    // input = vector to be clamped
    // hardLimit = absolute min and max limits for X and Y (x = xMin, y = xMax, z = yMin, w = yMax)
    // limit = limiting vector (do not go beyond this OR any of the hard limits). NOTE: Function assumes this vector is located within the hard limits. Should work even if it isn't, can't be arsed figuring that out atm.
    public static float ClampDist(Vector2 input, float4 hardLimit, Vector2 limit)
    {
        if (input.x > 0)
        {
            if (input.y > 0)
            {
                if (limit.x > input.x && limit.y > input.y)
                {
                    if ((input.x / input.y) * (input.x / input.y) > (limit.x - input.x) / (limit.y - input.y) * (limit.x - input.x) / (limit.y - input.y)) return ClampDist(input, limit.x, hardLimit.w);
                    return ClampDist(input, hardLimit.y, limit.y);
                }
                if (limit.x > input.x)
                    return ClampDist(input, limit.x, hardLimit.w);
                else if (limit.y > input.y)
                    return ClampDist(input, hardLimit.y, limit.y);
                else return ClampDist(input, hardLimit.y, hardLimit.w);
            }
            else
            {
                if (limit.x > input.x && limit.y < input.y)
                {
                    if ((input.x / input.y) * (input.x / input.y) < (limit.x - input.x) / (limit.y - input.y) * (limit.x - input.x) / (limit.y - input.y)) return ClampDist(input, limit.x, hardLimit.z);
                    return ClampDist(input, hardLimit.y, limit.y);
                }
                else if (limit.x > input.x)
                    return ClampDist(input, limit.x, hardLimit.z);
                else if (limit.y < input.y)
                    return ClampDist(input, hardLimit.y, limit.y);
                else return ClampDist(input, hardLimit.y, hardLimit.z);
            }
        }
        else
        {
            if (input.y > 0)
            {
                if (limit.x < input.x && limit.y > input.y)
                {
                    if ((input.x / input.y) * (input.x / input.y) < (limit.x - input.x) / (limit.y - input.y) * (limit.x - input.x) / (limit.y - input.y)) return ClampDist(input, limit.x, hardLimit.w);
                    return ClampDist(input, hardLimit.x, limit.y);
                }
                else if (limit.x < input.x)
                    return ClampDist(input, limit.x, hardLimit.w);
                else if (limit.y > input.y)
                    return ClampDist(input, hardLimit.x, limit.y);
                else return ClampDist(input, hardLimit.x, hardLimit.w);
            }
            else
            {
                if (limit.x < input.x && limit.y < input.y)
                {
                    if ((input.x / input.y) * (input.x / input.y) > (limit.x - input.x) / (limit.y - input.y) * (limit.x - input.x) / (limit.y - input.y)) return ClampDist(input, limit.x, hardLimit.z);
                    return ClampDist(input, hardLimit.x, limit.y);
                }
                else if (limit.x < input.x)
                    return ClampDist(input, limit.x, hardLimit.z);
                else if (limit.y < input.y)
                    return ClampDist(input, hardLimit.x, limit.y);
                else return ClampDist(input, hardLimit.x, hardLimit.z);
            }
        }

    }

    public static float2[] RoundedSq(float scale, float rounding, int curveDetail) // scale = "diameter" of square (side / 2); rounding = distance from middle to start point of rounding curve; curveDetail = amount of points on rounding curve (start included, end included)
    {
        float2[] points = new float2[curveDetail * 4 + 4];

        for (int i = 0; i <= curveDetail; i++)
        {
            points[i] = QuadCurve(new float2(rounding, scale), new float2(scale, scale), new float2(scale, rounding), i / (float)curveDetail);
        }
        for (int i = 0; i <= curveDetail; i++)
        {
            points[i + curveDetail + 1] = new float2(points[i].y, -points[i].x);
            points[i + curveDetail * 2 + 2] = new float2(-points[i].x, -points[i].y);
            points[i + curveDetail * 3 + 3] = new float2(-points[i].y, points[i].x);
        }

        return points;
    }

    public static (float2[], int[]) RoundedSqShape(float scale, float rounding, float unitScale, int curveDetail, float2[] tiles, HashSet<float2> ghost)
    {
        HashSet<float2> check = new HashSet<float2>();
        float2[] curve = new float2[curveDetail + 1];
        float2[] invCurve = new float2[curveDetail + 1];
        int[] variant = new int[tiles.Length]; // amount of vertices in segment
        Queue<float2> points = new Queue<float2>();
        float halfScale = unitScale / 2;

        if (tiles.Length == 1)
            return (RoundedSq(scale, rounding, curveDetail), new int[] { curveDetail * 4 + 4 });

        for (int i = 0; i < tiles.Length; i++) check.Add(tiles[i]);
        for (int i = 0; i <= curveDetail; i++)
        {
            curve[i] = QuadCurve(new float2(rounding, scale), new float2(scale, scale), new float2(scale, rounding), i / (float)curveDetail);
            invCurve[i] = QuadCurve(new float2(-scale, -halfScale), new float2(-scale, -scale), new float2(-halfScale, -scale), i / (float)curveDetail);
        }
        curveDetail++;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (ghost.Contains(tiles[i])) continue;
            if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y)))
            {
                if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y)))
                {
                    if (check.Contains(new float2(tiles[i].x, tiles[i].y + unitScale)))
                    {
                        if (check.Contains(new float2(tiles[i].x, tiles[i].y - unitScale)))
                        {
                            if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y + unitScale)))
                            {
                                if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y + unitScale)))
                                {
                                    if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y - unitScale)))
                                    {
                                        if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // all
                                        {
                                            variant[i] = 5;
                                            points.Enqueue(tiles[i]);
                                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                            points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                            points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                                        }
                                        else // all but bot left
                                        {
                                            variant[i] = curveDetail + 4;
                                            points.Enqueue(tiles[i]);
                                            points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                            points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                                            for (int j = 0; j < curveDetail; j++)
                                                points.Enqueue(tiles[i] + invCurve[j]);
                                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                        }
                                    }
                                    else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale)))  // all but bot right
                                    {
                                        variant[i] = curveDetail + 4;
                                        points.Enqueue(tiles[i]);
                                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                        for (int j = 0; j < curveDetail; j++)
                                            points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                    }
                                    else  // all but bot left, bot right
                                    {
                                        variant[i] = curveDetail * 2 + 3;
                                        points.Enqueue(tiles[i]);
                                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                        for (int j = 0; j < curveDetail; j++)
                                            points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                                        for (int j = 0; j < curveDetail; j++)
                                            points.Enqueue(tiles[i] + invCurve[j]);
                                    }
                                }
                                else if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y - unitScale)))
                                {
                                    if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // all but top left
                                    {
                                        variant[i] = curveDetail + 4;
                                        points.Enqueue(tiles[i]);
                                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                        for (int j = 0; j < curveDetail; j++)
                                            points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                    }
                                    else // all but bot left, top left
                                    {
                                        variant[i] = curveDetail * 2 + 3;
                                        points.Enqueue(tiles[i]);
                                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                                        for (int j = 0; j < curveDetail; j++)
                                            points.Enqueue(tiles[i] + invCurve[j]);
                                        for (int j = 0; j < curveDetail; j++)
                                            points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                                    }
                                }
                                else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // all but top left, bot right
                                {
                                    variant[i] = curveDetail * 2 + 3;
                                    points.Enqueue(tiles[i]);
                                    points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                                    points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                                }
                                else // cardinals + top right
                                {
                                    variant[i] = curveDetail * 3 + 2;
                                    points.Enqueue(tiles[i]);
                                    points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + invCurve[j]);
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                                }
                            }
                            else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y + unitScale)))
                            {
                                if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y - unitScale)))
                                {
                                    if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // all but top right
                                    {
                                        variant[i] = curveDetail + 4;
                                        points.Enqueue(tiles[i]);
                                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                        for (int j = 0; j < curveDetail; j++)
                                            points.Enqueue(tiles[i] - invCurve[j]);
                                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                                    }
                                    else // all but top right, bot left
                                    {
                                        variant[i] = curveDetail * 2 + 3;
                                        points.Enqueue(tiles[i]);
                                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                        for (int j = 0; j < curveDetail; j++)
                                            points.Enqueue(tiles[i] - invCurve[j]);
                                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                                        for (int j = 0; j < curveDetail; j++)
                                            points.Enqueue(tiles[i] + invCurve[j]);
                                    }
                                }
                                else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale)))  // all but top right, bot right
                                {
                                    variant[i] = curveDetail * 2 + 3;
                                    points.Enqueue(tiles[i]);
                                    points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] - invCurve[j]);
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                                    points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                }
                                else // cardinals + top left
                                {
                                    variant[i] = curveDetail * 3 + 2;
                                    points.Enqueue(tiles[i]);
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] - invCurve[j]);
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + invCurve[j]);
                                    points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                }
                            }
                            else if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y - unitScale)))
                            {
                                if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale)))  // all but top right, top left
                                {
                                    variant[i] = curveDetail * 2 + 3;
                                    points.Enqueue(tiles[i]);
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] - invCurve[j]);
                                    points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                                    points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                }
                                else // cardinals + bot right
                                {
                                    variant[i] = curveDetail * 3 + 2;
                                    points.Enqueue(tiles[i]);
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] - invCurve[j]);
                                    points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + invCurve[j]);
                                    for (int j = 0; j < curveDetail; j++)
                                        points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                                }
                            }
                            else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // cardinals + bot left
                            {
                                variant[i] = curveDetail * 3 + 2;
                                points.Enqueue(tiles[i]);
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] - invCurve[j]);
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                            }
                            else // cardinals
                            {
                                variant[i] = curveDetail * 4 + 1;
                                points.Enqueue(tiles[i]);
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] - invCurve[j]);
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] + invCurve[j]);
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                            }
                        }
                        else if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y + unitScale)))
                        {
                            if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y + unitScale))) // top, right, left, top right, top left
                            {
                                variant[i] = 5;
                                points.Enqueue(tiles[i]);
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - scale));
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - scale));
                            }
                            else // top, right, left, top right
                            {
                                variant[i] = curveDetail + 4;
                                points.Enqueue(tiles[i]);
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - scale));
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - scale));
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                            }
                        }
                        else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y + unitScale))) // top, right, left, top left
                        {
                            variant[i] = curveDetail + 4;
                            points.Enqueue(tiles[i]);
                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - scale));
                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] - invCurve[j]);
                            points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - scale));
                        }
                        else // top, right, left
                        {
                            variant[i] = curveDetail * 2 + 3;
                            points.Enqueue(tiles[i]);
                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - scale));
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] - invCurve[j]);
                            points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - scale));
                        }
                    }
                    else if (check.Contains(new float2(tiles[i].x, tiles[i].y - unitScale)))
                    {
                        if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y - unitScale)))
                        {
                            if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // bot, right, left, bot right, bot left
                            {
                                variant[i] = 5;
                                points.Enqueue(tiles[i]);
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + scale));
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + scale));
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                            }
                            else // bot, right, left, bot right
                            {
                                variant[i] = curveDetail + 4;
                                points.Enqueue(tiles[i]);
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + scale));
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] + invCurve[j]);
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + scale));
                            }
                        }
                        else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // bot, right, left, bot left
                        {
                            variant[i] = curveDetail + 4;
                            points.Enqueue(tiles[i]);
                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + scale));
                            points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + scale));
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                        }
                        else // bot, right, left
                        {
                            variant[i] = curveDetail * 2 + 3;
                            points.Enqueue(tiles[i]);
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] + invCurve[j]);
                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + scale));
                            points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + scale));
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                        }
                    }
                    else // right, left
                    {
                        variant[i] = 5;
                        points.Enqueue(tiles[i]);
                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - scale));
                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + scale));
                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + scale));
                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - scale));
                    }
                }
                else if (check.Contains(new float2(tiles[i].x, tiles[i].y + unitScale)))
                {
                    if (check.Contains(new float2(tiles[i].x, tiles[i].y - unitScale)))
                    {
                        if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y + unitScale)))
                        {
                            if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y - unitScale))) // top, bot, right, top right, bot right
                            {
                                variant[i] = 5;
                                points.Enqueue(tiles[i]);
                                points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y - halfScale));
                                points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y + halfScale));
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                            }
                            else // top, bot, right, top right
                            {
                                variant[i] = curveDetail + 4;
                                points.Enqueue(tiles[i]);
                                points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y + halfScale));
                                points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                                points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y - halfScale));
                            }
                        }
                        else if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y - unitScale))) // top, bot, right, bot right
                        {
                            variant[i] = curveDetail + 4;
                            points.Enqueue(tiles[i]);
                            points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y - halfScale));
                            points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y + halfScale));
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] - invCurve[j]);
                            points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                        }
                        else // top, bot, right
                        {
                            variant[i] = curveDetail * 2 + 3;
                            points.Enqueue(tiles[i]);
                            points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y - halfScale));
                            points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y + halfScale));
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] - invCurve[j]);
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                        }
                    }
                    else if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y + unitScale))) // top, right, top right
                    {
                        variant[i] = curveDetail + 4;
                        points.Enqueue(tiles[i]);
                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + halfScale));
                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - scale));
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] - curve[j]);
                        points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y + halfScale));
                    }
                    else // top, right
                    {
                        variant[i] = curveDetail * 2 + 3;
                        points.Enqueue(tiles[i]);
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] - curve[j]);
                        points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y + halfScale));
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] - invCurve[j]);
                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - scale));
                    }
                }
                else if (check.Contains(new float2(tiles[i].x, tiles[i].y - unitScale)))
                {
                    if (check.Contains(new float2(tiles[i].x + unitScale, tiles[i].y - unitScale))) // bot, right, bot right
                    {
                        variant[i] = curveDetail + 4;
                        points.Enqueue(tiles[i]);
                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - halfScale));
                        points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y - halfScale));
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] + new float2(-curve[j].y, curve[j].x));
                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + scale));
                    }
                    else // bot, right
                    {
                        variant[i] = curveDetail * 2 + 3;
                        points.Enqueue(tiles[i]);
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] + new float2(-curve[j].y, curve[j].x));
                        points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + scale));
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] + new float2(-invCurve[j].y, invCurve[j].x));
                        points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y - halfScale));
                    }
                }
                else // right
                {
                    variant[i] = curveDetail * 2 + 3;
                    points.Enqueue(tiles[i]);
                    points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y - scale));
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i] - curve[j]);
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i] + new float2(-curve[j].y, curve[j].x));
                    points.Enqueue(new float2(tiles[i].x + halfScale, tiles[i].y + scale));
                }
            }
            else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y)))
            {
                if (check.Contains(new float2(tiles[i].x, tiles[i].y + unitScale)))
                {
                    if (check.Contains(new float2(tiles[i].x, tiles[i].y - unitScale)))
                    {
                        if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y + unitScale)))
                        {
                            if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // top, bot, left, top left, bot left
                            {
                                variant[i] = 5;
                                points.Enqueue(tiles[i]);
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                                points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y + halfScale));
                                points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y - halfScale));
                            }
                            else // top, bot, left, top left
                            {
                                variant[i] = curveDetail + 4;
                                points.Enqueue(tiles[i]);
                                points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y + halfScale));
                                points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y - halfScale));
                                for (int j = 0; j < curveDetail; j++)
                                    points.Enqueue(tiles[i] + invCurve[j]);
                                points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                            }
                        }
                        else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // top, bot, left, bot left
                        {
                            variant[i] = curveDetail + 4;
                            points.Enqueue(tiles[i]);
                            points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y - halfScale));
                            points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                            points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y + halfScale));
                        }
                        else // top, bot, left
                        {
                            variant[i] = curveDetail * 2 + 3;
                            points.Enqueue(tiles[i]);
                            points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y - halfScale));
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] + invCurve[j]);
                            for (int j = 0; j < curveDetail; j++)
                                points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                            points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y + halfScale));
                        }
                    }
                    else if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y + unitScale))) // top, left, top left
                    {
                        variant[i] = curveDetail + 4;
                        points.Enqueue(tiles[i]);
                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + halfScale));
                        points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y + halfScale));
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] + new float2(curve[j].y, -curve[j].x));
                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - scale));
                    }
                    else // top, left
                    {
                        variant[i] = curveDetail * 2 + 3;
                        points.Enqueue(tiles[i]);
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] + new float2(curve[j].y, -curve[j].x));
                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - scale));
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] + new float2(invCurve[j].y, -invCurve[j].x));
                        points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y + halfScale));
                    }
                }
                else if (check.Contains(new float2(tiles[i].x, tiles[i].y - unitScale)))
                {
                    if (check.Contains(new float2(tiles[i].x - unitScale, tiles[i].y - unitScale))) // bot, left, bot left
                    {
                        variant[i] = curveDetail + 4;
                        points.Enqueue(tiles[i]);
                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - halfScale));
                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + scale));
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] + curve[j]);
                        points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y - halfScale));
                    }
                    else // bot, left
                    {
                        variant[i] = curveDetail * 2 + 3;
                        points.Enqueue(tiles[i]);
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] + curve[j]);
                        points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y - halfScale));
                        for (int j = 0; j < curveDetail; j++)
                            points.Enqueue(tiles[i] + invCurve[j]);
                        points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + scale));
                    }
                }
                else // left
                {
                    variant[i] = curveDetail * 2 + 3;
                    points.Enqueue(tiles[i]);
                    points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y + scale));
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i] + curve[j]);
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i] + new float2(curve[j].y, -curve[j].x));
                    points.Enqueue(new float2(tiles[i].x - halfScale, tiles[i].y - scale));
                }
            }
            else if (check.Contains(new float2(tiles[i].x, tiles[i].y + unitScale)))
            {
                if (check.Contains(new float2(tiles[i].x, tiles[i].y - unitScale))) // top, bot
                {
                    variant[i] = 5;
                    points.Enqueue(tiles[i]);
                    points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y - halfScale));
                    points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y + halfScale));
                    points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y + halfScale));
                    points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y - halfScale));
                }
                else // top
                {
                    variant[i] = curveDetail * 2 + 3;
                    points.Enqueue(tiles[i]);
                    points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y + halfScale));
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i] + new float2(curve[j].y, -curve[j].x));
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i] - curve[j]);
                    points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y + halfScale));
                }
            }
            else if (check.Contains(new float2(tiles[i].x, tiles[i].y - unitScale))) // bot
            {
                variant[i] = curveDetail * 2 + 3;
                points.Enqueue(tiles[i]);
                points.Enqueue(new float2(tiles[i].x - scale, tiles[i].y - halfScale));
                for (int j = 0; j < curveDetail; j++)
                    points.Enqueue(tiles[i] + new float2(-curve[j].y, curve[j].x));
                for (int j = 0; j < curveDetail; j++)
                    points.Enqueue(tiles[i] + curve[j]);
                points.Enqueue(new float2(tiles[i].x + scale, tiles[i].y - halfScale));
            }
        }

        return (BaseOps.QueueToArray(points), variant);
    }

    public static (float2[], float2[]) RoundedSqShapeRaw(float scale, float rounding, float unitScale, int curveDetail, float2[] tiles) // primer for RoundedSqShape2, with unordered and full array of tile coverage
    {
        Queue<float2> bq = new Queue<float2>();

        for (int i = 0; i < tiles.Length; i++)
            for (int j = 0; j < tiles.Length; j++)
                if (true)
                {

                }
        // maybe continue later (found alternative)

        return (RoundedSqShape2(scale, rounding, unitScale, curveDetail, null), null);
    }

    public static float2[] RoundedSqShape2(float scale, float rounding, float unitScale, int curveDetail, float2[] tiles) // multiple connected tiles forming one RoundedSq shape, edge tiles only (without inside corners); tiles array first element is always top right corner of the shape
    {
        float2[] curve = new float2[curveDetail];
        Queue<float2> points = new Queue<float2>();
        int dir = 0; // 0 = down, 1 = left, 2 = up, 3 = right

        if (tiles.Length == 1)
            return RoundedSq(scale, rounding, curveDetail);

        for (int i = 0; i < curveDetail; i++)
        {
            curve[i] = Lerp(new float2(rounding, scale), new float2(scale, rounding), i / (float)curveDetail);
            points.Enqueue(tiles[0] + curve[i]);
        }

        for (int i = 1; i < tiles.Length; i++)
        {
            if (dir == 0)
            {
                if (tiles[i].y == tiles[i - 1].y)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i - 1].x + curve[j].y, tiles[i - 1].y - curve[j].x));
                    dir = 1;
                }
                else if (tiles[i].x < tiles[i - 1].x)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i - 1].x + curve[j].y, tiles[i - 1].y - curve[j].x));
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i].x + unitScale - curve[j].x, tiles[i].y + curve[j].y));
                }
                else if (tiles[i].x > tiles[i - 1].x)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i].x - curve[j].y, tiles[i].y + unitScale - curve[j].x));
                    dir = 3;
                }
            }
            else if (dir == 1)
            {
                if (tiles[i].x == tiles[i - 1].x)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i - 1] - curve[j]);
                    dir = 2;
                }
                else if (tiles[i].y > tiles[i - 1].y)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i - 1] - curve[j]);
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i].x + curve[j].y, tiles[i].y - unitScale + curve[j].x));
                }
                else if (tiles[i].y < tiles[i - 1].y)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i].x + unitScale - curve[j].x, tiles[i].y + curve[j].y));
                    dir = 0;
                }
            }
            else if (dir == 2)
            {
                if (tiles[i].y == tiles[i - 1].y)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i - 1].x - curve[j].y, tiles[i - 1].y + curve[j].x));
                    dir = 3;
                }
                else if (tiles[i].x > tiles[i - 1].x)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i - 1].x - curve[j].y, tiles[i - 1].y + curve[j].x));
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i].x - unitScale + curve[j].x, tiles[i].y - curve[j].y));
                }
                else if (tiles[i].x < tiles[i - 1].x)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i].x + curve[j].y, tiles[i].y - unitScale + curve[j].x));
                    dir = 1;
                }
            }
            else
            {
                if (tiles[i].x == tiles[i - 1].x)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i - 1] + curve[j]);
                    dir = 0;
                }
                else if (tiles[i].y < tiles[i - 1].y)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(tiles[i - 1] + curve[j]);
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i].x - curve[j].y, tiles[i].y + unitScale - curve[j].x));
                }
                else if (tiles[i].y > tiles[i - 1].y)
                {
                    for (int j = 0; j < curveDetail; j++)
                        points.Enqueue(new float2(tiles[i].x - unitScale + curve[j].x, tiles[i].y - curve[j].y));
                    dir = 2;
                }
            }
        }

        if (tiles[0].x == tiles[tiles.Length - 1].x)
            for (int j = 0; j < curveDetail; j++)
                points.Enqueue(new float2(tiles[0].x - curve[j].y, tiles[0].y + curve[j].x));
        else if (tiles[0].y > tiles[tiles.Length - 1].y)
        {
            if (dir == 2)
                for (int j = 0; j < curveDetail; j++)
                    points.Enqueue(new float2(tiles[tiles.Length - 1].x - curve[j].y, tiles[tiles.Length - 1].y + curve[j].x));
            for (int j = 0; j < curveDetail; j++)
                points.Enqueue(new float2(tiles[0].x - unitScale + curve[j].x, tiles[0].y - curve[j].y));
            for (int j = 0; j < curveDetail; j++)
                points.Enqueue(new float2(tiles[0].x - curve[j].y, tiles[0].y + curve[j].x));
        }
        else if (tiles[0].y < tiles[tiles.Length - 1].y)
        {
            if (dir == 3)
                for (int j = 0; j < curveDetail; j++)
                    points.Enqueue(tiles[tiles.Length - 1] + curve[j]);
            for (int j = 0; j < curveDetail; j++)
                points.Enqueue(new float2(tiles[0].x - curve[j].y, tiles[0].y + unitScale - curve[j].x));
        }

        return BaseOps.QueueToArray(points);
    }

    public static float3[] RandomShapeCtrl(float scale, Unity.Mathematics.Random rdm) // finish this later (if it becomes necessary)
    {
        float3[] points = new float3[17];

        points[0] = new float3(-scale + rdm.NextFloat() / 2 * scale, 0, scale / 2 * rdm.NextFloat() - scale / 2);
        points[1] = new float3(-scale + (scale + points[0].x) * 2 * rdm.NextFloat(), 0, points[0].z + scale / 10 + rdm.NextFloat() * (scale * .9f - points[0].z));
        points[2] = new float3(points[0]);
        points[16] = points[0];

        return points;
    }

    public static float3 Triangulate(float3 a, float3 b, float3 c, float3 point)
    {
        float2 d = LineIntersect(new float2(a.x, a.z), new float2(b.x, b.z), new float2(c.x, c.z), new float2(point.x, point.z));
        float height = FloatLerp(a.y, b.y, BaseOps.Mgtd(d - new float2(a.x, a.z)) / BaseOps.Mgtd(new float2(b.x, b.z) - new float2(a.x, a.z)));

        return new float3(point.x, FloatLerp(height, c.y, BaseOps.Mgtd(new float2(point.x, point.z) - d) / BaseOps.Mgtd(new float2(c.x, c.z) - d)), point.z);
    }

    // hypotenuse orientation is botLeft to topRight
    public static float HeightInTile(float bL, float bR, float tL, float tR, float2 point, bool orientationTop = true)
    {
        float d; float height;
            if (math.lengthsq(point - new float2(0, 1)) < math.lengthsq(point - new float2(1, 0)))
            {
                 d = LineIntersect(float2.zero, new float2(0, 1), new float2(1, 1), point).y;
                 height = FloatLerp(bL, tL, d);
                return FloatLerp(height, tR, BaseOps.Mgtd(new float2(point.x, point.y - d)) / BaseOps.Mgtd(new float2(1, 1 - d)));
            }
             d = LineIntersect(float2.zero, new float2(1, 0), new float2(1, 1), point).x;
             height = FloatLerp(bL, bR, d);
            return FloatLerp(height, tR, BaseOps.Mgtd(point - d) / BaseOps.Mgtd(new float2(1, 1) - d));
    }

    public static Vector2 LineIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        float aMulti;
        float bMulti;
        float aOffset;
        float bOffset;
        float x;

        if (a1.x == a2.x || a1.y == a2.y || b1.x == b2.x || b1.y == b2.y)
        {
            if (a1.x == a2.x && b1.x == b2.x && a1.x != b1.x || a1.y == a2.y && b1.y == b2.y && a1.y != b1.y) return new float2(-1, -1);
            if (a1.x == a2.x && b1.x == b2.x || a1.y == a2.y && b1.y == b2.y) return a1;
            if (a1.x == a2.x && b1.y == b2.y) return new Vector2(a1.x, b1.y);
            if (a1.y == a2.y && b1.x == b2.x) return new Vector2(b1.x, a1.y);

            if (a1.x == a2.x) return new Vector2(a1.x, b2.y + (a1.x - b2.x) * ((b2.y - b1.y) / (b2.x - b1.x)));
            if (b1.x == b2.x) return new Vector2(b1.x, a2.y + (b1.x - a2.x) * ((a2.y - a1.y) / (a2.x - a1.x)));
            if (a1.y == a2.y) return new Vector2(b2.x + (a1.y - b2.y) * ((b2.x - b1.x) / (b2.y - b1.y)), a1.y);
            if (b1.y == b2.y) return new Vector2(a2.x + (b1.y - a2.y) * ((a2.x - a1.x) / (a2.y - a1.y)), b1.y);
        }

        aMulti = (a2.y - a1.y) / (a2.x - a1.x);
        bMulti = (b2.y - b1.y) / (b2.x - b1.x);
        aOffset = a1.y - a1.x * aMulti;
        bOffset = b1.y - b1.x * bMulti;

        x = 1 / (aMulti - bMulti) * (aOffset - bOffset);
        if (x < 0) x *= -1;
        return new Vector2(x, aOffset + x * aMulti);
    }

    public static Vector2 LineIntersectSigned(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        float aMulti;
        float bMulti;
        float aOffset;
        float bOffset;
        float x;

        if (a1.x == a2.x || a1.y == a2.y || b1.x == b2.x || b1.y == b2.y)
        {
            if (a1.x == a2.x && b1.x == b2.x && a1.x != b1.x || a1.y == a2.y && b1.y == b2.y && a1.y != b1.y) return new float2(-1, -1);
            if (a1.x == a2.x && b1.x == b2.x || a1.y == a2.y && b1.y == b2.y) return a1;
            if (a1.x == a2.x && b1.y == b2.y) return new Vector2(a1.x, b1.y);
            if (a1.y == a2.y && b1.x == b2.x) return new Vector2(b1.x, a1.y);

            if (a1.x == a2.x) return new Vector2(a1.x, b2.y + (a1.x - b2.x) * ((b2.y - b1.y) / (b2.x - b1.x)));
            if (b1.x == b2.x) return new Vector2(b1.x, a2.y + (b1.x - a2.x) * ((a2.y - a1.y) / (a2.x - a1.x)));
            if (a1.y == a2.y) return new Vector2(b2.x + (a1.y - b2.y) * ((b2.x - b1.x) / (b2.y - b1.y)), a1.y);
            if (b1.y == b2.y) return new Vector2(a2.x + (b1.y - a2.y) * ((a2.x - a1.x) / (a2.y - a1.y)), b1.y);
        }

        if (a2.x - a1.x > 0) aMulti = (a2.y - a1.y) / (a2.x - a1.x);
        else aMulti = -((a2.y - a1.y) / (a2.x - a1.x));
        if (b2.x - b1.x > 0) bMulti = (b2.y - b1.y) / (b2.x - b1.x);
        else bMulti = -((b2.y - b1.y) / (b2.x - b1.x));
        if (aMulti > 0) aOffset = a1.y - a1.x * aMulti;
        else aOffset = a1.y - a1.x * -aMulti;
        if (bMulti > 0) bOffset = b1.y - b1.x * bMulti;
        else bOffset = b1.y - b1.x * -bMulti;

        x = (bOffset - aOffset) / (aMulti - bMulti);
        if (x < 0) x *= -1;
        if (a2.x - a1.x < 0) return new Vector2(-x, aOffset + x * aMulti);
        return new Vector2(x, aOffset + x * aMulti);
    }

    public static float2 LineIntersect(float2 a1, float2 a2, float2 b1, float2 b2)
    {
        float aMulti;
        float bMulti;
        float aOffset;
        float bOffset;
        float x;

        if (a1.x == a2.x || a1.y == a2.y || b1.x == b2.x || b1.y == b2.y)
        {
            if (a1.x == a2.x && b1.x == b2.x && a1.x != b1.x || a1.y == a2.y && b1.y == b2.y && a1.y != b1.y) return new float2(-1, -1);
            if (a1.x == a2.x && b1.x == b2.x || a1.y == a2.y && b1.y == b2.y) return a1;
            if (a1.x == a2.x && b1.y == b2.y) return new float2(a1.x, b1.y);
            if (a1.y == a2.y && b1.x == b2.x) return new float2(b1.x, a1.y);

            if (a1.x == a2.x) return new float2(a1.x, b2.y + (a1.x - b2.x) * ((b2.y - b1.y) / (b2.x - b1.x)));
            if (b1.x == b2.x) return new float2(b1.x, a2.y + (b1.x - a2.x) * ((a2.y - a1.y) / (a2.x - a1.x)));
            if (a1.y == a2.y) return new float2(b2.x + (a1.y - b2.y) * ((b2.x - b1.x) / (b2.y - b1.y)), a1.y);
            if (b1.y == b2.y) return new float2(a2.x + (b1.y - a2.y) * ((a2.x - a1.x) / (a2.y - a1.y)), b1.y);
        }

        aMulti = (a2.y - a1.y) / (a2.x - a1.x);
        bMulti = (b2.y - b1.y) / (b2.x - b1.x);
        aOffset = a1.y - a1.x * aMulti;
        bOffset = b1.y - b1.x * bMulti;

        x = 1 / (aMulti - bMulti) * (aOffset - bOffset);
        if (x < 0) x *= -1;
        return new float2(x, aOffset + x * aMulti);
    }

    public static Vector3 PlaneIntersect(float plane, Vector3 origin, Vector3 dir)
    {
        if (dir.y == 0) return origin;
        float dist = (origin.y - plane) / dir.y;
        if (dist < 0) dist *= -1;
        return new Vector3(origin.x + dir.x * dist, plane, origin.z + dir.z * dist);
    }

    public static Vector2 RadToVector2(float radian)
    {
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }

    public static Vector2 DegToVector2(float degree)
    {
        return RadToVector2(degree * Mathf.Deg2Rad);
    }
}

