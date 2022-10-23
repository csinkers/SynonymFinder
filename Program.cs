using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SynonymFinder;

class Entry
{
    [JsonPropertyName("word")] public string Word { get; set; }
    [JsonPropertyName("key")] public string Key { get; set; }
    [JsonPropertyName("pos")] public string Pos { get; set; }
    [JsonPropertyName("synonyms")] public List<string> Synonyms { get; set; }
}

public class Program
{
    const string ThesaurusFilename = "en_thesaurus.jsonl";

    public static void Main()
    {
        var curDir = Assembly.GetExecutingAssembly().Location;

        while (!string.IsNullOrEmpty(curDir) && !File.Exists(Path.Combine(curDir, ThesaurusFilename)))
            curDir = Path.GetDirectoryName(curDir);

        if (string.IsNullOrEmpty(curDir))
        {
            Console.WriteLine($"Could not find thesaurus file {ThesaurusFilename}");
            return;
        }

        var json = File.ReadAllText(ThesaurusFilename);
        var jsonEntries = JsonSerializer.Deserialize<List<Entry>>(json);
        if (jsonEntries == null)
        {
            Console.WriteLine($"Could not deserialise thesaurus file {ThesaurusFilename}");
            return;
        }

        var dict = new Dictionary<string, List<string>>();
        foreach (var entry in jsonEntries)
        {
            var lower = entry.Word.ToLowerInvariant();
            if (!dict.TryGetValue(lower, out var synonyms))
            {
                synonyms = new List<string> { entry.Word };
                dict[lower] = synonyms;
            }

            foreach(var synonym in entry.Synonyms)
                synonyms.Add(synonym);
        }

        for (;;)
        {
            Console.Write("Enter title (empty to quit): ");
            var title = Console.ReadLine();
            if (string.IsNullOrEmpty(title))
                break;

            Console.WriteLine();
            var results = GetResults(title, dict);
            Shuffle(results);
            for (int i = 0; i < 50; i++)
                Console.WriteLine(results[i]);
        }
    }

    static string[] GetResults(string title, Dictionary<string, List<string>> synonymDictionary)
    {
        var parts = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var partSynonyms = new List<string>[parts.Length];
        var divisors = new int[parts.Length];

        int curMod = 1;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            var lower = part.ToLowerInvariant();
            partSynonyms[index] = new List<string> { part };

            if (!synonymDictionary.TryGetValue(lower, out var synonyms))
            {
                divisors[index] = curMod;
                continue;
            }

            foreach (var synonym in synonyms)
                partSynonyms[index].Add(synonym);

            divisors[index] = curMod;
            curMod *= partSynonyms[index].Count;
        }

        var results = new string[curMod];
        var sb = new StringBuilder();
        for (int i = 0; i < curMod; i++)
        {
            sb.Clear();
            bool first = true;
            for (var j = 0; j < partSynonyms.Length; j++)
            {
                var synonyms = partSynonyms[j];
                if (!first)
                    sb.Append(' ');

                int k = (i / divisors[j]) % synonyms.Count;
                sb.Append(synonyms[k]);
                first = false;
            }

            results[i] = sb.ToString();
        }

        return results;
    }

    static readonly Random Rng = new();
    static void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
