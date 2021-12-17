if (args.Length < 1) throw new InvalidOperationException("need single argument containing file path to input");

List<Point> points; // `Point` at bottom

foreach (var line in File.ReadLines(args[0]))
{
    
}

struct Point
{
    public int X { get; init; }
    public int Y { get; init; }
}

interface IFold
{
    void Perform(ref List<Point> points);
}

struct XFold
{
    public int Value { get; init; }
}
