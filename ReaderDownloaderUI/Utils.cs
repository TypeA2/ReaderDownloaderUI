using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PdfSharp.Pdf;

namespace ReaderDownloaderUI {
    internal static class Utils {
        public static bool IsLinux {
            get {
                int p = (int) Environment.OSVersion.Platform;
                return p == 4 || p == 6 || p == 128;
            }
        }

        private static string download_folder_id = "{374DE290-123F-4565-9164-39C4925E467B}";

        [DllImport("Shell32.dll")]
        private static extern int SHGetKnownFolderPath(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint flags, IntPtr token, out IntPtr path);

        public static string GetDownloadsFolder() {
            if (IsLinux) {
                return Path.GetFullPath("~/Downloads");
            }

            int result = SHGetKnownFolderPath(
                new Guid(download_folder_id), 0, new IntPtr(0), out IntPtr out_path);

            if (result < 0) {
                throw new ExternalException("Failed to retrieve known path using SHGetKnownFolderPath");
            }

            string path = Marshal.PtrToStringUni(out_path);
            Marshal.FreeCoTaskMem(out_path);
            return path;
        }

        public static string ValidPath(this string str) {
            return Path.GetInvalidFileNameChars()
                .Aggregate(str, (current, c) => current.Replace(c, '_'));
        }

        public static void AppendPdf(this PdfDocument doc, PdfDocument append) {
            foreach (PdfPage page in append.Pages) {
                doc.AddPage(page);
            }
        }
    }
}
