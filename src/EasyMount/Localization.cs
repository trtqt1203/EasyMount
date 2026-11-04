using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Markup;
namespace EasyMount;

public sealed class L : INotifyPropertyChanged
{
    public static string VersionLabel => "Windows x64 · v" + Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
    public static string CopyrightVersionLabel => VersionLabel + "  ·  © TravisTRTQT";
    public static L Current { get; } = new();
    public static string Language { get; private set; } = "en";
    private static readonly Dictionary<string,string> English = Load();
    private static readonly Dictionary<string,string> Keys = new();
    public event PropertyChangedEventHandler? PropertyChanged;
    private static Dictionary<string,string> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EasyMount.Translations.json")!;
        return JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!;
    }
    public static void SetLanguage(string language)
    {
        if (language is not ("vi" or "en") || language == Language) return;
        Language = language;
        Current.PropertyChanged?.Invoke(Current, new PropertyChangedEventArgs("Item[]"));
    }
    internal static string Register(string text)
    {
        var key = "k" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
        Keys[key] = text; return key;
    }
    public string this[string key] => T(Keys[key]);
    public static string T(string text)
    {
        if (Language != "en") return text;
        if (English.TryGetValue(text, out var value)) return value;
        // Translate only application-owned generated strings. Names and emails bypass this service.
        var time = Regex.Match(text, @"^(\d+) (phút|giờ|ngày) trước$");
        if (time.Success) return time.Groups[1].Value + (time.Groups[2].Value == "phút" ? "m" : time.Groups[2].Value == "giờ" ? "h" : "d") + " ago";
        var pool = Regex.Match(text, @"^Ổ gộp · (\d+)( nguồn| tài khoản: )?$");
        if (pool.Success) return "Pool · " + pool.Groups[1].Value + (pool.Groups[2].Value == " nguồn" ? " sources" : pool.Groups[2].Value == " tài khoản: " ? " accounts: " : "");
        var online = Regex.Match(text, @"^(\d+) / (\d+) ổ đĩa online  \|$");
        if (online.Success) return $"{online.Groups[1].Value} / {online.Groups[2].Value} drives online  |";
        foreach (var prefix in new[]{"●  ","○  ","◌  "})
            if (text.StartsWith(prefix, StringComparison.Ordinal)) return prefix + T(text[prefix.Length..]);
        return text;
    }
}

public sealed class TranslateExtension : MarkupExtension
{
    public string Text { get; set; }
    public TranslateExtension(string text) => Text = text;
    public override object ProvideValue(IServiceProvider serviceProvider) => new Binding("[" + L.Register(Text) + "]") { Source = L.Current, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}
