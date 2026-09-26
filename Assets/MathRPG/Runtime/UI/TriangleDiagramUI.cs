using UnityEngine;

namespace MathRPG
{
    public static class TriangleDiagramUI
    {
        public static void Draw(TrainingUI ui, Transform parent, TriangleDiagramData data)
        {
            if (data == null) return;
            DrawTriangle(ui, parent, data.showPair ? 205 : 375, data.leftTop, data.leftBottomLeft,
                data.leftBottomRight, data.leftSideLeft, data.leftSideRight, data.leftBase);
            if (data.showPair)
                DrawTriangle(ui, parent, 550, data.rightTop, data.rightBottomLeft,
                    data.rightBottomRight, data.rightSideLeft, data.rightSideRight, data.rightBase);
            ui.Label(parent, "도식 · 축척과 무관", 30, 67, 180, 20, 12);
        }

        static void DrawTriangle(TrainingUI ui, Transform parent, float x, string top, string left,
            string right, string sideLeft, string sideRight, string bottom)
        {
            var a = new Vector2(x, 101);
            var b = new Vector2(x - 73, 171);
            var c = new Vector2(x + 73, 171);
            Line(ui, parent, a, b); Line(ui, parent, b, c); Line(ui, parent, c, a);
            ui.Label(parent, top, x - 80, 78, 160, 22, 16, alignment: TextAnchor.MiddleCenter);
            ui.Label(parent, left, x - 158, 167, 150, 24, 15, alignment: TextAnchor.MiddleCenter);
            ui.Label(parent, right, x + 8, 167, 150, 24, 15, alignment: TextAnchor.MiddleCenter);
            ui.Label(parent, sideLeft, x - 116, 122, 65, 24, 16, alignment: TextAnchor.MiddleCenter);
            ui.Label(parent, sideRight, x + 51, 122, 65, 24, 16, alignment: TextAnchor.MiddleCenter);
            ui.Label(parent, bottom, x - 40, 181, 80, 24, 16, alignment: TextAnchor.MiddleCenter);
        }

        static void Line(TrainingUI ui, Transform parent, Vector2 start, Vector2 end)
        {
            var delta = end - start;
            var line = ui.Block(parent, "Triangle edge", start.x, start.y, delta.magnitude, 2, new Color(.46f, .9f, .75f));
            line.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }
}
