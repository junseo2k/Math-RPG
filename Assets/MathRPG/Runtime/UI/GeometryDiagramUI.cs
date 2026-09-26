using UnityEngine;

namespace MathRPG
{
    public static class GeometryDiagramUI
    {
        static readonly Color LineColor = new Color(.5f, .86f, .96f);

        public static void Draw(TrainingUI ui, Transform parent, GeometryDiagramData data)
        {
            if (data == null || data.figures == null) return;
            int count = Mathf.Min(2, data.figures.Length);
            ui.Label(parent, "도식 · 축척과 무관", 30, 76, 220, 20, 12);
            for (int f = 0; f < count; f++)
            {
                var figure = data.figures[f];
                if (figure == null) continue;
                float centerX = count == 1 ? 380 : 210 + f * 345;
                if (figure.kind == GeometryFigureKind.CircleTangent)
                { DrawCircle(ui, parent, centerX, figure); continue; }
                if (figure.kind == GeometryFigureKind.Cylinder)
                { DrawCylinder(ui, parent, centerX, figure); continue; }
                if (figure.vertices == null || figure.vertices.Length < 3) continue;
                var points = new Vector2[figure.vertices.Length];
                var center = Vector2.zero;
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new Vector2(centerX - 85 + figure.vertices[i].x * 170, 111 + figure.vertices[i].y * 67);
                    center += points[i] / points.Length;
                }
                for (int i = 0; i < points.Length; i++)
                {
                    var start = points[i];
                    var end = points[(i + 1) % points.Length];
                    Line(ui, parent, start, end);
                    Label(ui, parent, Get(figure.vertexLabels, i), start + (start - center).normalized * 20);
                    var mid = (start + end) * .5f;
                    Label(ui, parent, Get(figure.edgeLabels, i), mid + (mid - center).normalized * 17);
                }
                int corner = figure.rightAngleIndex;
                if (corner < 0 || corner >= points.Length) continue;
                var origin = points[corner];
                var a = (points[(corner + points.Length - 1) % points.Length] - origin).normalized * 10;
                var b = (points[(corner + 1) % points.Length] - origin).normalized * 10;
                Line(ui, parent, origin + a, origin + a + b);
                Line(ui, parent, origin + a + b, origin + b);
            }
        }

        static void DrawCircle(TrainingUI ui, Transform parent, float x, GeometryFigure figure)
        {
            var center = new Vector2(x, 147);
            Ellipse(ui, parent, center, 40, 40);
            Line(ui, parent, center, center + Vector2.right * 40);
            Line(ui, parent, new Vector2(x + 40, 98), new Vector2(x + 40, 196));
            Label(ui, parent, "O", center + new Vector2(-12, 0));
            Label(ui, parent, "P", center + new Vector2(56, 0));
            Label(ui, parent, figure.radiusLabel, center + new Vector2(20, -17));
            Label(ui, parent, "접선", new Vector2(x + 78, 103));
        }

        static void DrawCylinder(TrainingUI ui, Transform parent, float x, GeometryFigure figure)
        {
            Ellipse(ui, parent, new Vector2(x, 122), 58, 13);
            Ellipse(ui, parent, new Vector2(x, 178), 58, 13);
            Line(ui, parent, new Vector2(x - 58, 122), new Vector2(x - 58, 178));
            Line(ui, parent, new Vector2(x + 58, 122), new Vector2(x + 58, 178));
            Line(ui, parent, new Vector2(x, 122), new Vector2(x + 58, 122));
            Label(ui, parent, "r=" + figure.radiusLabel, new Vector2(x + 20, 99));
            Label(ui, parent, "h=" + figure.heightLabel, new Vector2(x + 93, 150));
        }

        static void Ellipse(TrainingUI ui, Transform parent, Vector2 center, float rx, float ry)
        {
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 2 * Mathf.PI / segments;
                float b = (i + 1) * 2 * Mathf.PI / segments;
                Line(ui, parent, center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry),
                    center + new Vector2(Mathf.Cos(b) * rx, Mathf.Sin(b) * ry));
            }
        }

        static string Get(string[] labels, int index) => labels != null && index < labels.Length ? labels[index] : "";
        static void Label(TrainingUI ui, Transform parent, string value, Vector2 position)
        {
            if (!string.IsNullOrEmpty(value))
                ui.Label(parent, value, position.x - 48, position.y - 12, 96, 24, 15, alignment: TextAnchor.MiddleCenter);
        }
        static void Line(TrainingUI ui, Transform parent, Vector2 start, Vector2 end)
        {
            var delta = end - start;
            var line = ui.Block(parent, "Geometry edge", start.x, start.y, delta.magnitude, 2, LineColor);
            line.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }
}
