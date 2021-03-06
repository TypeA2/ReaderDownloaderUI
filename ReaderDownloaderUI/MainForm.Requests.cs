﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ReaderDownloaderUI {
    internal struct ReaderEntry {
        public string name;
        public int index;
        public string cover_url;

        public override string ToString() {
            return $"{name} ({index})";
        }
    }

    public partial class MainForm {
        private const string login_page_url = "https://www.readeronline.leidenuniv.nl/customer/account/login/";
        private const string login_post_url = "https://www.readeronline.leidenuniv.nl/customer/account/loginPost/";

        private const string reader_list_url = "https://www.readeronline.leidenuniv.nl/reader/www/readers/index";

        private static async Task<string> GetFormKey() {
            Stream login_page = await WebHelper.Client.GetStreamAsync(login_page_url);
            HtmlDocument doc = new HtmlDocument();
            doc.Load(login_page);

            const string form_key_selector = "//form/input[@name=\"form_key\"]";

            string key = doc.DocumentNode
                .SelectSingleNode(form_key_selector)
                .Attributes["value"].Value;

            return key;
        }

        private static async Task Login(string key, string username, string password) {
            Dictionary<string, string> form = new Dictionary<string, string> {
                { "form_key", key },
                { "login[username]", username },
                { "login[password]", password }
            };

            FormUrlEncodedContent content = new FormUrlEncodedContent(form);
            await WebHelper.Client.PostAsync(login_post_url, content);
        }

        private static async Task<IEnumerable<ReaderEntry>> GetReaders() {
            Stream reader_page = await WebHelper.Client.GetStreamAsync(reader_list_url);
            HtmlDocument doc = new HtmlDocument();

            doc.Load(reader_page);

            // This bit seems really volatile
            // div.reader_list div:has(> a > img.reader_cover)
            const string readers_xpath = "//div[@class=\"reader_list\"]/div[@class=\"row\"]/div[not(@*)]/div[@class=\"col-lg-3 col-md-3\"]";

            // a > h3
            const string title_xpath = ".//a/h3";

            // a
            const string url_xpath = ".//a";

            // a > .reader_cover
            const string cover_xpath = ".//a/*[contains(concat(\" \",normalize-space(@class),\" \"),\" reader_cover \")]";

            try {
                return doc.DocumentNode.SelectNodes(readers_xpath)
                    .Select(node => new ReaderEntry {
                        name = node.SelectSingleNode(title_xpath).InnerHtml,
                        index = int.Parse(node.SelectSingleNode(url_xpath).Attributes["href"].Value.Split('/').Last()),
                        cover_url = node.SelectSingleNode(cover_xpath).Attributes["src"].Value
                    });
            } catch (ArgumentNullException) {
                return null;
            }
        }

        private static async Task<(uint, string)> GetPageCountAndReaderCode(int reader_id) {
            string reader_url = $"https://www.readeronline.leidenuniv.nl/reader/www/nodes/index/{reader_id}";

            Stream reader_page = await WebHelper.Client.GetStreamAsync(reader_url);
            HtmlDocument doc = new HtmlDocument();
            doc.Load(reader_page);

            // span#page_count
            const string page_count_xpath = "//span[@id=\"page_count\"]";

            // input#reader_code
            const string reader_code_xpath = "//input[@id=\"reader_code\"]";

            return (UInt32.Parse(doc.DocumentNode.SelectSingleNode(page_count_xpath).GetDataAttribute("value").Value),
                doc.DocumentNode.SelectSingleNode(reader_code_xpath).Attributes["value"].Value);
        }
    }
}
