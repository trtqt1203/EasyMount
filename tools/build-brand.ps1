$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore,PresentationFramework,WindowsBase
$root = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $root 'src\EasyMount\Assets'
$previews = Join-Path $root 'docs\brand'
New-Item -ItemType Directory -Force $assets,$previews | Out-Null
function Brush([string]$color) { return [Windows.Media.BrushConverter]::new().ConvertFromString($color) }
$cloud = 'M 168,282 C 132,282 110,260 110,229 C 110,197 134,173 165,173 C 175,134 207,109 247,109 C 290,109 324,139 331,175 C 365,173 395,196 395,230 C 395,260 370,282 338,282 Z'
$arrow = 'M 239,255 L 239,348 L 215,324 L 193,346 L 256,409 L 319,346 L 297,324 L 273,348 L 273,255 Z'
$stack = @('M 138,164 L 256,101 L 374,164 L 256,227 Z','M 138,239 L 256,302 L 374,239 L 374,282 L 256,346 L 138,282 Z','M 138,324 L 256,387 L 374,324 L 374,365 L 256,429 L 138,365 Z')
$lift = 'M 139,360 L 139,291 L 256,181 L 373,291 L 373,360 L 281,273 L 281,416 L 231,416 L 231,273 Z'
function Visual([int]$variant) {
    $visual = [Windows.Media.DrawingVisual]::new(); $dc = $visual.RenderOpen()
    $gradient = [Windows.Media.LinearGradientBrush]::new((Brush '#173C3E').Color,(Brush '#101B2C').Color,45)
    $dc.DrawRoundedRectangle($gradient,$null,[Windows.Rect]::new(0,0,512,512),110,110)
    if ($variant -eq 0) {
        $dc.DrawGeometry((Brush '#F1FFFA'),$null,[Windows.Media.Geometry]::Parse($cloud))
        $dc.DrawRoundedRectangle((Brush '#6EE5BB'),$null,[Windows.Rect]::new(110,316,285,97),24,24)
        $dc.DrawRoundedRectangle((Brush '#123B35'),$null,[Windows.Rect]::new(144,349,143,13),6,6)
        $dc.DrawEllipse((Brush '#123B35'),$null,[Windows.Point]::new(355,364),10,10)
        $dc.DrawGeometry((Brush '#17372F'),$null,[Windows.Media.Geometry]::Parse('M 240,227 L 272,227 L 272,304 L 295,281 L 315,302 L 256,361 L 197,302 L 217,281 L 240,304 Z'))
    } elseif ($variant -eq 1) {
        $dc.DrawGeometry((Brush '#E9FFF9'),$null,[Windows.Media.Geometry]::Parse($stack[0]))
        $dc.DrawGeometry((Brush '#77E9C6'),$null,[Windows.Media.Geometry]::Parse($stack[1]))
        $dc.DrawGeometry((Brush '#39AA98'),$null,[Windows.Media.Geometry]::Parse($stack[2]))
    } else {
        $dc.DrawGeometry((Brush '#7CE8CE'),$null,[Windows.Media.Geometry]::Parse($lift))
        $dc.DrawRoundedRectangle((Brush '#E7FFF8'),$null,[Windows.Rect]::new(201,101,110,27),13,13)
    }
    $dc.Close(); return $visual
}
function PngBytes($visual,[int]$size) {
    $scaled = [Windows.Media.DrawingVisual]::new(); $dc=$scaled.RenderOpen()
    $dc.PushTransform([Windows.Media.ScaleTransform]::new($size/512.0,$size/512.0))
    $dc.DrawDrawing($visual.Drawing); $dc.Pop(); $dc.Close()
    $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($scaled)
    $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new(); $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream=[IO.MemoryStream]::new(); $encoder.Save($stream); $bytes=$stream.ToArray(); $stream.Dispose(); return ,$bytes
}
$visuals=@((Visual 0),(Visual 1),(Visual 2))
for ($i=0;$i -lt 3;$i++) { [IO.File]::WriteAllBytes((Join-Path $previews ('logo-'+($i+1)+'.png')),(PngBytes $visuals[$i] 512)) }
[IO.File]::WriteAllBytes((Join-Path $assets 'easymount.png'),(PngBytes $visuals[0] 512))
$sizes=@(16,20,24,32,40,48,64,128,256); $frames=@()
foreach($size in $sizes) { $frames+=,(PngBytes $visuals[0] $size) }
$stream=[IO.MemoryStream]::new(); $writer=[IO.BinaryWriter]::new($stream)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
$offset=6+16*$sizes.Count
for($i=0;$i -lt $sizes.Count;$i++) {
    $dimension=if($sizes[$i] -eq 256){0}else{$sizes[$i]}
    $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
    $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frames[$i].Length);$writer.Write([uint32]$offset)
    $offset+=$frames[$i].Length
}
foreach($frame in $frames){$writer.Write([byte[]]$frame)}
[IO.File]::WriteAllBytes((Join-Path $root 'src\EasyMount\easymount.ico'),$stream.ToArray());$writer.Dispose();$stream.Dispose()
$sheet=[Windows.Media.DrawingVisual]::new();$dc=$sheet.RenderOpen()
$dc.DrawRectangle((Brush '#F3F7F5'),$null,[Windows.Rect]::new(0,0,1260,530))
for($i=0;$i -lt 3;$i++) {
    $dc.PushTransform([Windows.Media.TranslateTransform]::new(85+$i*420,65));$dc.PushTransform([Windows.Media.ScaleTransform]::new(0.49,0.49));$dc.DrawDrawing($visuals[$i].Drawing);$dc.Pop();$dc.Pop()
    $labels=@('A  CLOUD + DRIVE','B  MERGED DRIVES','C  MOUNT ARROW')
    $text=[Windows.Media.FormattedText]::new($labels[$i],[Globalization.CultureInfo]::InvariantCulture,[Windows.FlowDirection]::LeftToRight,[Windows.Media.Typeface]::new('Segoe UI Semibold'),22,(Brush '#183733'))
    $dc.DrawText($text,[Windows.Point]::new(85+$i*420,350))
    $small=[Windows.Media.FormattedText]::new('EasyMount',[Globalization.CultureInfo]::InvariantCulture,[Windows.FlowDirection]::LeftToRight,[Windows.Media.Typeface]::new('Segoe UI'),34,(Brush '#183733'))
    $dc.DrawText($small,[Windows.Point]::new(85+$i*420,395))
}
$dc.Close();$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(1260,530,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($sheet)
$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$out=[IO.File]::Create((Join-Path $previews 'logo-options.png'));$encoder.Save($out);$out.Dispose()
$svgStart='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><defs><linearGradient id="bg" x2="1" y2="1"><stop stop-color="#173C3E"/><stop offset="1" stop-color="#101B2C"/></linearGradient></defs><rect width="512" height="512" rx="110" fill="url(#bg)"/>'
$svg=$svgStart+'<path fill="#F1FFFA" d="'+$cloud+'"/><rect x="110" y="316" width="285" height="97" rx="24" fill="#6EE5BB"/><rect x="144" y="349" width="143" height="13" rx="6" fill="#123B35"/><circle cx="355" cy="364" r="10" fill="#123B35"/><path fill="#17372F" d="M240 227h32v77l23-23 20 21-59 59-59-59 20-21 23 23Z"/></svg>'
[IO.File]::WriteAllText((Join-Path $assets 'easymount.svg'),$svg)
Write-Output 'Brand assets and multi-resolution Windows icon generated.'
