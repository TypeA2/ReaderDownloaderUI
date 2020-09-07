using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ReaderDownloaderUI {
    public static class WebHelper {
        public static HttpClient Client = new HttpClient();
    }
}
