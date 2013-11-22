using SubtitlerLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Subtitler
{
    public class FileEntry
    {
        public string Path { get; set; }
        public string DisplayName { get; set; }

        public FileEntry(string path)
        {
            Path = path;
            DisplayName = System.IO.Path.GetFileName(path);
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SubParser parser;
        private TimeSpan totalTime;
        private DispatcherTimer timer;
        private bool timerChangedValue;
        private Subtitle[] currentSubtitles;
        private FileEntry currentSubtitleFileEntry;
        private TimeSpan currentStartSubStartTime;
        private TimeSpan currentEndSubStartTime;

        public MainWindow()
        {
            InitializeComponent();

            parser = new SubParser();
            MovieMediaElement.LoadedBehavior = MediaState.Manual;
            TimeSlider.ValueChanged += TimeSlider_ValueChanged;
            //parser.Log += parser_Log;
        }

        void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!timerChangedValue)
            {
                MovieMediaElement.Position = TimeSpan.FromMilliseconds(TimeSlider.Value);
            }

            CurrentPositionLabel.Text = TimeSpan.FromMilliseconds(TimeSlider.Value).ToString("hh\\:mm\\:ss");
            timerChangedValue = false;
        }

        private void FolderTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = FolderTextBox.Text;
            if (Directory.Exists(text))
            {
                if (!text.EndsWith("\\"))
                {
                    text += "\\";
                }

                var files = Directory.GetFiles(text).Where(line => 
                    {
                        var lowerLine = line.ToLower();
                        return lowerLine.EndsWith(".srt") || lowerLine.EndsWith(".avi") || lowerLine.EndsWith(".mkv") || lowerLine.EndsWith(".mp4");
                    } ).Select(line => new FileEntry(line)) ;
                FolderContentsListBox.Items.Clear();
                foreach (var file in files)
                {
                    FolderContentsListBox.Items.Add(file);
                }
            }
            else
            {
                FolderContentsListBox.Items.Clear();
            }

        }

        private void FolderContentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var fileEntry = (FileEntry)FolderContentsListBox.SelectedItem;
            if (!fileEntry.Path.EndsWith(".srt"))
            {
                MovieMediaElement.Source = new Uri(fileEntry.Path);

                MovieMediaElement.Play();
            }
            else
            {
                currentSubtitleFileEntry = fileEntry;
                LoadSubtitle(fileEntry);
            }
        }

        private void LoadSubtitle(FileEntry fileEntry)
        {
            var parser = new SubParser();
            var subText = File.ReadAllText(fileEntry.Path);
            currentSubtitles = parser.Parse(subText);

            var firstSub = currentSubtitles.First();
            var endSub = currentSubtitles.Last();

            SubtitlesPreviewTextBox.Text = subText;
            StartSubIndexLabel.Text = string.Format("#{0}:", firstSub.Index);
            StartSubText.Text = firstSub.Text;
            EndSubIndexLabel.Text = string.Format("#{0}:", endSub.Index);
            EndSubText.Text = endSub.Text;
            SetStartSubStartTime(firstSub.StartTime);
            SetEndSubStartTime(endSub.StartTime);
        }

        private void SetStartSubStartTime(TimeSpan time)
        {
            StartSubStartTime.Text = time.ToString("hh\\:mm\\:ss\\,fff");
            currentStartSubStartTime = time;
        }

        private void SetEndSubStartTime(TimeSpan time)
        {
            EndSubStartTime.Text = time.ToString("hh\\:mm\\:ss\\,fff");
            currentEndSubStartTime = time;
        }

        private void MovieMediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            totalTime = MovieMediaElement.NaturalDuration.TimeSpan;

            TotalPositionLabel.Text = totalTime.ToString("hh\\:mm\\:ss");
            TimeSlider.Maximum = totalTime.TotalMilliseconds;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (totalTime.TotalMilliseconds > 0)
            {
                timerChangedValue = true;
                TimeSlider.Value = MovieMediaElement.Position.TotalMilliseconds;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            MovieMediaElement.Play();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            MovieMediaElement.Stop();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            MovieMediaElement.Pause();
        }

        private void StartSubSyncButton_Click(object sender, RoutedEventArgs e)
        {
            SetStartSubStartTime(MovieMediaElement.Position);
        }

        private void EndSubSyncButton_Click(object sender, RoutedEventArgs e)
        {
            SetEndSubStartTime(MovieMediaElement.Position);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var subSyncer = new SubSyncer(currentSubtitles);
            var syncSubs = subSyncer.Sync(currentStartSubStartTime, currentEndSubStartTime);
            var subParser = new SubParser();
            File.WriteAllText(currentSubtitleFileEntry.Path, subParser.GetSubtitlesString(syncSubs), Encoding.UTF8);
            MessageBox.Show("Subtitles saved successfully.", "Subtitler", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /*
        void parser_Log(object sender, SubParser.LogEventArgs e)
        {
            LogTextBox.Text += e.Message;
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = parser.Parse(File.ReadAllText(SubtitleFileTextBox.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
         */
    }
}
