using System.Diagnostics;

namespace ColizeumReports;

public sealed class MainForm : Form
{
    private static readonly string[] Admins = ["Вадим", "Алексей", "Дмитрий", "Ульяна", "Вадик"];
    private readonly Color Yellow = Color.FromArgb(255, 229, 0);
    private readonly Color Bg = Color.FromArgb(15, 16, 18);
    private readonly Color Card = Color.FromArgb(29, 30, 34);
    private readonly Dictionary<string, NumericUpDown> nums = [];
    private string selectedAdmin = "";
    private string? excelPath;
    private bool reportPage;
    private ComboBox receiver = null!;
    private ComboBox shift = null!;
    private DateTimePicker date = null!;
    private TextBox preview = null!;
    private Label salary = null!;
    private Label pasteStatus = null!;

    public MainForm()
    {
        Text = "Colizeum Reports"; Width = 1220; Height = 820; MinimumSize = new Size(1000, 680);
        BackColor = Bg; ForeColor = Color.White; Font = new Font("Segoe UI", 10); KeyPreview = true;
        ShowAdminSelection();
    }

    private void ShowAdminSelection()
    {
        reportPage = false; Controls.Clear(); nums.Clear();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(38), RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 85)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(Title("COLIZEUM  REPORTS", 28), 0, 0);
        root.Controls.Add(new Label { Text = "Кто сегодня на смене?", AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.White, Anchor = AnchorStyles.Left }, 0, 1);
        var cards = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
        foreach (var name in Admins) cards.Controls.Add(AdminCard(name));
        root.Controls.Add(cards, 0, 2); Controls.Add(root);
    }

    private Control AdminCard(string name)
    {
        var panel = new Panel { Width = 330, Height = 210, BackColor = Card, Margin = new Padding(0, 0, 22, 22), Padding = new Padding(22) };
        var initial = new Label { Text = name[..1], Width = 58, Height = 58, TextAlign = ContentAlignment.MiddleCenter, BackColor = Yellow, ForeColor = Color.Black, Font = new Font("Segoe UI", 22, FontStyle.Bold), Location = new Point(22, 22) };
        var label = new Label { Text = name, AutoSize = true, Font = new Font("Segoe UI", 19, FontStyle.Bold), Location = new Point(95, 30) };
        var report = ActionButton("Создать отчёт", 286, (_, _) => { selectedAdmin = name; ShowReport(); }); report.Location = new Point(22, 102);
        var stats = DarkButton("Моя статистика 🔒", 286, (_, _) => OpenStats(name)); stats.Location = new Point(22, 156);
        panel.Controls.AddRange([initial, label, report, stats]); return panel;
    }

    private void ShowReport()
    {
        reportPage = true; Controls.Clear(); nums.Clear();
        receiver = Combo(Admins); receiver.SelectedIndex = Math.Max(0, Array.IndexOf(Admins, selectedAdmin) + 1) % Admins.Length;
        shift = Combo(["день", "ночь"]); shift.SelectedIndex = DateTime.Now.Hour >= 20 || DateTime.Now.Hour < 8 ? 1 : 0;
        date = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy", Width = 190, CalendarForeColor = Color.Black };
        preview = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI Emoji", 11), BackColor = Color.FromArgb(25, 26, 29), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        salary = new Label { AutoSize = true, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Yellow, Margin = new Padding(0, 8, 0, 8) };
        pasteStatus = new Label { Text = "Скопируйте скрин LGS и нажмите Ctrl+V", AutoSize = true, ForeColor = Color.Silver, Margin = new Padding(0, 0, 0, 8) };

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 2, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        header.Controls.Add(DarkButton("← Назад", 110, (_, _) => ShowAdminSelection()));
        header.Controls.Add(new Label { Text = $"  Отчёт · {selectedAdmin}", AutoSize = true, Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Yellow, Margin = new Padding(12, 8, 0, 0) });
        root.Controls.Add(header, 0, 0); root.SetColumnSpan(header, 2);

        var left = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 0, 18, 0) };
        left.Controls.Add(Row("Дата", date, "Смена", shift)); left.Controls.Add(Row("Смену принимает", receiver));
        var drop = new Panel { Width = 570, Height = 92, BackColor = Card, Margin = new Padding(0, 8, 0, 8) };
        drop.Controls.Add(new Label { Text = "ВСТАВИТЬ СКРИН LGS  ·  CTRL+V", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Yellow });
        left.Controls.Add(drop); left.Controls.Add(pasteStatus);
        left.Controls.Add(DarkButton("Выбрать рабочую таблицу Excel", 570, (_, _) => ChooseExcel()));
        foreach (var item in new[] { ("Выручка LGS", "revenue"), ("Пополнение аккаунтов", "topups"), ("Услуги", "services"), ("Еда / бар", "food"), ("Прочие доходы", "other"), ("Наличные", "cash"), ("Эквайринг", "acquiring"), ("Онлайн-платежи / СБП", "online"), ("Расход", "expenses"), ("Сбор", "fee"), ("Количество кальянов", "hookahs"), ("Доп. гость ПС", "guest"), ("В конверте", "envelope"), ("Остаток наличных", "remainder") })
        {
            var n = new NumericUpDown { DecimalPlaces = item.Item2 == "hookahs" ? 0 : 2, Maximum = 10000000, ThousandsSeparator = true, Width = 240, BackColor = Card, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            n.ValueChanged += (_, _) => RefreshPreview(); nums[item.Item2] = n; left.Controls.Add(Row(item.Item1, n));
        }
        root.Controls.Add(left, 0, 1);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5 };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        right.Controls.Add(preview, 0, 0); right.Controls.Add(salary, 0, 1);
        right.Controls.Add(ActionButton("Заполнить Excel", 460, (_, _) => SaveExcel()), 0, 2);
        right.Controls.Add(DarkButton("Скопировать отчёт для Telegram", 460, (_, _) => { Clipboard.SetText(Current().TelegramText()); MessageBox.Show("Отчёт скопирован. Вставьте его в Telegram.", "Готово"); }), 0, 3);
        root.Controls.Add(right, 1, 1); Controls.Add(root);
        foreach (Control c in new Control[] { receiver, shift, date }) c.TextChanged += (_, _) => RefreshPreview();
        RefreshPreview();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (reportPage && keyData == (Keys.Control | Keys.V) && Clipboard.ContainsImage())
        {
            try
            {
                using var image = Clipboard.GetImage()!; var path = Path.Combine(Path.GetTempPath(), $"colizeum_{Guid.NewGuid():N}.png"); image.Save(path);
                pasteStatus.Text = "Распознаю скрин…"; pasteStatus.ForeColor = Yellow; Application.DoEvents();
                Apply(OcrService.Read(path)); File.Delete(path); pasteStatus.Text = "✓ Скрин распознан — проверьте суммы"; pasteStatus.ForeColor = Color.LightGreen;
            }
            catch (Exception ex) { pasteStatus.Text = "Не удалось распознать: " + ex.Message; pasteStatus.ForeColor = Color.Salmon; }
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void OpenStats(string admin)
    {
        if (!AppDb.HasPin(admin))
        {
            var first = PromptPin($"{admin}: придумайте PIN", "Первый вход в статистику"); if (first is null) return;
            if (first.Length < 4) { MessageBox.Show("PIN должен содержать минимум 4 символа."); return; }
            var repeat = PromptPin("Повторите PIN", "Подтверждение"); if (first != repeat) { MessageBox.Show("PIN-коды не совпадают."); return; }
            AppDb.SetPin(admin, first);
        }
        else
        {
            var pin = PromptPin($"{admin}: введите PIN", "Личная статистика"); if (pin is null) return;
            if (!AppDb.VerifyPin(admin, pin)) { MessageBox.Show("Неверный PIN."); return; }
        }
        var s = AppDb.Stats(admin, DateTime.Today); var period = DateTime.Today.Day <= 15 ? "1–15" : $"16–{DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)}";
        MessageBox.Show($"Период: {period}\n\nСмен: {s.shifts}\nВыручка: {s.revenue:N0} ₽\nКальянов: {s.hookahs}\nНачислено: {s.salary:N0} ₽", $"Статистика · {admin}");
    }

    private static string? PromptPin(string label, string title)
    {
        using var f = new Form { Text = title, Width = 390, Height = 190, StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(25, 26, 29), ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var l = new Label { Text = label, Left = 24, Top = 22, Width = 330, Height = 30 };
        var box = new TextBox { Left = 24, Top = 58, Width = 330, UseSystemPasswordChar = true, Font = new Font("Segoe UI", 13) };
        var ok = new Button { Text = "Продолжить", Left = 204, Top = 105, Width = 150, Height = 34, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(255, 229, 0), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
        f.Controls.AddRange([l, box, ok]); f.AcceptButton = ok; return f.ShowDialog() == DialogResult.OK ? box.Text : null;
    }

    private void ChooseExcel() { using var dlg = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" }; if (dlg.ShowDialog() == DialogResult.OK) { excelPath = dlg.FileName; MessageBox.Show("Таблица подключена. Формулы и прошлые дни защищены.", "Готово"); } }
    private void SaveExcel()
    {
        try { if (excelPath is null) { ChooseExcel(); if (excelPath is null) return; } var d = Current(); var path = ExcelService.FillExisting(excelPath, d); AppDb.SaveReport(d); MessageBox.Show("Смена записана. Формулы проверены, резервная копия создана.", "Готово"); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Сохранение отменено"); }
    }

    private void Apply(ReportData d) { nums["revenue"].Value = d.Revenue; nums["topups"].Value = d.AccountTopUps; nums["services"].Value = d.Services; nums["food"].Value = d.Food; nums["other"].Value = d.OtherIncome; nums["cash"].Value = d.Cash; nums["acquiring"].Value = d.Acquiring; nums["online"].Value = d.OnlinePayments; nums["expenses"].Value = d.Expenses; RefreshPreview(); }
    private ReportData Current() => new() { Admin = selectedAdmin, Receiver = receiver.Text, Date = date.Value.Date, Shift = shift.Text, Revenue = nums["revenue"].Value, AccountTopUps = nums["topups"].Value, Services = nums["services"].Value, Food = nums["food"].Value, OtherIncome = nums["other"].Value, Cash = nums["cash"].Value, Acquiring = nums["acquiring"].Value, OnlinePayments = nums["online"].Value, Expenses = nums["expenses"].Value, Fee = nums["fee"].Value, ExtraGuest = nums["guest"].Value, HookahCount = (int)nums["hookahs"].Value, Envelope = nums["envelope"].Value, CashRemainder = nums["remainder"].Value };
    private void RefreshPreview() { if (!reportPage || preview is null || nums.Count == 0) return; var d = Current(); preview.Text = d.TelegramText(); salary.Text = $"Начисление за смену: {d.ShiftSalary:N0} ₽ + кальяны {d.HookahBonus:N0} ₽ = {d.Salary:N0} ₽"; }

    private Label Title(string text, float size) => new() { Text = text, AutoSize = true, Font = new Font("Segoe UI", size, FontStyle.Bold), ForeColor = Yellow, Anchor = AnchorStyles.Left };
    private ComboBox Combo(string[] values) { var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, BackColor = Card, ForeColor = Color.White }; c.Items.AddRange(values); return c; }
    private Button ActionButton(string text, int width, EventHandler click) { var b = BaseButton(text, width, Yellow, Color.Black); b.Click += click; return b; }
    private Button DarkButton(string text, int width, EventHandler click) { var b = BaseButton(text, width, Color.FromArgb(45, 46, 51), Color.White); b.Click += click; return b; }
    private static Button BaseButton(string text, int width, Color bg, Color fg) { var b = new Button { Text = text, Width = width, Height = 44, BackColor = bg, ForeColor = fg, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 4, 8, 4) }; b.FlatAppearance.BorderSize = 0; return b; }
    private static Control Row(params object[] items) { var p = new FlowLayoutPanel { AutoSize = true, Width = 570, Height = 50, WrapContents = false }; foreach (var x in items) { if (x is string s) p.Controls.Add(new Label { Text = s, Width = 160, Height = 34, TextAlign = ContentAlignment.MiddleLeft }); else if (x is Control c) p.Controls.Add(c); } return p; }
}
