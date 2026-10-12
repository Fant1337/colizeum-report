using System.Text.RegularExpressions;
using Tesseract;

namespace ColizeumReports;

public sealed record OcrResult(ReportData Data, IReadOnlyList<string> MissingFields);

public static partial class OcrService
{
    public static OcrResult Read(string imagePath)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        var russianData = Path.Combine(dataPath, "rus.traineddata");
        var englishData = Path.Combine(dataPath, "eng.traineddata");
        if (!File.Exists(russianData) || !File.Exists(englishData))
            throw new FileNotFoundException("Не найдены файлы OCR. Распакуйте всю папку приложения вместе с tessdata.");

        using var engine = new TesseractEngine(dataPath, "rus+eng", EngineMode.Default);
        engine.SetVariable("preserve_interword_spaces", "1");
        using var image = Pix.LoadFromFile(imagePath);
        using var page = engine.Process(image);
        var text = page.GetText();

        var missing = new List<string>();
        var revenue = Find(text, "Общая выручка", missing, "Общая выручка");
        var topups = Find(text, "Пополнение аккаунтов", missing, "Пополнение аккаунтов");
        var services = Find(text, "Услуги", missing, "Услуги");
        var food = Find(text, "Еда", missing, "Еда");
        var other = Find(text, "Прочие доходы", missing, "Прочие доходы");
        var online = Find(text, ["Сумма онлайн-платежей", "Сумма онлайн платежей"], missing, "Онлайн-платежи");
        var expenses = Find(text, "Расходы за смену", missing, "Расходы за смену");
        var (cash, acquiring) = FindPair(text, "Нал/Безнал", missing);

        return new OcrResult(new ReportData
        {
            Revenue = revenue,
            AccountTopUps = topups,
            Services = services,
            Food = food,
            OtherIncome = other,
            OnlinePayments = online,
            Expenses = expenses,
            Cash = cash,
            Acquiring = acquiring
        }, missing);
    }

    private static decimal Find(string text, string label, List<string> missing, string displayName) =>
        Find(text, [label], missing, displayName);

    private static decimal Find(string text, string[] labels, List<string> missing, string displayName)
    {
        foreach (var line in text.Split('\n'))
        {
            if (!labels.Any(label => line.Contains(label, StringComparison.OrdinalIgnoreCase))) continue;
            var value = LastNumber(line);
            if (value is null) { missing.Add(displayName); return 0; }
            return value.Value;
        }
        missing.Add(displayName);
        return 0;
    }

    private static (decimal cash, decimal acquiring) FindPair(string text, string label, List<string> missing)
    {
        var line = text.Split('\n').FirstOrDefault(x => x.Contains(label, StringComparison.OrdinalIgnoreCase)) ?? "";
        var nums = NumberRegex().Matches(line).Select(x => Parse(x.Value)).ToArray();
        if (nums.Length < 2)
        {
            missing.Add("Нал/Безнал");
            return (0, 0);
        }
        return (nums[^2], nums[^1]);
    }

    private static decimal? LastNumber(string line)
    {
        var matches = NumberRegex().Matches(line);
        return matches.Count == 0 ? null : Parse(matches[^1].Value);
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

