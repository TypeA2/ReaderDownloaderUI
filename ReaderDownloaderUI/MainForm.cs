using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Eto.Forms;
using Eto.Drawing;
using Newtonsoft.Json;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace ReaderDownloaderUI {
    public struct SettingsWrapper {
        public string Username {
            get => UsernameGetter();
            set => UsernameSetter(value);
        }

        public string Password {
            get => EncryptionProvider.Decrypt(PasswordGetter());
            set => PasswordSetter(EncryptionProvider.Encrypt(value));
        }

        public delegate string Getter();
        public delegate void Setter(string value);

        public Getter UsernameGetter;
        public Setter UsernameSetter;
        public Getter PasswordGetter;
        public Setter PasswordSetter;
    }
    public partial class MainForm : Form {
        private TableLayout CentralLayout => (TableLayout) Content;

        private SettingsWrapper settings;

        public MainForm(SettingsWrapper settings) {
            this.settings = settings;

            Title = "Readers online offline";
            LoginForm();
        }

        private readonly TextBox username_input = new TextBox { PlaceholderText = "s1234567", ID = "USERNAME" };
        private readonly PasswordBox password_input = new PasswordBox { ID = "PASSWORD" };
        private readonly Button login_button = new Button { Text = "Login" };
        private readonly Label status_label = new Label();

        private void LoginForm() {
            ClientSize = new Size(360, 150);
            Resizable = false;
            Maximizable = false;

            login_button.Click += LoginButtonHandler;

            username_input.TextInput += TextInputHandler;
            username_input.TextChanged += TextChangedHandler;
            password_input.TextInput += TextInputHandler;
            password_input.TextChanged += TextChangedHandler;

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

            username_input.Text = settings.Username;
            password_input.Text = settings.Password;
        }

        private readonly ListBox reader_list = new ListBox();
        private readonly Button download_button = new Button { Text = "Download", Enabled = false };

        private void DownloadForm(IEnumerable<ReaderEntry> readers) {
            ClientSize = new Size(960, 540);
            Resizable = true;
            Maximizable = true;


            SizeChanged += (_, __) => reader_list.Height = ClientSize.Height - 48;
            OnSizeChanged(null);
            
            Content = new TableLayout {
                Spacing = new Size(5, 5),
                Padding = new Padding(10),
                Rows = { reader_list, download_button, null }
            };

            reader_list.DataStore = readers.Select(e => (object) e);

            reader_list.SelectedKeyChanged += (_, __) => download_button.Enabled = true;
            download_button.Click += DownloadButtonHandler;
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

        private static void TextInputHandler(object obj, TextInputEventArgs args) {
            TextControl control = (TextControl) obj;

            if (!String.IsNullOrWhiteSpace(control.Text)) {
                control.BackgroundColor = Color.FromRgb(0xffffff);
            }
        }

        private void TextChangedHandler(object obj, EventArgs args) {
            TextControl control = (TextControl) obj;

            switch (control.ID) {
                case "USERNAME":
                    settings.Username = control.Text;
                    break;

                case "PASSWORD":
                    settings.Password = control.Text;
                    break;
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
            IEnumerable<ReaderEntry> readers;

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
                readers = await GetReaders();

                if (readers == null) {
                    Status("Login failed.");
                    return;
                }

                Status();
            }

            DownloadForm(readers);
        }

        private void DownloadButtonHandler(object obj, EventArgs args) {
            ReaderEntry reader = (ReaderEntry) reader_list.SelectedValue;

            SaveFileDialog dialog = new SaveFileDialog {
                Title = "Save as...",
                CheckFileExists = false,
                Directory = new Uri(Utils.GetDownloadsFolder()),
                FileName = $"{reader.name}.pdf".ValidPath(),
                Filters = { new FileFilter("PDF Document", ".pdf") }
            };

            if (dialog.ShowDialog(Application.Instance.MainForm) == DialogResult.Ok) {
                DownloadReader(reader, dialog.FileName);
            }
        }

        struct PDFFragment {
            public bool Success { get; set; }
            public string File { get; set; }
        }

        private async void DownloadReader(ReaderEntry reader, string path) {
            download_button.Enabled = false;
            download_button.Text = "Retrieving pages...";

            (uint page_count, string reader_code) = await GetPageCountAndReaderCode(reader.index);

            download_button.Text = $"Downloading 0 / {page_count}";

            PdfDocument output = new PdfDocument(path);

            Uri default_referrer = WebHelper.Client.DefaultRequestHeaders.Referrer;
            WebHelper.Client.DefaultRequestHeaders.Referrer =
                new Uri("https://www.readeronline.leidenuniv.nl/reader/www/nodes/index/{reader.index}");

            for (uint i = 1; i <= page_count; ++i) {
                download_button.Text = $"Downloading {i} / {page_count}";
                string url =
                    $"https://www.readeronline.leidenuniv.nl/reader/www/nodes/nodes/get_pdf?reader_id={reader.index}&reader_code={reader_code}&page_number={i}";

                PDFFragment result = JsonConvert.DeserializeObject<PDFFragment>(await WebHelper.Client.GetStringAsync(url));

                MemoryStream stream = new MemoryStream(Convert.FromBase64String(result.File));

                PdfDocument page = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
                output.AppendPdf(page);
            }

            WebHelper.Client.DefaultRequestHeaders.Referrer = default_referrer;

            output.Close();

            download_button.Text = "Download";
            download_button.Enabled = true;
        }
    }
}
