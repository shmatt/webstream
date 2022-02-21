using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FFMediaToolkit;
using FFMediaToolkit.Encoding;
using FFMediaToolkit.Graphics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Bmp;

namespace WebStream
{
    class Program
    {
        static void Main(string[] args)
        {
            var ffmpegPath =  @".\ffmpeg\x86_64\";
            FFmpegLoader.FFmpegPath = ffmpegPath;

            new DriverManager().SetUpDriver(new ChromeConfig());

            var options = new ChromeOptions();
            options.AddArgument("headless");

            var driver = new ChromeDriver(options);

            var size = new System.Drawing.Size(1280, 720);
            driver.Manage().Window.Size = size;

            driver.Navigate().GoToUrl("https://api.matt.tew.io/live?public=true");

            var fps = 10;
            var outfile = $@"rtmp://a.rtmp.youtube.com/live2/z6r0-hude-xcmr-m5kc-a609";
            var arg =
                "-y -f rawvideo -s:v 1280x720 -r 30 -pixel_format bgr24 -i - -re -f lavfi -i anullsrc -c:v libx264 -g 40 -preset superfast -b:v 2500k -b:a 128k -ar 44100 -c:a aac -threads 2 -vf format=yuv420p -f flv {0}";

            var pinf = new System.Diagnostics.ProcessStartInfo($"{ffmpegPath}ffmpeg", string.Format(arg, outfile));
            pinf.UseShellExecute = false;
            pinf.RedirectStandardInput = true;
            
            Console.WriteLine("Starting ffmpeg...");
            var proc = System.Diagnostics.Process.Start(pinf);
            
            using (var stream = new BinaryWriter(proc.StandardInput.BaseStream))
            {
                var ended = false;
                
                var timer = new Timer(_ =>
                {
                    if (ended) return;
                
                    var screenshot = ((ITakesScreenshot) driver).GetScreenshot();
                    var image = Image.Load(screenshot.AsByteArray);
                    
                
                    var bgr24 = image.CloneAs<Bgr24>();
                
                    var ms = new MemoryStream();
                
                    var groups = bgr24.GetPixelMemoryGroup().ToArray();
                    foreach (var @group in groups)
                    {
                        ms.Write(MemoryMarshal.AsBytes(@group.Span).ToArray());
                    }
                
                    var imageData = ImageData.FromArray(ms.ToArray(), ImagePixelFormat.Bgr24, size);
                
                    stream.Write(ms.ToArray());
                    
                }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(200));

                Console.ReadLine();
                ended = true;
                proc.WaitForExit();
                
            }

            
        }
    }
}