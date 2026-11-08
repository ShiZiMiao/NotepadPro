# Generates the original NotepadPro application icon (multi-size .ico, PNG entries).
# The drawing code is C# via Add-Type (C# 5 compatible for Windows PowerShell 5.1).
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File tools/make-icon.ps1
$ErrorActionPreference = 'Stop'

$src = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text;

public static class IconGen
{
    public static void Generate(string outPath)
    {
        int[] sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };
        Dictionary<int, byte[]> pngs = new Dictionary<int, byte[]>();
        foreach (int sz in sizes) pngs[sz] = Render((float)sz);

        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8, true);
        bw.Write((ushort)0);                 // reserved
        bw.Write((ushort)1);                 // type: icon
        bw.Write((ushort)sizes.Length);
        int offset = 6 + 16 * sizes.Length;
        foreach (int sz in sizes)
        {
            byte[] data = pngs[sz];
            int dim = sz >= 256 ? 0 : sz;
            bw.Write((byte)dim);
            bw.Write((byte)dim);
            bw.Write((byte)0);               // palette
            bw.Write((byte)0);               // reserved
            bw.Write((ushort)1);             // planes
            bw.Write((ushort)32);            // bpp
            bw.Write((uint)data.Length);
            bw.Write((uint)offset);
            offset += data.Length;
        }
        foreach (int sz in sizes) bw.Write(pngs[sz]);
        bw.Flush();
        bw.Dispose();

        string full = Path.GetFullPath(outPath);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllBytes(full, ms.ToArray());
        ms.Dispose();
    }

    private static byte[] Render(float size)
    {
        float s = size / 256f;
        Bitmap bmp = new Bitmap((int)size, (int)size);
        Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        // Rounded-rect page (dark slate blue)
        RectangleF rect = new RectangleF(24f * s, 12f * s, 208f * s, 232f * s);
        float r = 40f * s;
        GraphicsPath page = new GraphicsPath();
        page.AddArc(rect.X, rect.Y, 2f * r, 2f * r, 180f, 90f);
        page.AddArc(rect.Right - 2f * r, rect.Y, 2f * r, 2f * r, 270f, 90f);
        page.AddArc(rect.Right - 2f * r, rect.Bottom - 2f * r, 2f * r, 2f * r, 0f, 90f);
        page.AddArc(rect.X, rect.Bottom - 2f * r, 2f * r, 2f * r, 90f, 90f);
        page.CloseFigure();
        LinearGradientBrush pageBrush = new LinearGradientBrush(
            rect, Color.FromArgb(255, 40, 74, 118), Color.FromArgb(255, 26, 46, 78),
            LinearGradientMode.Vertical);
        g.FillPath(pageBrush, page);

        // Text lines
        int lineH = Math.Max(1, (int)(12 * s));
        SolidBrush lineBrush = new SolidBrush(Color.FromArgb(210, 235, 242, 250));
        float xs = 56f * s, xe = 200f * s;
        float[] lineYs = new float[] { 64f * s, 96f * s, 128f * s };
        foreach (float y in lineYs)
            g.FillRectangle(lineBrush, xs, y, xe - xs, lineH);
        SolidBrush accent = new SolidBrush(Color.FromArgb(255, 84, 176, 122));
        g.FillRectangle(accent, xs, 160f * s, 88f * s, lineH);

        // Big letter N
        FontFamily fam = new FontFamily("Segoe UI");
        Font font = new Font(fam, 110f * s, FontStyle.Bold, GraphicsUnit.Pixel);
        SolidBrush textBrush = new SolidBrush(Color.FromArgb(255, 98, 192, 255));
        RectangleF tr = new RectangleF(16f * s, 10f * s, 140f * s, 140f * s);
        StringFormat fmt = new StringFormat();
        fmt.Alignment = StringAlignment.Center;
        fmt.LineAlignment = StringAlignment.Center;
        g.DrawString("N", font, textBrush, tr, fmt);

        g.Dispose();
        page.Dispose(); pageBrush.Dispose(); lineBrush.Dispose();
        accent.Dispose(); fam.Dispose(); font.Dispose(); textBrush.Dispose(); fmt.Dispose();

        MemoryStream outMs = new MemoryStream();
        bmp.Save(outMs, ImageFormat.Png);
        bmp.Dispose();
        return outMs.ToArray();
    }
}
'@

Add-Type -TypeDefinition $src -ReferencedAssemblies @('System.Drawing')
$root = Split-Path -Parent $PSScriptRoot
$outPath = Join-Path $root "src\NotepadPro\Resources\notepadpro.ico"
[IconGen]::Generate($outPath)
Write-Host "Icon written to $outPath ($((Get-Item $outPath).Length) bytes)"
