using AStarNavigator;
using AStarNavigator.Algorithms;
using AStarNavigator.Providers;

if (args.Length < 1) throw new InvalidOperationException("need single argument containing file path to input");

await using var input = new FileStream(args[0], FileMode.Open);
var reader = new StreamReader(input);
byte[][] values = (await reader.ReadToEndAsync()).Split('\n')
    .Select(s => s.ToCharArray()
        .Select(c => byte.Parse(c.ToString()))
        .ToArray())
    .ToArray();

var nav = new TileNavigator(
    new EmptyBlockedProvider(),
    new DiagonalNeighborProvider(),
    new PythagorasAlgorithm(),
    new ManhattanHeuristicAlgorithm()
);

var sum = AStar(new Point {X = 0, Y = 0}, new Point
    {
        X = values.Length,
        Y = values[0].Length
    })
    .Sum(p => values[p.X][p.Y]);



struct Point : IEquatable<Point>
{
    public int X, Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

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

    public static bool operator ==(Point left, Point right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Point left, Point right)
    {
        return !left.Equals(right);
    }
}
