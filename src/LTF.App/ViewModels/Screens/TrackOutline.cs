using System;
using System.Collections.Generic;
using Avalonia;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The schematic track outline the race-weekend map draws its car markers along (M23e). A single generic loop
/// — the same shape the design mockup ships (<c>design/mockups/ui.dc.html</c>), reused for every circuit, since
/// the domain carries no per-track geometry. The view renders the outline from the identical SVG path string;
/// this type flattens that path once into a poly-line with cumulative arc-length so a marker can be placed at a
/// given fraction of the way round the lap (<see cref="PointAtFraction"/>). Pure geometry — no engine data, no
/// randomness — so it is a display-only projection of the recorded telemetry gaps and never touches the race.
/// </summary>
internal static class TrackOutline
{
    // The coordinate extent of the outline (matches the mockup viewBox), so the view's canvas can size to it.
    public const double Width = 840;
    public const double Height = 560;

    private static readonly IReadOnlyList<Point> Points = Flatten();
    private static readonly double[] Cumulative = BuildCumulative(Points);

    /// <summary>The point a given fraction (0..1, wrapping) of the way round the lap from the start/finish line,
    /// in the outline's coordinate space. A car <c>g</c> laps' worth of time behind the leader sits at
    /// <c>PointAtFraction(-g)</c> — just short of the line by that fraction.</summary>
    public static Point PointAtFraction(double fraction)
    {
        var f = fraction - Math.Floor(fraction); // wrap into [0, 1)
        var total = Cumulative[^1];
        var target = f * total;

        var i = 1;
        while (i < Cumulative.Length - 1 && Cumulative[i] < target)
        {
            i++;
        }

        var segment = Cumulative[i] - Cumulative[i - 1];
        var t = segment > 1e-9 ? (target - Cumulative[i - 1]) / segment : 0.0;
        var a = Points[i - 1];
        var b = Points[i];
        return new Point(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
    }

    private static double[] BuildCumulative(IReadOnlyList<Point> pts)
    {
        var cum = new double[pts.Count];
        for (var i = 1; i < pts.Count; i++)
        {
            var dx = pts[i].X - pts[i - 1].X;
            var dy = pts[i].Y - pts[i - 1].Y;
            cum[i] = cum[i - 1] + Math.Sqrt((dx * dx) + (dy * dy));
        }

        return cum;
    }

    // Flatten the mockup's outline path (moves, lines and cubic Béziers) into a dense poly-line. The segment
    // list is transcribed verbatim from design/mockups/ui.dc.html so the flattened points lie on the exact
    // curve the view renders from the same path string.
    private static IReadOnlyList<Point> Flatten()
    {
        var pts = new List<Point>();
        var cur = new Point(110, 400);
        pts.Add(cur);

        Point Line(double x, double y)
        {
            cur = new Point(x, y);
            pts.Add(cur);
            return cur;
        }

        Point Cubic(double c1x, double c1y, double c2x, double c2y, double x, double y)
        {
            var p0 = cur;
            var c1 = new Point(c1x, c1y);
            var c2 = new Point(c2x, c2y);
            var p1 = new Point(x, y);
            const int steps = 18;
            for (var k = 1; k <= steps; k++)
            {
                var t = (double)k / steps;
                var u = 1 - t;
                var bx = (u * u * u * p0.X) + (3 * u * u * t * c1.X) + (3 * u * t * t * c2.X) + (t * t * t * p1.X);
                var by = (u * u * u * p0.Y) + (3 * u * u * t * c1.Y) + (3 * u * t * t * c2.Y) + (t * t * t * p1.Y);
                pts.Add(new Point(bx, by));
            }

            cur = p1;
            return cur;
        }

        Line(110, 170);
        Cubic(110, 136, 132, 116, 166, 116);
        Line(260, 116);
        Cubic(284, 116, 298, 130, 308, 152);
        Line(330, 200);
        Cubic(342, 224, 360, 232, 384, 226);
        Line(466, 204);
        Cubic(496, 196, 516, 212, 522, 240);
        Line(548, 340);
        Cubic(556, 372, 580, 384, 610, 376);
        Line(672, 360);
        Cubic(708, 350, 732, 372, 732, 406);
        Cubic(732, 438, 710, 456, 678, 456);
        Line(260, 456);
        Cubic(224, 456, 202, 436, 202, 404);
        Cubic(202, 378, 180, 362, 154, 368);
        Line(130, 374);
        Cubic(116, 378, 110, 388, 110, 400); // close back to the start/finish point

        return pts;
    }
}
