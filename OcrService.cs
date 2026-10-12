using System.Text.RegularExpressions;
using Tesseract;

namespace ColizeumReports;

public static partial class OcrService
{
    public static ReportData Read(string imagePath)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (!File.Exists(Path.Combine(dataPath, "rus.traineddata")))
            throw new FileNotFoundException("Не найден OCR-файл tessdata/rus.traineddata. Скачайте сборку из GitHub Actions целиком.");

        using var engine = new TesseractEngine(dataPath, "rus+eng", EngineMode.Default);
        engine.SetVariable("preserve_interword_spaces", "1");
        using var image = Pix.LoadFromFile(imagePath);
        using var page = engine.Process(image);
        var text = page.GetText();

        return new ReportData
        {
            Revenue = Find(text, "Общая выручка"),
            AccountTopUps = Find(text, "Пополнение аккаунтов"),
            Services = Find(text, "Услуги"),
            Food = Find(text, "Еда"),
            OtherIncome = Find(text, "Прочие доходы"),
            OnlinePayments = Find(text, "Сумма онлайн-платежей", "Сумма онлайн платежей"),
            Expenses = Find(text, "Расходы за смену"),
            Cash = FindPair(text, "Нал/Безнал", first: true),
            Acquiring = FindPair(text, "Нал/Безнал", first: false)
        };
    }

    private static decimal Find(string text, params string[] labels)
    {
        foreach (var line in text.Split('\n'))
        foreach (var label in labels)
            if (line.Contains(label, StringComparison.OrdinalIgnoreCase))
                return LastNumber(line);
        return 0;
    }

    private static decimal FindPair(string text, string label, bool first)
    {
        var line = text.Split('\n').FirstOrDefault(x => x.Contains(label, StringComparison.OrdinalIgnoreCase)) ?? "";
        var nums = NumberRegex().Matches(line).Select(x => Parse(x.Value)).ToArray();
        if (nums.Length < 2) return 0;
        return first ? nums[^2] : nums[^1];
    }

    private static decimal LastNumber(string line)
    {
        var matches = NumberRegex().Matches(line);
        return matches.Count == 0 ? 0 : Parse(matches[^1].Value);
    }

    private static decimal Parse(string value)
    {
        value = value.Replace("₽", "").Replace("Р", "").Replace(" ", "").Replace(" ", "").Replace(',', '.');
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    [GeneratedRegex(@"\d[\d\s ]*(?:[,.]\d{1,2})?")]
    private static partial Regex NumberRegex();
}
