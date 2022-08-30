using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Net.Http;

namespace ScrapeMonet
{
    public static class Program
    {
        private const string CacheMonetBasePath = "http://cachemonet.com";

        private static HttpClient client;

        public static async Task Main(string[] args)
        {
            DateTime startTime = DateTime.Now;

            WriteLog("Now scraping CacheMonet");

            using (client = new HttpClient()
            {
                BaseAddress = new Uri(CacheMonetBasePath)
            })
            {
                List<Task> tasks = new List<Task>();

                tasks.Add(DownloadFilesFromJsonUrl("/json/bg.json", "bg"));
                tasks.Add(DownloadFilesFromJsonUrl("/json/center.json", "center"));
                tasks.Add(DownloadMiscFromUrl("/"));

                // start all scraping tasks and wait for them to complete
                await Task.WhenAll(tasks);

                DateTime endTime = DateTime.Now;
                TimeSpan scrapeDuration = endTime - startTime;

                WriteLog($"Scrape complete! Scrape took {scrapeDuration}");
            }
        }

        public static async Task DownloadMiscFromUrl(string path)
        {
            // scrape the main html page and download anything that looks interesting
            // (this includes the main song, various gifs and other images)
            string htmlText = await client.GetStringAsync(path);

            // assuming all strings surrounded by "" close, every second (odd indicies) string in the split of " will yield all quoted strings.
            var quoteTexts = htmlText.Split('\"').Where((item, index) => index % 2 != 0); ;

            // now we have a list of all text that was surrounded by quotes.
            // we want to select any of them that look like gif urls or other interesting resource urls

            // define extensions that are interesting
            List<string> endsWithExtensions = new List<string>() { ".gif", ".png", ".jpg", ".ico", ".mp3", ".ogg", ".mp4", ".aac" };

            // this list will be populated by full urls to the resources
            List<string> downloadUrls = quoteTexts
                .Where(s => endsWithExtensions.Any(e => s.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
                .Select(s => s.StartsWith(CacheMonetBasePath, StringComparison.InvariantCultureIgnoreCase) ? s : "/" + s)
                .ToList();

            // download all the urls we found
            await DownloadFileList(downloadUrls, "misc");
        }

        public static async Task DownloadFilesFromJsonUrl(string path, string destinationDirectory)
        {
            // download everything listed in the json
            WriteLog($"Downloading all files from within {path}");

            List<string>? downloadUrls = await client.GetFromJsonAsync<List<string>>(path);

            if (downloadUrls is not null)
            {
                var absolutePaths = downloadUrls.Select(s => "/" + s);

                // download all the files
                await DownloadFileList(absolutePaths, destinationDirectory);

                WriteLog($"Downloaded all files from within {path}");
            }
        }

        public static async Task DownloadFileList(IEnumerable<string> fileUrls, string destinationDirectory)
        {
            // create the destination directory paths if it doesn't exist
            Directory.CreateDirectory(destinationDirectory);

            // download all of the files simultaniously
            await Task.WhenAll(fileUrls.Select(s => Task.Run(async () =>
            {
                try
                {
                    await DownloadFile(s, destinationDirectory);
                }
                catch (Exception e)
                {
                    WriteLog($"Error downloading {s} - Details: {e.Message}");
                }
            }
            )));

            //await Task.Run(() => Parallel.ForEach(fileUrls, async url => await DownloadFile(url, destinationDirectory)));
        }

        public static async Task DownloadFile(string fileUrl, string destinationDirectory)
        {
            string destination = destinationDirectory + "/" + fileUrl.Split('/').Last();
            WriteLog($"Downloading file {fileUrl} -> {destination}");

            await using (FileStream destinationFile = File.OpenWrite(destination))
            {
                using (HttpResponseMessage response = await client.GetAsync(fileUrl))
                {
                    response.EnsureSuccessStatusCode();
                    var stream = await response.Content.ReadAsStreamAsync();
                    await stream.CopyToAsync(destinationFile);
                    await destinationFile.FlushAsync();
                }
            }

            WriteLog($"Downloaded file {fileUrl} -> {destination}");
        }

        public static void WriteLog(string message)
        {
            // log line with timestamp
            Console.WriteLine($"[{DateTime.Now:O}] {message}");
        }
    }
}
