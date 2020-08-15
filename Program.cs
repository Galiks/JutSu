using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace JutSu
{
    class Program
    {
        private const string adBlockExtension = "cfhdojbkjhnklbpkdaibdccddilifddb-3.9.2-Crx4Chrome.com.crx";

        public static ChromeDriver Driver { get; set; }
        public static string CurrentUrl { get; set; }

        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
                                               // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        static void Main(string[] args)
        {
            //AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);
            try
            {
                StartWatching();
            }
            finally
            {
                Exit();
            }

            
        }

        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                Exit();
            }
            return false;
        }

        private static void Exit()
        {
            RewriteAnime(CurrentUrl);
            Console.WriteLine("Program was closed");
            Environment.Exit(0);
        }

        private static IEnumerable<Anime> GetAnimes()
        {
            try
            {
                return JsonConvert.DeserializeObject<List<Anime>>(File.ReadAllText("anime.json"));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void WriteToJson(Anime anime)
        {
            var json = GetAnimes()?.ToList();
            if (json == null)
            {
                json = new List<Anime>();
            }
            json.Add(anime);
            File.WriteAllText("anime.json", JsonConvert.SerializeObject(json, Formatting.Indented));
        }

        private static void WriteToJson(IEnumerable<Anime> animes)
        {
            var json = GetAnimes().ToList();
            json.AddRange(animes);
            File.WriteAllText("anime.json", JsonConvert.SerializeObject(animes, Formatting.Indented));
        }

        private static void RewriteJson(Anime anime)
        {
            var animes = GetAnimes();
            var currentAnime = animes?.Where(a => a.Name.Equals(anime.Name)).FirstOrDefault();
            if (currentAnime != null)
            {
                currentAnime.Url = anime.Url;
                currentAnime.Episode = anime.Episode;
                File.WriteAllText("anime.json", JsonConvert.SerializeObject(animes, Formatting.Indented)); 
            }
            else
            {
                WriteToJson(anime);
            }
        }

        private static void StartWatching()
        {
            try
            {
                bool isFullscreen = false;
                ChromeOptions chromeOptions = new ChromeOptions();
                chromeOptions.AddExtension(adBlockExtension);
                Driver = new ChromeDriver(chromeOptions);
                var currentAnime = GetAnimes().Where(a => a.Name.Equals("onepiece")).FirstOrDefault();
                Driver.Navigate().GoToUrl(currentAnime.Url);
                CurrentUrl = Driver.Url;

                while (true)
                {
                    isFullscreen = PlayButtom(isFullscreen);
                    SkipIntroButton();
                    isFullscreen = NextVideoButton(isFullscreen);
                    if (!IsExist())
                    {
                        Driver.Quit();
                    }

                }
            }
            catch (WebDriverException)
            {
                Driver.Quit();
            }            
        }

        private static bool IsExist()
        {
            try
            {
                var title = Driver.Title;
                return true;
            }
            catch (WebDriverException)
            {
                return false;
            }
        }

        private static void RewriteAnime(string url)
        {
            string[] splitUrl = url.Split(new []{'/'}, StringSplitOptions.RemoveEmptyEntries);
            string episodeLikeString = (splitUrl[3].Split(new[] { '-', '.' }, StringSplitOptions.RemoveEmptyEntries))[1];
            if (int.TryParse(episodeLikeString, out int episode))
            {
                Anime anime = new Anime()
                {
                    Name = splitUrl[2],
                    Url = url,
                    Episode = episode
                };

                RewriteJson(anime);
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        private static bool NextVideoButton(bool isFullscreen)
        {
            try
            {
                var nextVideoButtom = Driver.FindElement(By.CssSelector("#my-player > div:nth-child(8)"));
                //ClassName("vjs-overlay vjs-overlay-bottom-right vjs-overlay-skip-intro vjs-overlay-background"));
                if (nextVideoButtom.Displayed)
                {
                    nextVideoButtom.Click();
                    isFullscreen = !isFullscreen;
                }
                CurrentUrl = Driver.Url;
            }
            catch (Exception)
            {
                return isFullscreen;
                //Console.WriteLine($"Next Video Buttom Error: {e.Message}");
            }

            return isFullscreen;
        }

        private static void SkipIntroButton()
        {
            try
            {
                var skipIntroButtom = Driver.FindElement(By.CssSelector("#my-player > div.vjs-overlay.vjs-overlay-bottom-left.vjs-overlay-skip-intro.vjs-overlay-background"));
                //ClassName("vjs-overlay vjs-overlay-bottom-left vjs-overlay-skip-intro vjs-overlay-background"));
                if (skipIntroButtom.Displayed)
                {
                    skipIntroButtom.Click();
                }
            }
            catch (Exception)
            {
                return;
                //Console.WriteLine($"Skip Intro Buttom Error: {e.Message}");
            }
        }

        private static bool PlayButtom(bool isFullscreen)
        {
            try
            {
                var playButton = Driver.FindElement(By.ClassName("vjs-big-play-button"));
                if (playButton.Displayed)
                {
                    playButton.Click();
                    isFullscreen = SetFullscrenn(isFullscreen);
                }
            }
            catch (Exception)
            {
                try
                {
                    var playButton = Driver.FindElement(By.ClassName("vjs-play-control vjs-control vjs-button vjs-paused"));
                    if (playButton.Displayed)
                    {
                        playButton.Click();
                    }
                }
                catch (Exception)
                {
                    return isFullscreen;
                }
            }

            return isFullscreen;
        }

        private static bool SetFullscrenn(bool isFullscreen)
        {
            try
            {
                if (!isFullscreen)
                {
                    var fullscrenButton = Driver.FindElement(By.CssSelector("#my-player > div.vjs-control-bar > button.vjs-fullscreen-control.vjs-control.vjs-button"));
                    //FindElementByClassName("vjs-fullscreen-control vjs-control vjs-button");

                    fullscrenButton.Click();
                    isFullscreen = !isFullscreen;

                }
            }
            catch (Exception)
            {
                return isFullscreen;
            }

            return isFullscreen;
        }


    }
}

