﻿namespace OpenMEPSandbox.Geometry.Abstract;

public class BoundingBox
{
    private BoundingBox()
    {
    }

    /// <summary>
    /// Create a bounding box from a list of points
    /// </summary>
    /// <param name="points">the list point to create new bounding box</param>
    /// <returns name="boundingBox">new boundingBox from list point</returns>
    /// <example>
    /// ![](../OpenMEPPage/geometry/dyn/pic/BoundingBox.ByPoints.png)
    /// [BoundingBox.ByPoints.dyn](../OpenMEPPage/geometry/dyn/BoundingBox.ByPoints.dyn)
    ///</example>
    public static Autodesk.DesignScript.Geometry.BoundingBox ByPoints(List<Autodesk.DesignScript.Geometry.Point> points)
    {
        if (points.Count == 0)
        {
            throw new ArgumentNullException($"Points is empty");
        }

        if (points.Count == 1)
        {
            throw new ArgumentNullException($"Points require more than 1 point");
        }

        Autodesk.DesignScript.Geometry.Point minPoint = points[0];
        Autodesk.DesignScript.Geometry.Point maxPoint = points[0];
        foreach (var point in points)
        {
            if (point.X < minPoint.X)
            {
                minPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(point.X, minPoint.Y, minPoint.Z);
            }

            if (point.Y < minPoint.Y)
            {
                minPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(minPoint.X, point.Y, minPoint.Z);
            }

            if (point.Z < minPoint.Z)
            {
                minPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(minPoint.X, minPoint.Y, point.Z);
            }

            if (point.X > maxPoint.X)
            {
                maxPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(point.X, maxPoint.Y, maxPoint.Z);
            }

            if (point.Y > maxPoint.Y)
            {
                maxPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(maxPoint.X, point.Y, maxPoint.Z);
            }

            if (point.Z > maxPoint.Z)
            {
                maxPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(maxPoint.X, maxPoint.Y, point.Z);
            }
        }
        return Autodesk.DesignScript.Geometry.BoundingBox.ByCorners(minPoint, maxPoint);
    }
    
    /// <summary>
    /// Get center point of bounding box
    /// </summary>
    /// <param name="boundingBox">the boundingBox need get point center</param>
    /// <returns name="point">center point of boundingBox</returns>
    /// <example>
    /// ![](../OpenMEPPage/geometry/dyn/pic/BoundingBox.Center.png)
    /// [BoundingBox.Center.dyn](../OpenMEPPage/geometry/dyn/BoundingBox.Center.dyn)
    ///</example>
    public static Autodesk.DesignScript.Geometry.Point Center(Autodesk.DesignScript.Geometry.BoundingBox boundingBox)
    {
        Autodesk.DesignScript.Geometry.Point maxPoint = boundingBox.MaxPoint;
        Autodesk.DesignScript.Geometry.Point minPoint = boundingBox.MinPoint;
        return Autodesk.DesignScript.Geometry.Point.ByCoordinates((maxPoint.X + minPoint.X) / 2, (maxPoint.Y + minPoint.Y) / 2, (maxPoint.Z + minPoint.Z) / 2);
    }

    /// <summary>
    /// Get 4 points corners of bounding box
    /// </summary>
    /// <param name="boundingBox"></param>
    /// <returns name="points">list point corner of the boundingBox</returns>
    /// /// <example>
    /// ![](../OpenMEPPage/geometry/dyn/pic/BoundingBox.Corners.png)
    /// [BoundingBox.Corners.dyn](../OpenMEPPage/geometry/dyn/BoundingBox.Corners.dyn)
    ///</example>
    public static List<Autodesk.DesignScript.Geometry.Point> Corners(Autodesk.DesignScript.Geometry.BoundingBox boundingBox)
    {
         var minPoint = boundingBox.MinPoint;
         var maxPoint = boundingBox.MaxPoint;
         List<Autodesk.DesignScript.Geometry.Point> corners = new List<Autodesk.DesignScript.Geometry.Point>();
            corners.Add(minPoint);
            corners.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(minPoint.X, minPoint.Y, maxPoint.Z));
            corners.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(minPoint.X, maxPoint.Y, minPoint.Z));
            corners.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(minPoint.X, maxPoint.Y, maxPoint.Z));
            corners.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(maxPoint.X, minPoint.Y, minPoint.Z));
            corners.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(maxPoint.X, minPoint.Y, maxPoint.Z));
            corners.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(maxPoint.X, maxPoint.Y, minPoint.Z));
            corners.Add(maxPoint);
            return corners;
    }
}