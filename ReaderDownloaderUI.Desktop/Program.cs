using System;
using Eto.Forms;
using Eto.Drawing;

namespace ReaderDownloaderUI.Desktop {
    internal class Program {
        [STAThread]
        private static void Main(string[] args) {
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}