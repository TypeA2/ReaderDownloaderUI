using System;
using System.Diagnostics;
using Eto.Forms;
namespace ReaderDownloaderUI.Desktop {
    internal class Program {
        [STAThread]
        private static void Main(string[] args) {
            
            SettingsWrapper settings = new SettingsWrapper {
                UsernameGetter = () => Properties.Settings.Default.CurrentUsername,
                UsernameSetter = value => Properties.Settings.Default.CurrentUsername = value,
                PasswordGetter = () => Properties.Settings.Default.CurrentPassword,
                PasswordSetter = value => Properties.Settings.Default.CurrentPassword = value
            };

            Application app = new Application(Eto.Platform.Detect);
            app.Terminating += (_, __) => Properties.Settings.Default.Save();
            app.Run(new MainForm(settings));
        }
    }
}