using System.Globalization;

if (args.Length < 1) throw new InvalidOperationException("need input file path passed as argument");
var inputString = File.ReadAllText(args[0]);
var values = ParseBits(inputString).ToArray().AsSpan();

List<Packet> packets = new();

while (!values.IsEmpty)
{
    packets.Add(ParsePacket(ref values));
    if (values.ToArray().All(b => !b)) break;
}

var sum = packets.Sum(GetVersionSum);
Console.WriteLine($"Version Sum: {sum}");
var value = packets.Single().Value;
Console.WriteLine($"Value: {value}");

int GetVersionSum(Packet p) => p.Version + (p.SubPackets ?? Array.Empty<Packet>()).Sum(GetVersionSum);

IEnumerable<bool> ParseBits(string input)
{
    foreach (var c in input.ToCharArray())
    {
        var i = int.Parse(c.ToString(), NumberStyles.HexNumber);
        yield return (i & 0b1000) > 0;
        yield return (i & 0b0100) > 0;
        yield return (i & 0b0010) > 0;
        yield return (i & 0b0001) > 0;
    }
}

ulong ReadBits(ReadOnlySpan<bool> v)
{
    ulong r = 0;
    
    foreach (var b in v)
    {
        r <<= 1;
        r |= (uint) (b ? 1 : 0);
    }

    return r;
}

ulong Read(ref Span<bool> s, int bits)
{
    var v = s[..bits];
    s = s[bits..];
    return ReadBits(v);
}

bool ReadBit(ref Span<bool> s)
{
    var v = s[..1][0];
    s = s[1..];
    return v;
}

// ReSharper disable once VariableHidesOuterVariable
Packet ParsePacket(ref Span<bool> values)
{
    var ver = Read(ref values, 3);
    var type = Read(ref values, 3);
    var packet = new Packet {Version = (int) ver, Type = (ValueType) type};

    // ReSharper disable once VariableHidesOuterVariable
    void ReadSub(ref Span<bool> values)
    {
        List<Packet> sub = new();
        var isCount = ReadBit(ref values);
            
        if (!isCount)
        {
            var length = Read(ref values, 15);
            var piece = values[..(int) length];
            values = values[(int) length..];

            while (!piece.IsEmpty)
            {
                sub.Add(ParsePacket(ref piece));
            }
        }
        else
        {
            var count = Read(ref values, 11);
                
            for (int i = 0; i < (int)count; i++)
            {
                sub.Add(ParsePacket(ref values));
            }
        }

        packet.SubPackets = sub.ToArray();
    }
    
    switch (packet.Type)
    {
        case ValueType.Literal:
            packet.LiteralValue = 0;
            bool bit;
            
            do
            {
                bit = ReadBit(ref values);
                packet.LiteralValue <<= 4;
                packet.LiteralValue |= Read(ref values, 4);
            } while (bit);
            
            break;
        case ValueType.Sum:
        case ValueType.Product:
        case ValueType.Minimum:
        case ValueType.Maximum:
        case ValueType.GreaterThan:
        case ValueType.LessThan:
        case ValueType.EqualTo:
        default:
            ReadSub(ref values);
            break;
    }

    return packet;
}

enum ValueType : int
{
    Sum = 0,
    Product = 1,
    Minimum = 2,
    Maximum = 3,
    Literal = 4,
    GreaterThan = 5,
    LessThan = 6,
    EqualTo = 7
}

struct Packet
{
    public int Version { get; init; }
    public ValueType Type { get; init; }
    public ulong? LiteralValue { get; set; }
    public Packet[]? SubPackets { get; set; }

    public ulong Value => Type switch
    {   
        ValueType.Sum => SubPackets!.Aggregate(0ul, (v, p) => v + p.Value),
        ValueType.Product => SubPackets!.Aggregate(1ul, (v, p) => v * p.Value),
        ValueType.Minimum => SubPackets!.Select(p => p.Value).Min(),
        ValueType.Maximum => SubPackets!.Select(p => p.Value).Max(),
        ValueType.Literal => LiteralValue!.Value,
        ValueType.GreaterThan => SubPackets![0].Value > SubPackets![1].Value ? 1ul : 0ul,
        ValueType.LessThan => SubPackets![0].Value < SubPackets![1].Value ? 1ul : 0ul,
        ValueType.EqualTo => SubPackets![0].Value == SubPackets![1].Value ? 1ul : 0ul,
        _ => throw new ArgumentOutOfRangeException()
    };
}
