using System.Text.RegularExpressions;
using Cocona;
using ResxTranslator;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var builder = CoconaApp.CreateBuilder();
var app = builder.Build();

app.AddCommand(async (string dir, string? to = null, string? from = null, bool overwrite = false, string? apiKey = null) =>
{
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        apiKey = Environment.GetEnvironmentVariable("API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("apiKey not set via args and env API_KEY is also not set");
            return 1;
        }
    }

    if (!Directory.Exists(dir))
    {
        Console.WriteLine($"Directory '{dir}' does not exist.");
        return 1;
    }

    var allFiles = Directory.GetFiles(dir, "*.resx", SearchOption.AllDirectories);
    if (allFiles.Length == 0)
    {
        Console.WriteLine($"No .resx files found in '{dir}'.");
        return 1;
    }


    string? fromFilePath;
    if (string.IsNullOrWhiteSpace(from))
    {
        // Find the default .resx file
        var regex = new Regex(@"^[a-zA-Z_]+\.resx$");
        fromFilePath = allFiles.FirstOrDefault(x => regex.IsMatch(Path.GetFileName(x)));
    }
    else
    {
        fromFilePath = allFiles.FirstOrDefault(f => f.EndsWith($".{from}.resx"));
    }

    if (string.IsNullOrWhiteSpace(fromFilePath))
    {
        Console.WriteLine($"No .resx file found for '{from ?? "Default"}'.");
        return 1;
    }

    var toFiles = new List<string>();
    if (string.IsNullOrWhiteSpace(to))
    {
        toFiles = allFiles.Where(f => f != fromFilePath).ToList();
    }
    else
    {
        var toFilePostFix = $".{to}.resx";
        var toFilePath = allFiles.FirstOrDefault(f => f.EndsWith(toFilePostFix));
        if (string.IsNullOrWhiteSpace(toFilePath))
        {
            Console.WriteLine($"No .resx file found for '{to}'.");
            return 1;
        }

        toFiles.Add(toFilePath);
    }

    Console.WriteLine($"Number of files to translate: " + toFiles.Count);

    foreach (var toFile in toFiles)
    {
        Console.WriteLine($"Translating: " + toFile);

        var fromData = ResxIO.Read(fromFilePath);
        var toData = ResxIO.Read(toFile);
        var toTranslate = new Dictionary<string, string>();

        foreach (var key in fromData.Keys)
        {
            var shouldTranslate = false;
            if (!toData.TryGetValue(key, out var toValue))
            {
                shouldTranslate = true;
            }
            else
            {
                if (overwrite || string.IsNullOrWhiteSpace(toValue))
                {
                    shouldTranslate = true;
                }
            }

            if (!shouldTranslate)
            {
                continue;
            }

            var fromValue = fromData[key];
            toTranslate[key] = fromValue;
        }

        if (toTranslate.Count == 0)
        {
            Console.WriteLine("No translation required for " + toFile);
            continue;
        }

        var translator = new Translator(apiKey);

        var count = 0;
        var total = toTranslate.Count;

        var translated = new Dictionary<string, string>();
        foreach (var (key, value) in toTranslate)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                translated[key] = value;
                continue;
            }

            try
            {
                if (string.IsNullOrEmpty(from))
                {
                    // assume default language as english
                    from = "en";
                }

                string? toCode;
                var codeRegex = new Regex(@"^[a-zA-Z_]+\.(?'code'[a-zA-Z]{2})\.resx$");
                var fileName = Path.GetFileName(toFile);
                var match = codeRegex.Match(fileName);
                if (match.Success)
                {
                    toCode = match.Groups["code"].Value;
                }
                else
                {
                    Console.WriteLine($"Could not determine language code from file name: {fileName}");
                    continue;
                }

                var translatedValue = await translator.TranslateAsync(from, toCode, value);
                translated[key] = translatedValue;
                count++;
                Console.WriteLine($"Translated {count}/{total} {from} => {toCode}:\n{value} =>\n{translatedValue}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to translate: {value}. Reason: " + e.Message);

                if (translated.Any())
                {
                    Console.WriteLine($"Saving available translations. Count: {translated.Count}. Expected: {toTranslate.Count}");
                    ResxIO.Write(toFile, translated);
                    return 2;
                }

                Console.WriteLine("No translations available. Exiting.");
                return 2;
            }
        }

        ResxIO.Write(toFile, translated);
    }

    Console.WriteLine("Done.");
    return 0;
});

app.Run();