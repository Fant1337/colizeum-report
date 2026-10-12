using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace ColizeumReports;

public static class AppDb
{
    private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ColizeumReports");
    private static readonly string DbPath = Path.Combine(Dir, "colizeum.db");
    private static string ConnectionString => $"Data Source={DbPath}";

    public static void Initialize()
    {
        Directory.CreateDirectory(Dir);
        using var db = new SqliteConnection(ConnectionString); db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS admins(name TEXT PRIMARY KEY, pin_hash TEXT, pin_salt TEXT);
            CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS reports(
              id INTEGER PRIMARY KEY AUTOINCREMENT, admin TEXT NOT NULL, report_date TEXT NOT NULL,
              shift TEXT NOT NULL, total REAL NOT NULL, food REAL NOT NULL, game_time REAL NOT NULL,
              hookah_count INTEGER NOT NULL, salary REAL NOT NULL, created_at TEXT NOT NULL,
              UNIQUE(admin, report_date, shift));
            """;
        cmd.ExecuteNonQuery();
    }

    public static string? GetExcelPath()
    {
        using var db = new SqliteConnection(ConnectionString); db.Open();
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT value FROM settings WHERE key='excel_path'";
        return cmd.ExecuteScalar() as string;
    }

    public static void SetExcelPath(string path)
    {
        using var db = new SqliteConnection(ConnectionString); db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO settings(key,value) VALUES('excel_path',$v) ON CONFLICT(key) DO UPDATE SET value=$v";
        cmd.Parameters.AddWithValue("$v", path); cmd.ExecuteNonQuery();
    }

    public static bool HasPin(string admin)
    {
        using var db = new SqliteConnection(ConnectionString); db.Open();
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT pin_hash FROM admins WHERE name=$n"; cmd.Parameters.AddWithValue("$n", admin);
        return cmd.ExecuteScalar() is string s && !string.IsNullOrEmpty(s);
    }

    public static void SetPin(string admin, string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16); var hash = Hash(pin, salt);
        using var db = new SqliteConnection(ConnectionString); db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO admins(name,pin_hash,pin_salt) VALUES($n,$h,$s) ON CONFLICT(name) DO UPDATE SET pin_hash=$h,pin_salt=$s";
        cmd.Parameters.AddWithValue("$n", admin); cmd.Parameters.AddWithValue("$h", Convert.ToBase64String(hash)); cmd.Parameters.AddWithValue("$s", Convert.ToBase64String(salt)); cmd.ExecuteNonQuery();
    }

    public static bool VerifyPin(string admin, string pin)
    {
        using var db = new SqliteConnection(ConnectionString); db.Open();
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT pin_hash,pin_salt FROM admins WHERE name=$n"; cmd.Parameters.AddWithValue("$n", admin);
        using var r = cmd.ExecuteReader(); if (!r.Read()) return false;
        return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(r.GetString(0)), Hash(pin, Convert.FromBase64String(r.GetString(1))));
    }

    private static byte[] Hash(string pin, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(pin, salt, 120000, HashAlgorithmName.SHA256, 32);

    public static void SaveReport(ReportData d)
    {
        using var db = new SqliteConnection(ConnectionString); db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO reports(admin,report_date,shift,total,food,game_time,hookah_count,salary,created_at)
            VALUES($a,$d,$s,$t,$f,$g,$h,$p,$c)
            ON CONFLICT(admin,report_date,shift) DO UPDATE SET total=$t,food=$f,game_time=$g,hookah_count=$h,salary=$p,created_at=$c
            """;
        cmd.Parameters.AddWithValue("$a", d.Admin); cmd.Parameters.AddWithValue("$d", d.Date.ToString("yyyy-MM-dd")); cmd.Parameters.AddWithValue("$s", d.Shift);
        cmd.Parameters.AddWithValue("$t", d.Total); cmd.Parameters.AddWithValue("$f", d.Food); cmd.Parameters.AddWithValue("$g", d.GameTime);
        cmd.Parameters.AddWithValue("$h", d.HookahCount); cmd.Parameters.AddWithValue("$p", d.Salary); cmd.Parameters.AddWithValue("$c", DateTime.Now.ToString("O")); cmd.ExecuteNonQuery();
    }

    public static (int shifts, decimal revenue, decimal salary, int hookahs) Stats(string admin, DateTime today)
    {
        var from = today.Day <= 15 ? new DateTime(today.Year, today.Month, 1) : new DateTime(today.Year, today.Month, 16);
        var to = today.Day <= 15 ? new DateTime(today.Year, today.Month, 15) : new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        using var db = new SqliteConnection(ConnectionString); db.Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*),COALESCE(SUM(total),0),COALESCE(SUM(salary),0),COALESCE(SUM(hookah_count),0) FROM reports WHERE admin=$a AND report_date BETWEEN $f AND $t";
        cmd.Parameters.AddWithValue("$a", admin); cmd.Parameters.AddWithValue("$f", from.ToString("yyyy-MM-dd")); cmd.Parameters.AddWithValue("$t", to.ToString("yyyy-MM-dd"));
        using var r = cmd.ExecuteReader(); r.Read();
        return (Convert.ToInt32(r.GetValue(0)), Convert.ToDecimal(r.GetValue(1)), Convert.ToDecimal(r.GetValue(2)), Convert.ToInt32(r.GetValue(3)));
    }
}
