using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ScrapeMonet {
    public static class Program {
        private const string CacheMonetBasePath = "http://cachemonet.com/";

        public static void Main(string[] args) {
            try {
                // run an async task on the threadpool
                Task.Run(MainAsync).Wait();
            }
            catch (Exception e) {
                // catch all for any errors
                WriteLog("Error: {0}", e.ToString());
            }
            finally {
                // wait for enter before exiting
                Console.WriteLine("Press enter to exit...");
                Console.ReadLine();
            }
        }

        public static async Task MainAsync() {
            DateTime startTime = DateTime.Now;

            WriteLog("Now scraping CacheMonet");
            List<Task> tasks = new List<Task>();

            tasks.Add(DownloadFilesFromJsonUrl(CacheMonetBasePath + "json/bg.json", CacheMonetBasePath, "bg"));
            tasks.Add(DownloadFilesFromJsonUrl(CacheMonetBasePath + "json/center.json", CacheMonetBasePath, "center"));
            tasks.Add(DownloadMiscFromUrl(CacheMonetBasePath));

            // start all scraping tasks and wait for them to complete
            await Task.WhenAll(tasks);

            DateTime endTime = DateTime.Now;
            TimeSpan scrapeDuration = endTime - startTime;

            WriteLog("Scrape complete! Scrape took {0}", scrapeDuration.ToString());
        }

        public static async Task DownloadMiscFromUrl(string url) {
            // scrape the main html page and download anything that looks interesting
            // (this includes the main song, various gifs and other images)
            string htmlText;

            using (WebClient webClient = new WebClient()) {
                htmlText = await webClient.DownloadStringTaskAsync(new Uri(url));
            }

            // assuming all strings surrounded by "" close, every second (odd indicies) string in the split of " will yield all quoted strings.
            var quoteTexts = htmlText.Split('\"').Where((item, index) => index % 2 != 0); ;

            // now we have a list of all text that was surrounded by quotes.
            // we want to select any of them that look like gif urls or other interesting resource urls

            // define extensions that are interesting
            List<string> endsWithExtensions = new List<string>() { ".gif", ".png", ".jpg", ".ico", ".mp3", ".ogg", ".mp4", ".aac" };

            // this list will be populated by full urls to the resources
            List<string> downloadUrls = new List<string>();

            foreach (string quotesString in quoteTexts) {
                // check if this string is interesting
                bool match = false;
                // check if the string ends with an interesting extension
                foreach (string matchString in endsWithExtensions) {
                    if (quotesString.EndsWith(matchString, StringComparison.InvariantCultureIgnoreCase)) {
                        match = true;
                        break;
                    }
                }

                // if this string is interesting, add it do the downloads list
                if (match) {
                    if (quotesString.StartsWith(CacheMonetBasePath, StringComparison.InvariantCultureIgnoreCase)) {
                        downloadUrls.Add(quotesString);
                    }
                    else {
                        downloadUrls.Add(CacheMonetBasePath + quotesString);
                    }
                }
            }

            // download all the urls we found
            await DownloadFileList(downloadUrls, "misc");
        }

        public static async Task DownloadFilesFromJsonUrl(string url, string downloadBasePath, string destinationDirectory) {
            // download everything listed in the json
            WriteLog("Downloading all files from within {0}", url);

            string jsonText = null;
            using (WebClient webClient = new WebClient()) {
                // download the json as a string
                jsonText = await webClient.DownloadStringTaskAsync(new Uri(url));
            }

            List<string> downloadUrls = new List<string>();
            // assume the json is in a string array format and deserialise it
            JsonConvert.PopulateObject(jsonText, downloadUrls);

            // make each path a full path
            for(int i = 0; i < downloadUrls.Count; i++) {
                downloadUrls[i] = downloadBasePath + downloadUrls[i];
            }

            // download all the files
            await DownloadFileList(downloadUrls, destinationDirectory);

            WriteLog("Downloaded all files from within {0}", url);
        }

        public static async Task DownloadFileList(List<string> fileUrls, string destinationDirectory) {
            // create the destination directory paths if it doesn't exist
            Directory.CreateDirectory(destinationDirectory);

            // download all of the files simultaniously
            await Task.WhenAll(fileUrls.Select(s => Task.Run(async () => {
                try {
                    await DownloadFile(s, destinationDirectory);
                }
                catch (Exception e) {
                    WriteLog("Error downloading {0} - Details: {1}", s, e.Message);
                }
            }
            )));

            //await Task.Run(() => Parallel.ForEach(fileUrls, async url => await DownloadFile(url, destinationDirectory)));
        }

        public static async Task DownloadFile(string fileUrl, string destinationDirectory) {
            using (WebClient webClient = new WebClient()) {
                WriteLog("Downloading file {0}", fileUrl);
                await webClient.DownloadFileTaskAsync(fileUrl, destinationDirectory + "/" + fileUrl.Split('/').Last());
                WriteLog("Downloaded file {0}", fileUrl);
            }
        }

        public static void WriteLog(string message, params string[] args) {
            // log line with timestamp
            Console.WriteLine("[{0}] {1}", DateTime.Now.ToString("o"), string.Format(message, args));
        }
    }
}
