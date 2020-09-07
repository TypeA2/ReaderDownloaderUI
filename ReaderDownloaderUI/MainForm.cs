using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using Eto.Forms;
using Eto.Drawing;
using HtmlAgilityPack;

namespace ReaderDownloaderUI {
    public partial class MainForm : Form {
        private TableLayout CentralLayout => (TableLayout) Content;

        public MainForm() {
            Title = "Readers online offline";
            LoginForm();
        }

        private readonly TextBox username_input = new TextBox {
            PlaceholderText = "s1234567"
        };

        private readonly PasswordBox password_input = new PasswordBox();

        private readonly Button login_button = new Button {
            Text = "Login"
        };

        private readonly Label status_label = new Label();

        private void LoginForm() {
            ClientSize = new Size(360, 160);

            login_button.Click += LoginButtonHandler;

            username_input.TextInput += TextInputHandler;
            password_input.TextInput += TextInputHandler;
            
            KeyDown += KeyDownHandler;

            Content = new TableLayout {
                Spacing = new Size(5, 5),
                Padding = new Padding(40, 15),
                Rows = {
                    new TableRow(new TableCell(), "Please login:"),
                    new TableRow("ULCN username:", username_input),
                    new TableRow("ULCN password:", password_input),
                    new TableRow(new TableCell(), login_button),
                    new TableRow(new TableCell(), status_label),
                    null
                }
            };
        }

        private void Status(string msg = "") {
            status_label.Text = msg;
        }

        private void EnableUI(bool enabled) {
            username_input.Enabled = enabled;
            password_input.Enabled = enabled;
            login_button.Enabled = enabled;
        }

        private void KeyDownHandler(object obj, KeyEventArgs args) {
            if (args.Key == Keys.Enter) {
                LoginButtonHandler(login_button, null);
            }
        }

        private void TextInputHandler(object obj, TextInputEventArgs args) {
            TextControl control = (TextControl) obj;

            if (!String.IsNullOrWhiteSpace(control.Text)) {
                control.BackgroundColor = Color.FromRgb(0xffffff);
            }
        }

        private class EnableUILock : IDisposable {
            private readonly MainForm _form;
            private readonly bool _enable;
            public EnableUILock(MainForm form, bool enable) {
                _form = form;
                _enable = enable;
                _form.EnableUI(_enable);
            }

            public void Dispose() {
                _form.EnableUI(!_enable);
            }
        }

        private async void LoginButtonHandler(object obj, EventArgs args) {
            using (_ = new EnableUILock(this, false)) {
                Status();

                if (!IsReadyForLogin()) {
                    Status("All fields must be filled out.");
                    return;
                }

                string key = await GetFormKey();

                Status("Logging in...");
                await Login(key, username_input.Text, password_input.Text);

                Status("Retrieving readers...");
                IEnumerable<ReaderEntry> readers = await GetReaders();

                if (readers == null) {
                    Status("Login failed.");
                    return;
                }

                Status();

                foreach (var reader in readers) {
                    Debug.WriteLine($"{reader.index}: {reader.name} {reader.cover_url}");
                }
            }
        }
    }
}
