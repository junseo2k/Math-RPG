using System;
using UnityEngine;

namespace MathRPG
{
    public enum GeometryFigureKind { Polygon, CircleTangent, Cylinder }
    [Serializable]
    public sealed class GeometryDiagramData
    {
        public GeometryFigure[] figures;
    }

    [Serializable]
    public sealed class GeometryFigure
    {
        public GeometryFigureKind kind;
        public string radiusLabel;
        public string heightLabel;
        // Vertices use a unit square, with y increasing downward like the quiz UI.
        public Vector2[] vertices;
        public string[] vertexLabels;
        public string[] edgeLabels;
        public int rightAngleIndex = -1;
    }
}
