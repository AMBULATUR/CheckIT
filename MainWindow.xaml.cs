using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;


namespace CheckIt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DC dc = new DC();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = dc;

        }

        private void DoWork(object sender, RoutedEventArgs e)
        {
            dc.WorkAsync(urlBox.Text);
        }

        private void Clear(object sender, RoutedEventArgs e)
        {
            dc.Clear();
        }
    }
    public class DC : INotifyPropertyChanged
    {
        Browser browser = null;
        async Task initBrowserAsync()
        {
            try
            {
                WriteLine("Checkout last version of browser, please wait");
                var task = await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            }
            catch (Exception ex)
            {
                WriteLine("Exception, please restart program: " + ex.Message);
                return;
            }
            finally
            {
                WriteLine("Browser checkout done.");
            }

            try
            {
                WriteLine("Starting browser, please wait. ");
                browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true
                });
            }
            catch (Exception ex)
            {
                WriteLine("Please, restart program, exception: " + ex.Message);
                return;
            }
            finally
            {
                WriteLine("Work done, be safe now, waiting for links.");
            }
        }

        public DC()
        {
            var init = initBrowserAsync();
        }
        private string buffer;
        public void WriteLine(string str)
        {
            Buffer += str + Environment.NewLine;
        }
        public void Clear()
        {
            Buffer = "";
        }
        string patternFullURL = @"url\(""(.*)\""\)";
        string patternGood = @"(http.*).jpg"; // Without huiny
        public async Task WorkAsync(string url)
        {
            if (browser == null)
            {
                WriteLine("Please wait, starting...");
                return;
            }
            var page = await browser.NewPageAsync();
            WriteLine("Loading page...");
            await page.GoToAsync(url);
            //https://github.com/puppeteer/puppeteer/issues/3051
            WriteLine("Parsing data: ");

            await page.WaitForSelectorAsync(".section-tagline");
            var tag = await page.EvaluateFunctionAsync<dynamic>("() => ({a: document.querySelector('.section-tagline').innerText})");
            WriteLine((string)tag.a);

            await page.WaitForSelectorAsync(".section-title");
            var title = await page.EvaluateFunctionAsync<dynamic>("() => ({a: document.querySelector('.section-title').innerText})");
            WriteLine((string)title.a);

            await page.WaitForSelectorAsync(".section-subtitle");
            var subtitle = await page.EvaluateFunctionAsync<dynamic>("() => ({a: document.querySelector('.section-subtitle').innerText})");
            WriteLine((string)subtitle.a);

            await page.WaitForSelectorAsync(".item-product-image");
            var images = await page.EvaluateExpressionAsync<string[]>("Array.from(document.querySelectorAll('.item-product-image')).map(div=>div.style.backgroundImage)");
            int i = 0;
            WriteLine($"Please wait, image downloading...");

            WebClient client = new WebClient();
            {
                var createdDir = Directory.CreateDirectory($"images\\{(string)tag.a}");

                foreach (var item in images)
                {
                    var matchFull = Regex.Match(item, patternFullURL);
                    var matchGood = Regex.Match(matchFull.Groups[1].Value, patternGood);
                    client.DownloadFile(new Uri(matchGood.Groups[1].Value + ".jpg"), createdDir.FullName + $"\\image{++i}.jpg");
                }
            }

            var d = 0;
            await page.CloseAsync();
            WriteLine("done");
        }

        public string Buffer
        {
            get
            {
                return buffer;
            }
            set
            {
                buffer = value; NotifyPropertyChanged("Buffer");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
