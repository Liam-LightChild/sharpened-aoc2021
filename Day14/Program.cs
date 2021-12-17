using System.Text.RegularExpressions;

if (args.Length < 1) throw new InvalidOperationException("need single argument containing file path to input");

List<char> parts;
await using var inputStream = new FileStream(args[0], FileMode.Open);
using var input = new StreamReader(inputStream);

parts = (await input.ReadLineAsync())!.ToCharArray().ToList();
var readLine = input.ReadLineAsync();

Dictionary<(char, char), char> replacements = new();
SemaphoreSlim semaphoreSlim = new(1, 1);

List<Task> tasks = new();

while (true)
{
    var line = await readLine;
    if (line == null) break;
    readLine = input.ReadLineAsync();
    if (!line.Any()) continue;

    tasks.Add(Task.Run(delegate
    {
        var match = Regex.Match(line, "^(?<pattern>..) -> (?<insertion>.)$");
        var p = match.Groups["pattern"].ValueSpan.ToArray();
        var r = match.Groups["insertion"].ValueSpan[0];
        semaphoreSlim.Wait();
        try
        {
            replacements[(p[0], p[1])] = r;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }));
}

Task.WaitAll(tasks.ToArray());
tasks.Clear();

Console.WriteLine($"{string.Join(", ", parts)} => {string.Join(", ", replacements)}");

for (int j = 0; j < 10; j++)
{
    for (var i = 0; i < parts.Count - 1; i++)
    {
        var a = new[] {parts[i], parts[i + 1]};

        foreach (var ((p1, p2), value) in replacements)
        {
            if (a[0] == p1 && a[1] == p2)
            {
                parts.Insert(++i, value);
                break;
            }
        }
    }

    // Console.WriteLine($"{string.Join(", ", parts)} => {string.Join(", ", replacements)}");
}

Dictionary<char, int> counts = new();
foreach (var c in parts.Distinct())
{
    counts[c] = parts.Count(v => v == c);
}

(char Char, int Count) currentHighest = (char.MinValue, int.MinValue), currentLowest = (char.MinValue, int.MaxValue);

foreach (var (c, count) in counts)
{
    if (count > currentHighest.Count) currentHighest = (c, count);
    if (count < currentLowest.Count) currentLowest = (c, count);
}

Console.WriteLine($"{currentHighest.Count} - {currentLowest.Count} = {currentHighest.Count - currentLowest.Count}");

// todo bad bad bad
for (int j = 0; j < 30; j++)
{
    await Console.Out.WriteAsync($"iter {j+10}");

    List<(int, char)> locations = new();

    for (var i = 0; i < parts.Count - 1; i++)
    {
        var k = i;
        tasks.Add(Task.Run(delegate
        {
            var a = new[] {parts[k], parts[k + 1]};

            foreach (var ((p1, p2), value) in replacements)
            {
                if (a[0] == p1 && a[1] == p2)
                {
                    semaphoreSlim.Wait();
                    locations.Add((k+1, value));
                    semaphoreSlim.Release();
                    break;
                }
            }
        }));
    }
    
    await Console.Out.WriteAsync("; waiting for tasks");
    Task.WaitAll(tasks.ToArray());
    
    await Console.Out.WriteAsync("; sorting");
    locations.Sort();
    locations.Reverse();
    
    await Console.Out.WriteAsync("; inserting");
    foreach (var (i, c) in locations)
    {
        parts.Insert(i, c);
    }

    await Console.Out.WriteLineAsync("; finished");
}

Console.WriteLine($"{currentHighest.Count} - {currentLowest.Count} = {currentHighest.Count - currentLowest.Count}");
