using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Unosquare.FFME.Common;

/// <summary>
/// Power by https://github.com/unosquare/ffmediaelement
/// Clone by https://github.com/ShelbyBoss/TakeVideoScreenshot
/// </summary>
namespace FFMpegMediaElement {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private bool isUpadatingSlider;
        private DateTime videoCreated;
        private DispatcherTimer timer;
        private BitmapDataBuffer bmpBuffer;

        public MainWindow() {
            InitializeComponent();

            //NOTE:             
            //If ffmpeg is 64bit, program is must be 64 bit.
            //Only support ffmpeg shared library build.
            Unosquare.FFME.Library.FFmpegDirectory = @"E:\Installers\ffmpeg\ffmpeg-20190826-0821bc4-win64-shared\bin";

            me.BufferingStarted += (s, e) => Debug.WriteLine("Buffer started.");
            me.BufferingEnded += (s, e) => Debug.WriteLine("Buffer ended.");
            me.MediaReady += (s, e) => Debug.WriteLine("Media ready.");
            me.MediaOpened += (s, e) => Debug.WriteLine("Media opened.");
            me.MediaEnded += (s, e) => Debug.WriteLine("Media ended.");
            me.MediaClosed += (s, e) => Debug.WriteLine("Media closed.");
            me.MediaFailed += (s, e) => Debug.WriteLine("Media failed.");
            Unosquare.FFME.MediaElement.FFmpegMessageLogged += (s, e) => Debug.WriteLine(e.Message);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Me_MediaOpened(object sender, MediaOpenedEventArgs e) {
            sld.Maximum = me.NaturalDuration.HasValue ? me.NaturalDuration.Value.TotalMilliseconds : 0;
        }

        private void Me_RenderingVideo(object sender, RenderingVideoEventArgs e) {
            bmpBuffer = e.Bitmap;
        }

        private void Timer_Tick(object sender, EventArgs e) {
            if (isUpadatingSlider) return;

            isUpadatingSlider = true;
            sld.Value = me.Position.TotalMilliseconds;
            isUpadatingSlider = false;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                FileInfo file = new FileInfo(files[0]);

                videoCreated = file.LastWriteTime;
                me.Source = new Uri(file.FullName);
                me.Play();
            }
        }

        private bool IsPicture(FileInfo file) {
            string e = file.Extension.ToLower();
            return e == ".jpeg" || e == ".jpg" || e == ".png" || e == ".bmp";
        }

        private void BtnFile_Click(object sender, RoutedEventArgs e) {
            using (var dialog = new System.Windows.Forms.OpenFileDialog {
                Title = "Select video file",
                Multiselect = true,
            }) {
                var dialogResult = dialog.ShowDialog();
                if (dialogResult == System.Windows.Forms.DialogResult.OK) {
                    var files = dialog.FileNames;
                    var file = new FileInfo(files[0]);
                    me.Source = new Uri(file.FullName);
                    //me.Source = new Uri(@"rtsp://user:OfficeCCTV@10.1.10.10:554/cam/realmonitor?channel=2&subtype=1"); // file.FullName);
                    me.Play();
                }
            }

        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e) {
            PlayPause();
        }

        private void BtnSlower_Click(object sender, RoutedEventArgs e) {
            SlowerDown();
        }

        private void BtnFaster_Click(object sender, RoutedEventArgs e) {
            SpeedUp();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) {
            PreviousFrame();
        }

        private void BtnFor_Click(object sender, RoutedEventArgs e) {
            NextFrame();
        }

        private void BtnTake_Click(object sender, RoutedEventArgs e) {
            TakeScreenshot();
        }

        private void Sld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (isUpadatingSlider) return;

            isUpadatingSlider = true;
            me.Position = TimeSpan.FromMilliseconds(sld.Value);
            isUpadatingSlider = false;
        }

        private void Me_KeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Space:
                    PlayPause();
                    break;

                case Key.OemMinus:
                    SlowerDown();
                    break;

                case Key.OemPlus:
                    SpeedUp();
                    break;

                case Key.Q:
                    PreviousFrame();
                    break;

                case Key.E:
                    NextFrame();
                    break;

                case Key.W:
                case Key.S:
                    TakeScreenshot();
                    break;

                case Key.A:
                    me.Position -= TimeSpan.FromSeconds(5);
                    break;

                case Key.D:
                    me.Position += TimeSpan.FromSeconds(5);
                    break;
            }
        }

        private void PlayPause() {
            switch (me.MediaState) {
                case MediaPlaybackState.Play:
                    me.Pause();
                    break;

                default:
                    me.Play();
                    break;
            }

            //switch (me.LoadedBehavior)
            //{
            //    case MediaState.Play:
            //        me.LoadedBehavior = MediaState.Pause;
            //        break;

            //    default:
            //        me.LoadedBehavior = MediaState.Play;
            //        break;
            //}
        }

        private void SlowerDown() {
            me.SpeedRatio /= 1.1;
        }

        private void SpeedUp() {
            me.SpeedRatio *= 1.1;
        }

        private void PreviousFrame() {
            me.Position -= me.PositionStep;
        }

        private void NextFrame() {
            me.Position += me.PositionStep;
        }

        private void TakeScreenshot() {
            string path;
            string filePrefix = tbxFilePrefix.Text.Trim();

            if (filePrefix.Length == 0) {
                MessageBox.Show("Check targetpath!");
                return;
            }

            var captureTime = videoCreated + me.Position;
            do {
                string fileName = string.Format(" {0}.png", Convert(captureTime));
                path = filePrefix + fileName;

                captureTime = captureTime.AddMilliseconds(1);

            } while (File.Exists(path));

            rect.Fill = Brushes.Black;
            Task.Factory.StartNew(() => {
                bmpBuffer.CreateDrawingBitmap().Save(path);
                Dispatcher.Invoke(() => rect.Fill = Brushes.Transparent);
            });
        }

        private static string Convert(DateTime t) {
            string date = string.Format("{0,2}-{1,2}-{2,2}", t.Year, t.Month, t.Day).Replace(" ", "0");
            string time = string.Format("{0,2}-{1,2}-{2,2}-{3,3}", t.Hour, t.Minute, t.Second, t.Millisecond).Replace(" ", "0");

            return date + " " + time;
        }

        protected override void OnClosing(CancelEventArgs e) {
            timer.Stop();

            base.OnClosing(e);
        }

        private void Rect_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
            ((FrameworkElement)sender).Focus();
        }
    }
}
