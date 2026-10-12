using ClosedXML.Excel;

namespace ColizeumReports;

public static class ExcelService
{
    private static readonly HashSet<int> WritableColumns = [2, 5, 7, 8, 9, 11, 12, 14];
    private static readonly HashSet<int> FormulaColumns = [3, 4, 6, 10, 13];

    public static string FillExisting(string sourcePath, ReportData d)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Файл Excel не найден.", sourcePath);
        if (d.Date.Date < DateTime.Today) throw new InvalidOperationException("Запрещено изменять предыдущие дни.");
        using var wb = new XLWorkbook(sourcePath);
        var ws = FindMonthSheet(wb, d.Date);
        var row = FindShiftRow(ws, d.Date, d.Shift);
        EnsureTargetIsSafe(ws, row, d.Date);
        var formulasBefore = FormulaSnapshot(wb);
        var valuesBefore = ValueSnapshot(wb);

        SetValue(ws, row, 2, ExcelAdminName(d.Admin));
        SetValue(ws, row, 5, d.Food);
        SetValue(ws, row, 7, d.Fee);
        SetValue(ws, row, 8, d.Hookah);
        SetValue(ws, row, 9, d.Cash);
        SetValue(ws, row, 11, d.OnlinePayments);
        SetValue(ws, row, 12, d.Acquiring);
        SetValue(ws, row, 14, d.Expenses);

        VerifyOnlyTargetCellsChanged(wb, valuesBefore, ws.Name, row);
        VerifyFormulasUnchanged(wb, formulasBefore);
        var backupDir = Path.Combine(Path.GetDirectoryName(sourcePath)!, "Резервные копии Colizeum Reports");
        Directory.CreateDirectory(backupDir);
        File.Copy(sourcePath, Path.Combine(backupDir, $"{Path.GetFileNameWithoutExtension(sourcePath)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"), false);
        var temp = sourcePath + ".colizeum.tmp.xlsx";
        try
        {
            wb.SaveAs(temp);
            using (var check = new XLWorkbook(temp)) VerifyFormulasUnchanged(check, formulasBefore);
            File.Move(temp, sourcePath, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return sourcePath;
    }

    private static IXLWorksheet FindMonthSheet(XLWorkbook wb, DateTime date)
    {
        var month = date.Month switch
        {
            1 => "ЯНВАРЬ", 2 => "ФЕВРАЛЬ", 3 => "МАРТ", 4 => "АПРЕЛЬ",
            5 => "МАЙ", 6 => "ИЮНЬ", 7 => "ИЮЛЬ", 8 => "АВГУСТ",
            9 => "СЕНТЯБРЬ", 10 => "ОКТЯБРЬ", 11 => "НОЯБРЬ", 12 => "ДЕКАБРЬ",
            _ => throw new ArgumentOutOfRangeException(nameof(date))
        };
        return wb.Worksheets.FirstOrDefault(x => x.Name.Trim().Equals(month, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"В книге не найден лист «{month}».");
    }

    private static int FindShiftRow(IXLWorksheet ws, DateTime date, string shift)
    {
        foreach (var cell in ws.Column(1).CellsUsed())
            if (cell.TryGetValue<DateTime>(out var value) && value.Date == date.Date)
                return cell.Address.RowNumber + (shift.Equals("ночь", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        throw new InvalidOperationException($"Не найдена дата {date:dd.MM.yyyy}.");
    }

    private static void EnsureTargetIsSafe(IXLWorksheet ws, int row, DateTime date)
    {
        if (ws.Cell(row, 2).GetString().Equals("Итого", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Выбранная строка является строкой «Итого».");
        foreach (var col in FormulaColumns)
            if (!ws.Cell(row, col).HasFormula)
                throw new InvalidOperationException($"В {ws.Cell(row, col).Address} отсутствует ожидаемая формула.");
        var startRow = row - (row - 2) % 3;
        if (!ws.Cell(startRow, 1).TryGetValue<DateTime>(out var actual) || actual.Date != date.Date)
            throw new InvalidOperationException("Строка не соответствует выбранной дате.");
    }

    private static void SetValue(IXLWorksheet ws, int row, int col, object value)
    {
        if (!WritableColumns.Contains(col)) throw new InvalidOperationException("Запрещённый столбец.");
        var cell = ws.Cell(row, col);
        if (cell.HasFormula) throw new InvalidOperationException($"{cell.Address} содержит формулу.");
        if (value is string s) cell.Value = s;
        else if (value is decimal d) cell.Value = d;
        else throw new InvalidOperationException("Неподдерживаемое значение.");
    }

    private static string ExcelAdminName(string name) => name switch
    {
        "Вадим" => "1Вадим",
        "Алексей" => "2Алексей",
        "Дмитрий" => "3Дмитрий",
        "Ульяна" => "5Ульяна",
        "Вадик" => "6Вадик",
        _ => throw new InvalidOperationException("Неизвестный администратор.")
    };

    private static string CellKey(IXLWorksheet ws, IXLCell cell) =>
        $"{ws.Name}!{XLHelper.GetColumnLetterFromNumber(cell.Address.ColumnNumber)}{cell.Address.RowNumber}";

    private static Dictionary<string, string> FormulaSnapshot(XLWorkbook wb) => wb.Worksheets
        .SelectMany(ws => ws.CellsUsed(c => c.HasFormula).Select(c => new KeyValuePair<string, string>(CellKey(ws, c), c.FormulaA1))).ToDictionary();

    private static Dictionary<string, string> ValueSnapshot(XLWorkbook wb) => wb.Worksheets
        .SelectMany(ws => ws.CellsUsed().Select(c => new KeyValuePair<string, string>(CellKey(ws, c), c.HasFormula ? "=" + c.FormulaA1 : c.Value.ToString()))).ToDictionary();

    private static void VerifyFormulasUnchanged(XLWorkbook wb, Dictionary<string, string> before)
    {
        var after = FormulaSnapshot(wb);
        if (before.Count != after.Count || before.Any(x => !after.TryGetValue(x.Key, out var f) || f != x.Value))
            throw new InvalidOperationException("Обнаружено изменение формулы. Сохранение отменено.");
    }

    private static void VerifyOnlyTargetCellsChanged(XLWorkbook wb, Dictionary<string, string> before, string sheet, int row)
    {
        var after = ValueSnapshot(wb);
        var changed = before.Keys.Union(after.Keys).Where(k => before.GetValueOrDefault(k) != after.GetValueOrDefault(k));
        var allowed = WritableColumns.Select(c => $"{sheet}!{XLHelper.GetColumnLetterFromNumber(c)}{row}").ToHashSet();
        var forbidden = changed.Where(x => !allowed.Contains(x)).ToArray();
        if (forbidden.Length > 0) throw new InvalidOperationException("Изменение запрещённых ячеек: " + string.Join(", ", forbidden));
    }
}
