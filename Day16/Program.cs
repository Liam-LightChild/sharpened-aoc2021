using System.Globalization;

const string inputString = "E20D41802B2984BD00540010F82D09E35880350D61A41D3004E5611E585F40159ED7AD7C90CF6BD6BE49C802DEB00525272CC1927752698693DA7C70029C0081002140096028C5400F6023C9C00D601ED88070070030005C2201448400E400F40400C400A50801E20004C1000809D14700B67676EE661137ADC64FF2BBAD745B3F2D69026335E92A0053533D78932A9DFE23AC7858C028920A973785338832CFA200F47C81D2BBBC7F9A9E1802FE00ACBA44F4D1E775DDC19C8054D93B7E72DBE7006AA200C41A8510980010D8731720CB80132918319804738AB3A8D3E773C4A4015A498E680292B1852E753E2B29D97F0DE6008CB3D4D031802D2853400D24DEAE0137AB8210051D24EB600844B95C56781B3004F002B99D8F635379EDE273AF26972D4A5610BA51004C12D1E25D802F32313239377B37100105343327E8031802B801AA00021D07231C2F10076184668693AC6600BCD83E8025231D752E5ADE311008A4EA092754596C6789727F069F99A4645008247D2579388DCF53558AE4B76B257200AAB80107947E94789FE76E36402868803F0D62743F00043A1646288800084C3F8971308032996A2BD8023292DF8BE467BB3790047F2572EF004A699E6164C013A007C62848DE91CC6DB459B6B40087E530AB31EE633BD23180393CBF36333038E011CBCE73C6FB098F4956112C98864EA1C2801D2D0F319802D60088002190620E479100622E4358952D84510074C0188CF0923410021F1CE1146E3006E3FC578EE600A4B6C4B002449C97E92449C97E92459796EB4FF874400A9A16100A26CEA6D0E5E5EC8841C9B8FE37109C99818023A00A4FD8BA531586BB8B1DC9AE080293B6972B7FA444285CC00AE492BC910C1697B5BDD8425409700562F471201186C0120004322B42489A200D4138A71AA796D00374978FE07B2314E99BFB6E909678A0";
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
