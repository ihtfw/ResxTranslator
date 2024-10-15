using Cocona;
using ResxTranslator;

Console.OutputEncoding = System.Text.Encoding.UTF8;

CoconaApp.Run(async (string dir, string to, string? from = null, bool overwrite = false, string? apiKey = null) =>
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

    var files = Directory.GetFiles(dir, "*.resx", SearchOption.AllDirectories);
    if (files.Length == 0)
    {
        Console.WriteLine($"No .resx files found in '{dir}'.");
        return 1;
    }

    var toFilePostFix = $".{to}.resx";
    var toFilePath = files.FirstOrDefault(f => f.EndsWith(toFilePostFix));
    if (string.IsNullOrWhiteSpace(toFilePath))
    {
        Console.WriteLine($"No .resx file found for '{to}'.");
        return 1;
    }

    string? fromFilePath;
    if (string.IsNullOrWhiteSpace(from))
    {
        var toFileName = Path.GetFileName(toFilePath);
        var defaultFileName = toFileName.Substring(0, toFileName.Length - toFilePostFix.Length) + ".resx";
        fromFilePath = files.FirstOrDefault(x => Path.GetFileName(x) == defaultFileName);
    }
    else
    {
        fromFilePath = files.FirstOrDefault(f => f.EndsWith($".{from}.resx"));
    }

    if (string.IsNullOrWhiteSpace(fromFilePath))
    {
        Console.WriteLine($"No .resx file found for '{from ?? "Default"}'.");
        return 1;
    }


    var fromData = ResxIO.Read(fromFilePath);
    var toData = ResxIO.Read(toFilePath);
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
        Console.WriteLine("No translation required.");
        return 0;
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
            var translatedValue = await translator.TranslateAsync(from, to, value);
            translated[key] = translatedValue;
            count++;
            Console.WriteLine($"Translated {count}/{total}: {value} => {translatedValue}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to translate: {value}. Reason: " + e.Message);

            if (translated.Any())
            {
                Console.WriteLine($"Saving available translations. Count: {translated.Count}. Expected: {toTranslate.Count}");
                ResxIO.Write(toFilePath, translated);
                return 2;
            }

            Console.WriteLine("No translations available. Exiting.");
            return 2;
        }
    }

    ResxIO.Write(toFilePath, translated);
    return 0;
});