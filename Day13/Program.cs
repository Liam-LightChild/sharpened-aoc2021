using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) throw new InvalidOperationException("need single argument containing file path to input");

List<Point> points = new(); // `Point` at bottom

var first = true;

foreach (var line in File.ReadLines(args[0]))
{
    if (Regex.IsMatch(line, @"^\d+,\d+$"))
    {
        var parts = line.Split(',').Select(int.Parse).ToArray();
        points.Add(new Point { X = parts[0], Y = parts[1] });
    }
    else if (line.StartsWith("fold along"))
    {
        var m = Regex.Match(line, @"^fold along (?<axis>.)=(?<distance>\d+)$");
        var axis = m.Groups["axis"].Value.ToCharArray()[0];
        switch (axis)
        {
            case 'x':
                new XFold {Value = int.Parse(m.Groups["distance"].Value)}.Perform(ref points);
                break;
            case 'y':
                new YFold {Value = int.Parse(m.Groups["distance"].Value)}.Perform(ref points);
                break;
        }
        points = points.Distinct().ToList();
        if (first)
        {
            Console.WriteLine($"First iter: {points.Count}");
            first = false;
        }
    }
}

int maxX = 0, maxY = 0;
    
foreach (var point in points)
{
    if (point.X > maxX) maxX = point.X;
    if (point.Y > maxY) maxY = point.Y;
}

for (var i = 0; i <= maxY; i++)
{
    for (var j = 0; j <= maxX; j++) Console.Write(points.Any(point => point.X == j && point.Y == i) ? '#' : '.');

    Console.WriteLine();
}

[DebuggerDisplay("{X}, {Y}")]
struct Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public bool Equals(Point other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Point other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}

interface IFold
{
    void Perform(ref List<Point> points);
}

readonly struct XFold : IFold
{
    public int Value { get; init; }
    
    public void Perform(ref List<Point> points)
    {
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (p.X < Value) continue;
            p.X += (int)MathF.Round(((float)Value - points[i].X) * 2f);
            points[i] = p;
        }
    }
}

readonly struct YFold : IFold
{
    public int Value { get; init; }
    
    public void Perform(ref List<Point> points)
    {
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (p.Y < Value) continue;
            p.Y += (int)MathF.Round(((float)Value - points[i].Y) * 2f);
            points[i] = p;
        }
    }
}
