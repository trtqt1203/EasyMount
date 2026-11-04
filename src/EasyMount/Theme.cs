using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
namespace EasyMount;

public static class Theme
{
    private static readonly Dictionary<string,SolidColorBrush> Brushes = new();
    private static readonly Dictionary<string,PaletteColor> Values = new();
    private sealed class PaletteColor : INotifyPropertyChanged
    {
        private Color color;
        public Color Color { get => color; set { color=value;PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(nameof(Color))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private static readonly string[] Colors = {"080E0D","0F1918","163028","123B31","E8FBF5","A5C4BA","91B2A7","7AADA0","1E3D35","2A5248","00E5B4","62FFDA","F87171","14F87171","33F87171","2600E5B4","0A00E5B4","302615","80602B","F8C56B","F59E0B"};
    public static bool IsLight { get; private set; }
    public static event EventHandler? Changed;
    public static string Hex(string dark, bool foreground = false)
    {
        if (!IsLight) return dark;
        return dark.ToUpperInvariant() switch
        {
            "#080E0D" => foreground ? "#080E0D" : "#F3F7F5",
            "#0F1918" => "#FFFFFF", "#163028" => "#E5F0EA", "#123B31" => "#D3EADF",
            "#E8FBF5" => "#142B24", "#A5C4BA" => "#405C51", "#91B2A7" => "#506A60", "#7AADA0" => "#4B695D",
            "#1E3D35" => "#D1DED7", "#2A5248" => "#9EB7AA",
            "#00E5B4" or "#62FFDA" => foreground ? "#006B50" : "#00E5B4",
            "#F87171" => foreground ? "#B42332" : "#E65161", "#F59E0B" => foreground ? "#865500" : "#C68000",
            "#302615" => "#FFF5DC", "#80602B" => "#D5B873", "#F8C56B" => "#705000",
            _ => dark
        };
    }
    private static string Key(string dark,bool foreground) => "Theme_" + (foreground ? "fg_" : "bg_") + dark.TrimStart('#').ToUpperInvariant();
    public static SolidColorBrush Brush(string dark,bool foreground=false)
    {
        var key=Key(dark,foreground);
        if(!Brushes.TryGetValue(key,out var brush))
        {
            Values[key]=new PaletteColor{Color=(Color)ColorConverter.ConvertFromString(Hex(dark,foreground))};
            Brushes[key]=brush=new SolidColorBrush();
            BindingOperations.SetBinding(brush,SolidColorBrush.ColorProperty,new Binding(nameof(PaletteColor.Color)){Source=Values[key],Mode=BindingMode.OneWay});
        }
        return brush;
    }
    public static void Apply(string mode)
    {
        IsLight=mode=="light";
        foreach(var hex in Colors)
        foreach(var foreground in new[]{false,true})
        {
            var dark="#"+hex;var color=(Color)ColorConverter.ConvertFromString(Hex(dark,foreground));
            var brush=Brush(dark,foreground);Values[Key(dark,foreground)].Color=color;
            Application.Current.Resources[Key(dark,foreground)]=brush;
            Application.Current.Resources[Key(dark,foreground)+"_Color"]=color;
        }
        Application.Current.Resources["Surface"]=Brush("#0F1918");
        Application.Current.Resources["Muted"]=Brush("#A5C4BA",true);
        Application.Current.Resources["TextPrimary"]=Brush("#E8FBF5",true);
        Application.Current.Resources["Accent"]=Brush("#00E5B4",true);
        Changed?.Invoke(null,EventArgs.Empty);
    }
}
