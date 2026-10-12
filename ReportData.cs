using System.Globalization;

namespace ColizeumReports;

public sealed class ReportData
{
    public string Admin { get; set; } = "Вадим";
    public string Receiver { get; set; } = "Дмитрий";
    public DateTime Date { get; set; } = DateTime.Today;
    public string Shift { get; set; } = "день";
    public decimal Revenue { get; set; }
    public decimal AccountTopUps { get; set; }
    public decimal Services { get; set; }
    public decimal Food { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal Cash { get; set; }
    public decimal Acquiring { get; set; }
    public decimal OnlinePayments { get; set; }
    public decimal Expenses { get; set; }
    public decimal Envelope { get; set; }
    public decimal CashRemainder { get; set; }
    public decimal Fee { get; set; }
    public decimal ExtraGuest { get; set; }
    public int HookahCount { get; set; }
    public decimal Hookah => HookahCount * 250m;

    public decimal Total => Revenue + OnlinePayments;
    // Mirrors the workbook's formula: cash + all cashless payments - food.
    public decimal GameTime => Cash + Cashless - Food;
    public decimal Cashless => Acquiring + OnlinePayments;
    public decimal RevenueDifference => Revenue - Cash - Acquiring;
    public bool RevenueReconciles => Math.Abs(RevenueDifference) <= 1m;
    public decimal ShiftSalary => Total switch
    {
        >= 30000 => 3250,
        >= 20000 => 3000,
        >= 18000 => 2900,
        >= 15000 => 2700,
        _ => 2500
    };
    public decimal HookahBonus => HookahCount * 250m;
    public decimal Salary => ShiftSalary + HookahBonus;

    private static string M(decimal value, bool decimals = true) =>
        value.ToString(decimals ? "N2" : "N0", CultureInfo.GetCultureInfo("ru-RU"));

    public string TelegramText() => $@"{Admin} {Date:dd.MM.yyyy} {Shift}
========================
💵Итого за смену - {M(Total)}
🕹Игровое время - {M(GameTime)}
🍗Бар - {M(Food)}
🥬Нал - {M(Cash)}
🛜Безнал (СБП + Эквайринг) - {M(Cashless)}
🖥СБП - {M(OnlinePayments)}
💳Эквайринг - {M(Acquiring)}
Услуги - {M(Services, false)}
Сбор - {M(Fee, false)}
Доп гость ПС - {M(ExtraGuest, false)}
Кальян - {M(Hookah, false)}
==========
В конверте:  {M(Envelope, false)}
Остаток нал в кассе: {M(CashRemainder, false)}
Смену сдал: {Admin}
Смену принял: {Receiver}";
}
