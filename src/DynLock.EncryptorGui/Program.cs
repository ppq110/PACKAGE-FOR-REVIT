using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using DynLock.Core;

namespace DynLock.EncryptorGui
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbedded;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Run();
        }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void Run()
        {
            System.Drawing.Icon appIcon = null;
            try
            {
                using (var s = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("DynLock.EncryptorGui.Resources.BimDynamo.ico"))
                {
                    if (s != null)
                    {
                        var bmp = ExtractIcoFrame(s, 32);
                        if (bmp != null)
                            appIcon = System.Drawing.Icon.FromHandle(bmp.GetHicon());
                    }
                }
            }
            catch
            {
            }

            if (!Auth.AuthServerConfig.TryLoad(out _, out string configError))
            {
                MessageBox.Show(
                    "BIMLab auth server is not configured.\n\n" +
                    configError + "\n\n" +
                    "Set these environment variables:\n" +
                    DynLockRuntimeConfig.AuthServerUrlEnvVar + "\n" +
                    "or create:\n" + Auth.AuthServerConfig.AuthServerConfigPath,
                    "BIMLab Studio - Missing auth server configuration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using (var login = new Forms.LoginForm())
            {
                if (appIcon != null)
                    login.Icon = appIcon;

                if (login.ShowDialog() != DialogResult.OK)
                    return;
            }

            var form = new MainForm();
            if (appIcon != null)
                form.Icon = appIcon;
            Application.Run(form);
        }

        private static System.Drawing.Bitmap ExtractIcoFrame(Stream icoStream, int targetSize)
        {
            var raw = new byte[icoStream.Length];
            icoStream.Read(raw, 0, raw.Length);

            int count = BitConverter.ToUInt16(raw, 4);
            int bestIdx = 0;
            int bestDiff = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                int b = 6 + i * 16;
                int w = raw[b] == 0 ? 256 : raw[b];
                int d = Math.Abs(w - targetSize);
                if (d < bestDiff)
                {
                    bestDiff = d;
                    bestIdx = i;
                }
            }

            int entryBase = 6 + bestIdx * 16;
            int dataSize = (int)BitConverter.ToUInt32(raw, entryBase + 8);
            int dataOff = (int)BitConverter.ToUInt32(raw, entryBase + 12);

            using (var ms = new MemoryStream(raw, dataOff, dataSize))
            {
                var src = new System.Drawing.Bitmap(ms);
                if (src.Width == targetSize && src.Height == targetSize)
                    return src;

                var resized = new System.Drawing.Bitmap(targetSize, targetSize);
                using (var g = System.Drawing.Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(src, 0, 0, targetSize, targetSize);
                }
                src.Dispose();
                return resized;
            }
        }

        private static Assembly ResolveEmbedded(object sender, ResolveEventArgs args)
        {
            string wanted = new AssemblyName(args.Name).Name + ".dll";
            var self = Assembly.GetExecutingAssembly();
            using (Stream s = self.GetManifestResourceStream(wanted))
            {
                if (s == null) return null;
                var bytes = new byte[s.Length];
                s.Read(bytes, 0, bytes.Length);
                return Assembly.Load(bytes);
            }
        }
    }
}
