using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace DynLock.Addin.UI
{
    /// <summary>
    /// Load icon tu BimDynamo.ico (PNG-based ICO) nhung trong DLL.
    /// GDI+ Icon.ToBitmap() không đọc được PNG-ICO -> cần extract PNG bytes thủ công.
    /// </summary>
    internal static class AddinIcons
    {
        // Ve icon nguoi dung (person silhouette) - dung cho nut Login tren ribbon.
        public static Bitmap LoginIcon(int size = 32)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                float s = size;
                using (var brush = new SolidBrush(Color.FromArgb(30, 120, 215)))
                {
                    // Dau: hinh tron o tren giua
                    float hd = s * 0.40f;
                    float hx = (s - hd) / 2f;
                    float hy = s * 0.04f;
                    g.FillEllipse(brush, hx, hy, hd, hd);

                    // Vai/than: nua tren cua ellipse rong o phan duoi
                    float bw = s * 0.86f;
                    float bh = s * 0.68f;
                    float bx = (s - bw) / 2f;
                    float by = s * 0.50f;

                    // Chi hien nua tren cua ellipse (hinh vai cong)
                    g.SetClip(new RectangleF(0f, by, s, bh * 0.52f));
                    g.FillEllipse(brush, bx, by, bw, bh);
                    g.ResetClip();
                }
            }
            return bmp;
        }

        public static Bitmap HeaderIcon(int size = 32)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream("DynLock.Addin.Resources.BimDynamo.ico"))
                {
                    if (s == null) return null;
                    return ExtractPngFrame(s, size);
                }
            }
            catch { return null; }
        }

        public static Bitmap QuestionIcon(int size = 32)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                float s = size;
                using (var bg = new SolidBrush(Color.FromArgb(238, 242, 248)))
                using (var border = new Pen(Color.FromArgb(92, 112, 145), Math.Max(1f, s * 0.055f)))
                using (var textBrush = new SolidBrush(Color.FromArgb(52, 72, 108)))
                using (var font = new Font("Segoe UI", s * 0.68f, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    g.FillEllipse(bg, s * 0.06f, s * 0.06f, s * 0.88f, s * 0.88f);
                    g.DrawEllipse(border, s * 0.06f, s * 0.06f, s * 0.88f, s * 0.88f);

                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                    };
                    g.DrawString("?", font, textBrush, new RectangleF(0, -s * 0.04f, s, s), sf);
                }
            }
            return bmp;
        }

        public static Bitmap AddIcon(int size = 32)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                float s = size;
                using (var pen = new Pen(Color.FromArgb(45, 112, 178), Math.Max(2f, s * 0.09f)))
                {
                    g.DrawEllipse(pen, s * 0.08f, s * 0.08f, s * 0.84f, s * 0.84f);
                }

                using (var plus = new Pen(Color.FromArgb(32, 132, 218), Math.Max(4f, s * 0.16f)))
                {
                    plus.StartCap = LineCap.Round;
                    plus.EndCap = LineCap.Round;
                    g.DrawLine(plus, s * 0.30f, s * 0.50f, s * 0.70f, s * 0.50f);
                    g.DrawLine(plus, s * 0.50f, s * 0.30f, s * 0.50f, s * 0.70f);
                }
            }
            return bmp;
        }

        public static Bitmap PileModelingIcon(int size = 32)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                float s = size;
                Color dark = Color.FromArgb(0, 82, 158);
                Color mid = Color.FromArgb(38, 132, 218);
                Color light = Color.FromArgb(190, 229, 255);

                PointF top = new PointF(s * 0.50f, s * 0.08f);
                PointF left = new PointF(s * 0.16f, s * 0.27f);
                PointF right = new PointF(s * 0.84f, s * 0.27f);
                PointF front = new PointF(s * 0.50f, s * 0.45f);

                using (var brush = new LinearGradientBrush(new RectangleF(0, 0, s, s), light, mid, 45f))
                using (var pen = new Pen(dark, Math.Max(1f, s * 0.04f)))
                {
                    g.FillPolygon(brush, new[] { top, right, front, left });
                    g.DrawPolygon(pen, new[] { top, right, front, left });
                }

                DrawPile(g, s, s * 0.28f, s * 0.37f, s * 0.12f, s * 0.48f, light, mid, dark);
                DrawPile(g, s, s * 0.50f, s * 0.43f, s * 0.12f, s * 0.54f, light, mid, dark);
                DrawPile(g, s, s * 0.72f, s * 0.37f, s * 0.12f, s * 0.48f, light, mid, dark);

                using (var brush = new LinearGradientBrush(new RectangleF(s * 0.38f, s * 0.15f, s * 0.24f, s * 0.22f), light, dark, 45f))
                using (var pen = new Pen(dark, Math.Max(1f, s * 0.035f)))
                {
                    var block = new RectangleF(s * 0.39f, s * 0.15f, s * 0.22f, s * 0.20f);
                    g.FillRectangle(brush, block);
                    g.DrawRectangle(pen, block.X, block.Y, block.Width, block.Height);
                }
            }
            return bmp;
        }

        public static Bitmap JoinAllIcon(int size = 32)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                float s = size;
                Color dark = Color.FromArgb(0, 82, 158);
                Color mid = Color.FromArgb(35, 130, 218);
                Color light = Color.FromArgb(194, 232, 255);

                DrawCube(g, new RectangleF(s * 0.08f, s * 0.42f, s * 0.19f, s * 0.19f), light, mid, dark);
                DrawCube(g, new RectangleF(s * 0.12f, s * 0.70f, s * 0.24f, s * 0.17f), light, mid, dark);
                DrawBeam(g, new RectangleF(s * 0.12f, s * 0.14f, s * 0.24f, s * 0.18f), light, mid, dark);

                using (var pen = new Pen(Color.FromArgb(80, 45, 145, 225), Math.Max(2f, s * 0.09f)))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.ArrowAnchor;
                    g.DrawBezier(pen, s * 0.38f, s * 0.24f, s * 0.56f, s * 0.30f, s * 0.53f, s * 0.55f, s * 0.70f, s * 0.54f);
                    g.DrawLine(pen, s * 0.32f, s * 0.52f, s * 0.70f, s * 0.54f);
                    g.DrawBezier(pen, s * 0.38f, s * 0.78f, s * 0.55f, s * 0.76f, s * 0.52f, s * 0.58f, s * 0.70f, s * 0.54f);
                }

                DrawCube(g, new RectangleF(s * 0.70f, s * 0.34f, s * 0.23f, s * 0.38f), light, mid, dark);
                DrawCube(g, new RectangleF(s * 0.64f, s * 0.70f, s * 0.30f, s * 0.17f), light, mid, dark);
            }
            return bmp;
        }

        public static Bitmap PluginIcon(string iconPath, int size = 32, string pluginName = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                {
                    using (var src = new Bitmap(iconPath))
                    {
                        var resized = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (var g = Graphics.FromImage(resized))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.Clear(Color.Transparent);
                            g.DrawImage(src, 0, 0, size, size);
                        }
                        return resized;
                    }
                }
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(pluginName))
            {
                if (pluginName.IndexOf("pile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    pluginName.IndexOf("coc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    pluginName.IndexOf("c\u1ecdc", StringComparison.OrdinalIgnoreCase) >= 0)
                    return PileModelingIcon(size);

                if (pluginName.IndexOf("join", StringComparison.OrdinalIgnoreCase) >= 0)
                    return JoinAllIcon(size);
            }

            return QuestionIcon(size);
        }

        private static void DrawPile(Graphics g, float s, float cx, float y, float w, float h, Color light, Color mid, Color dark)
        {
            var rect = new RectangleF(cx - w / 2f, y, w, h);
            using (var brush = new LinearGradientBrush(rect, light, mid, 0f))
            using (var pen = new Pen(dark, Math.Max(1f, s * 0.03f)))
            {
                g.FillRectangle(brush, rect);
                g.DrawLine(pen, rect.Left, rect.Top, rect.Left, rect.Bottom);
                g.DrawLine(pen, rect.Right, rect.Top, rect.Right, rect.Bottom);
                g.DrawArc(pen, rect.Left, rect.Bottom - w * 0.55f, rect.Width, w * 0.55f, 0, 180);
            }
        }

        private static void DrawCube(Graphics g, RectangleF r, Color light, Color mid, Color dark)
        {
            float d = Math.Min(r.Width, r.Height) * 0.24f;
            PointF a = new PointF(r.Left + d, r.Top);
            PointF b = new PointF(r.Right, r.Top + d);
            PointF c = new PointF(r.Right - d, r.Bottom);
            PointF e = new PointF(r.Left, r.Bottom - d);
            PointF f = new PointF(r.Left, r.Top + d);
            PointF center = new PointF(r.Left + r.Width / 2f, r.Top + r.Height / 2f);

            using (var top = new SolidBrush(light))
            using (var side = new LinearGradientBrush(r, mid, dark, 0f))
            using (var front = new LinearGradientBrush(r, light, mid, 90f))
            using (var pen = new Pen(dark, 1f))
            {
                g.FillPolygon(top, new[] { a, b, center, f });
                g.FillPolygon(side, new[] { b, c, center });
                g.FillPolygon(front, new[] { f, center, c, e });
                g.DrawPolygon(pen, new[] { a, b, c, e, f });
            }
        }

        private static void DrawBeam(Graphics g, RectangleF r, Color light, Color mid, Color dark)
        {
            using (var brush = new LinearGradientBrush(r, light, mid, 90f))
            using (var pen = new Pen(dark, 1f))
            {
                g.FillRectangle(brush, r);
                g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
                var web = new RectangleF(r.Left + r.Width * 0.36f, r.Top + r.Height * 0.18f, r.Width * 0.28f, r.Height * 0.64f);
                g.FillRectangle(new SolidBrush(Color.FromArgb(120, Color.White)), web);
                g.DrawRectangle(pen, web.X, web.Y, web.Width, web.Height);
            }
        }

        /// <summary>
        /// Parse ICO header, tim frame gan nhat voi targetSize,
        /// extract PNG bytes roi tao Bitmap.
        /// </summary>
        private static Bitmap ExtractPngFrame(Stream icoStream, int targetSize)
        {
            var raw = new byte[icoStream.Length];
            icoStream.Read(raw, 0, raw.Length);

            int count = BitConverter.ToUInt16(raw, 4);

            // Tim frame co kich thuoc gan targetSize nhat
            int bestIdx  = 0;
            int bestDiff = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                int b = 6 + i * 16;
                int w = raw[b] == 0 ? 256 : raw[b];
                int d = Math.Abs(w - targetSize);
                if (d < bestDiff) { bestDiff = d; bestIdx = i; }
            }

            int entryBase = 6 + bestIdx * 16;
            int dataSize  = (int)BitConverter.ToUInt32(raw, entryBase + 8);
            int dataOff   = (int)BitConverter.ToUInt32(raw, entryBase + 12);

            // Kiểm tra frame có phải PNG (magic bytes 89 50 4E 47)
            bool isPng = (dataOff + 4 < raw.Length)
                      && raw[dataOff]     == 0x89
                      && raw[dataOff + 1] == 0x50
                      && raw[dataOff + 2] == 0x4E
                      && raw[dataOff + 3] == 0x47;

            Bitmap bmp;
            if (isPng)
            {
                using (var ms = new MemoryStream(raw, dataOff, dataSize))
                    bmp = new Bitmap(ms);
            }
            else
            {
                // Fallback cho BMP-based frame
                using (var ms = new MemoryStream(raw, dataOff, dataSize))
                    bmp = new Bitmap(ms);
            }

            // Resize ve dung targetSize neu can
            if (bmp.Width == targetSize && bmp.Height == targetSize)
                return bmp;

            var resized = new Bitmap(targetSize, targetSize);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, 0, 0, targetSize, targetSize);
            }
            bmp.Dispose();
            return resized;
        }
    }
}
