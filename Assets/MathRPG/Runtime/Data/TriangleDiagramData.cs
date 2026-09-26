using System;

namespace MathRPG
{
    [Serializable]
    public sealed class TriangleDiagramData
    {
        public bool showPair;
        public string leftTop, leftBottomLeft, leftBottomRight;
        public string leftSideLeft, leftSideRight, leftBase;
        public string rightTop, rightBottomLeft, rightBottomRight;
        public string rightSideLeft, rightSideRight, rightBase;
    }
}
