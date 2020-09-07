using System;
using Eto.Drawing;

namespace ReaderDownloaderUI {
    partial class MainForm {
        private bool IsReadyForLogin() {
            bool failed = false;

            username_input.BackgroundColor = Color.FromRgb(0xffffff);
            password_input.BackgroundColor = Color.FromRgb(0xffffff);

            if (String.IsNullOrWhiteSpace(username_input.Text)) {
                failed = true;
                username_input.BackgroundColor = Color.FromRgb(0xff0000);
            }

            if (String.IsNullOrWhiteSpace(password_input.Text)) {
                failed = true;
                password_input.BackgroundColor = Color.FromRgb(0xff0000);
            }

            return !failed;
        }
    }
}
