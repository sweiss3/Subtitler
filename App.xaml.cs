using SubtitlerLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Subtitler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                base.OnStartup(e);
            }
            else
            {
                if (e.Args.Length != 3)
                {
                    Console.WriteLine("You must provide no arguments or 3 arguments.");
                    Console.WriteLine("Usage: ");
                    Console.WriteLine("  {0} startTime endTime subtitleFile.srt", Path.GetFileName( Assembly.GetExecutingAssembly().Location));
                    Console.WriteLine("");
                    Console.WriteLine("  startTime and endTime should be in the format: hh:ss:mm,fff");
                    Environment.Exit(1);
                }

                try
                {
                    string timeFormat = "hh\\:mm\\:ss\\,fff";
                    var startTime = TimeSpan.ParseExact(e.Args[0], timeFormat, CultureInfo.InvariantCulture);
                    //Console.WriteLine("Start time: " + startTime.ToString(timeFormat));
                    var endTime = TimeSpan.ParseExact(e.Args[1], timeFormat, CultureInfo.InvariantCulture);
                    //Console.WriteLine("End time: " + endTime.ToString(timeFormat));
                    var subtitleFile = e.Args[2];

                    var subParser = new SubParser();
                    string subtitleString = File.ReadAllText(subtitleFile);
                    var subs = subParser.Parse(subtitleString);
                    var subSyncer = new SubSyncer(subs);
                    var syncSubs = subSyncer.Sync(startTime, endTime);
                    File.WriteAllText(subtitleFile, subParser.GetSubtitlesString(syncSubs), Encoding.UTF8);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Bad parameters passed: {0}", ex.ToString());
                    Environment.Exit(1);
                }

                

            }
        }
    }
}
