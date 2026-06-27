using System;
using System.Drawing;

namespace DynLock.Core
{
    /// <summary>
    /// Helper class to generate BIMLab DynLock logos and icons programmatically.
    /// Generates lock-based security icons representing encryption functionality.
    /// </summary>
    public static class LogoHelper
    {
        // BIMLab Colors
        private static readonly Color BimLabBlue = Color.FromArgb(25, 118, 210);     // Primary blue
        private static readonly Color BimLabDarkBlue = Color.FromArgb(13, 71, 161);  // Dark blue
        private static readonly Color AccentGold = Color.FromArgb(255, 193, 7);      // Gold accent
        private static readonly Color DarkGray = Color.FromArgb(48, 48, 48);         // Dark background

        /// <summary>
        /// Generate main application logo/icon (high resolution for forms).
        /// </summary>
        public static Bitmap GenerateLogo(int size = 128)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                DrawLockIcon(g, size, BimLabBlue, AccentGold);
            }
            return bitmap;
        }

        /// <summary>
        /// Generate ribbon button icon for Revit (small size).
        /// </summary>
        public static Bitmap GenerateRibbonIcon(int size = 32)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                DrawLockIcon(g, size, BimLabBlue, AccentGold, scale: 0.85f);
            }
            return bitmap;
        }

        /// <summary>
        /// Generate large icon for window title and toolbar.
        /// </summary>
        public static Bitmap GenerateLargeIcon(int size = 256)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.FromArgb(245, 245, 245));
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw background circle
                var bgBrush = new SolidBrush(BimLabBlue);
                g.FillEllipse(bgBrush, size * 0.1f, size * 0.1f, size * 0.8f, size * 0.8f);
                bgBrush.Dispose();
                
                DrawLockIcon(g, size, Color.White, AccentGold, scale: 0.9f);
            }
            return bitmap;
        }

        /// <summary>
        /// Generate small icon for tray or status (16x16).
        /// </summary>
        public static Bitmap GenerateSmallIcon(int size = 16)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                DrawLockIcon(g, size, BimLabBlue, AccentGold, scale: 0.8f);
            }
            return bitmap;
        }

        /// <summary>
        /// Render full BIMLab logo (3 bars + "iM|Lab" text) - dung cho Revit ribbon button.
        /// </summary>
        public static Bitmap GenerateBimLabLogo(int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                float w = width, h = height;
                var blue = Color.FromArgb(28, 82, 145);   // BIMLab navy blue

                // === 3 thanh ngang ben trai ===
                float barZoneW = w * 0.28f;               // vung danh cho 3 bar
                float barH     = h * 0.17f;               // chieu cao moi bar
                float gap      = h * 0.13f;               // khoang cach giua cac bar
                float barsTopY = (h - (3 * barH + 2 * gap)) / 2f;
                float barX     = w * 0.03f;

                using (var br = new SolidBrush(blue))
                {
                    // Bar tren (ngan nhat - ~62%)
                    g.FillRectangle(br, barX, barsTopY,                    barZoneW * 0.62f, barH);
                    // Bar giua (dai nhat - 100%)
                    g.FillRectangle(br, barX, barsTopY + barH + gap,       barZoneW,         barH);
                    // Bar duoi (trung binh - ~78%)
                    g.FillRectangle(br, barX, barsTopY + 2*(barH + gap),   barZoneW * 0.78f, barH);
                }

                // === Text "iM|Lab" ===
                float textX   = w * 0.34f;
                float imSize  = h * 0.65f;
                float labSize = h * 0.44f;

                using (var fntIM  = new Font("Arial", imSize,  FontStyle.Bold,    GraphicsUnit.Pixel))
                using (var fntLab = new Font("Arial", labSize, FontStyle.Regular,  GraphicsUnit.Pixel))
                using (var br     = new SolidBrush(blue))
                {
                    var imMetrics  = g.MeasureString("iM", fntIM);
                    var sepMetrics = g.MeasureString("|",  fntIM);
                    var labMetrics = g.MeasureString("Lab", fntLab);

                    float baseY = (h - imMetrics.Height) / 2f;

                    // "iM" - bold, can giua chieu cao
                    g.DrawString("iM",  fntIM,  br, textX,                                baseY);
                    // "|"  - cung font voi "iM"
                    g.DrawString("|",   fntIM,  br, textX + imMetrics.Width  - w * 0.02f, baseY);
                    // "Lab" - nho hon, can day voi "iM"
                    float labY = baseY + imMetrics.Height - labMetrics.Height + h * 0.04f;
                    g.DrawString("Lab", fntLab, br, textX + imMetrics.Width + sepMetrics.Width - w * 0.04f, labY);
                }
            }
            return bmp;
        }

        /// <summary>
        /// Draw a lock icon with encryption theme.
        /// </summary>
        private static void DrawLockIcon(Graphics g, int size, Color lockColor, Color accentColor, float scale = 1f)
        {
            var margin = size * (1 - scale) / 2;
            var workSize = size - margin * 2;

            // Lock body (rectangle with rounded corners)
            var bodyRect = new RectangleF(
                margin + workSize * 0.2f,
                margin + workSize * 0.5f,
                workSize * 0.6f,
                workSize * 0.4f
            );

            using (var brush = new SolidBrush(lockColor))
            using (var pen = new Pen(lockColor, 2))
            {
                g.FillRectangle(brush, bodyRect);
                
                // Draw lock body border
                g.DrawRectangle(pen, bodyRect.X, bodyRect.Y, bodyRect.Width, bodyRect.Height);
                
                // Lock shackle (arc/semicircle)
                var shackleX = margin + workSize * 0.3f;
                var shackleY = margin + workSize * 0.15f;
                var shackleWidth = workSize * 0.4f;
                var shackleHeight = workSize * 0.4f;
                
                g.DrawArc(pen, shackleX, shackleY, shackleWidth, shackleHeight, 0, 180);
                
                // Keyhole circle
                var keyholeCenterX = margin + workSize * 0.5f;
                var keyholeCenterY = margin + workSize * 0.65f;
                var keyholeRadius = workSize * 0.08f;
                
                using (var keyholeColor = new SolidBrush(lockColor))
                {
                    g.FillEllipse(keyholeColor, 
                        keyholeCenterX - keyholeRadius,
                        keyholeCenterY - keyholeRadius,
                        keyholeRadius * 2,
                        keyholeRadius * 2);
                }
            }

            // Add accent shine/spark for encryption effect
            if (accentColor != Color.Transparent)
            {
                using (var accentBrush = new SolidBrush(accentColor))
                {
                    var sparkX = margin + workSize * 0.75f;
                    var sparkY = margin + workSize * 0.25f;
                    var sparkSize = workSize * 0.1f;
                    
                    g.FillEllipse(accentBrush, sparkX, sparkY, sparkSize, sparkSize);
                    
                    // Small accent lines
                    using (var accentPen = new Pen(accentColor, 1))
                    {
                        g.DrawLine(accentPen, sparkX + sparkSize * 1.2f, sparkY, sparkX + sparkSize * 2f, sparkY);
                        g.DrawLine(accentPen, sparkX, sparkY + sparkSize * 1.2f, sparkX, sparkY + sparkSize * 2f);
                    }
                }
            }
        }
    }
}
