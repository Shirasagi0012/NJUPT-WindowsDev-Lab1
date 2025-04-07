using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagLibUWP;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MusicPlayer
{
    public sealed partial class MainPage : Page
    {
        private MediaPlayer mediaPlayer;
        private ObservableCollection<MusicItem> playlist = new ObservableCollection<MusicItem>();
        private DispatcherTimer timer;
        private bool isUserSeeking = false;
        private bool isRepeating = false;

        public MainPage ( )
        {
            this.InitializeComponent();
            InitializePlayer();
        }

        private void InitializePlayer ( )
        {
            // 使用MediaPlayer进行播放
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

            // 初始化播放列表
            PlaylistsListView.ItemsSource = playlist;

            // 使用DispatcherTimer以实现定时更新进度条
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (sender, e) =>
            {
                if ( !isUserSeeking && mediaPlayer.PlaybackSession != null )
                {
                    // 更新进度条
                    var position = mediaPlayer.PlaybackSession.Position;
                    var duration = mediaPlayer.PlaybackSession.NaturalDuration;

                    if ( duration.TotalSeconds > 0 )
                    {
                        ProgressSlider.Value = position.TotalSeconds / duration.TotalSeconds * 100;
                        CurrentTimeTextBlock.Text = FormatTimeSpan(position);
                        TotalTimeTextBlock.Text = FormatTimeSpan(duration);
                    }
                }
            };
        }

        private string FormatTimeSpan (TimeSpan timeSpan)
        {
            return $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
        }

        private void ProgressSlider_PointerPressed (object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            isUserSeeking = true;
        }

        private void ProgressSlider_PointerCaptureLost (object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            isUserSeeking = false;
            if ( mediaPlayer.PlaybackSession != null )
            {
                var newPosition = TimeSpan.FromSeconds(ProgressSlider.Value / 100 *
                    mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds);
                mediaPlayer.PlaybackSession.Position = newPosition;
            }
        }

        private void ProgressSlider_ValueChanged (object sender, RangeBaseValueChangedEventArgs e)
        {
            if ( isUserSeeking && mediaPlayer.PlaybackSession != null )
            {
                var position = TimeSpan.FromSeconds(e.NewValue / 100 *
                    mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds);
                CurrentTimeTextBlock.Text = FormatTimeSpan(position);
            }
        }

        private async void AddButton_Click (object sender, RoutedEventArgs e)
        {
var picker = new FileOpenPicker();
picker.ViewMode = PickerViewMode.List;
picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
picker.FileTypeFilter.Add(".mp3");
picker.FileTypeFilter.Add(".wav");
picker.FileTypeFilter.Add(".flac");
picker.FileTypeFilter.Add(".m4a");
picker.FileTypeFilter.Add(".wma");

var files = await picker.PickMultipleFilesAsync();
if ( files != null && files.Count > 0 )
{
    foreach ( var file in files )
    {
        // 使用TagLib读取音乐文件信息
        var fileInfo = await Task.Run(() => TagManager.ReadFile(file));
        var tag = fileInfo.Tag;

        var title = tag.Title;
        if ( string.IsNullOrEmpty(title) )
            title = Path.GetFileNameWithoutExtension(file.Name);

        var artist = tag.Artist;
        if ( string.IsNullOrEmpty(artist) )
            artist = "未知艺术家";

        var album = tag.Album;
        if ( string.IsNullOrEmpty(album) )
            album = "未知专辑";


        var song = new MusicItem
        {
            Title = title,
            Artist = artist,
            Album = album,
            File = file
        };
        playlist.Add(song);
    }

    // 自动开始播放
    if ( mediaPlayer.Source == null )
    {
        PlaylistsListView.SelectedIndex = 0;
        await PlaySelectedMusic();
    }
}
        }

        private async void PlaylistsListView_SelectionChanged (object sender, SelectionChangedEventArgs e)
        {
            if ( PlaylistsListView.SelectedItem != null )
            {
                await PlaySelectedMusic();
            }
        }

private async Task PlaySelectedMusic ( )
{
    if ( PlaylistsListView.SelectedItem is MusicItem selectedMusic )
    {
        mediaPlayer.Pause();

        var stream = await selectedMusic.File.OpenAsync(FileAccessMode.Read);
        mediaPlayer.Source = MediaSource.CreateFromStream(stream, selectedMusic.File.ContentType);
        mediaPlayer.Play();

        UpdatePlayPauseButton(true);
        timer.Start();
    }
}

        private void PlayPauseButton_Click (object sender, RoutedEventArgs e)
        {
            if ( mediaPlayer.Source == null && playlist.Count > 0 )
            {
                PlaylistsListView.SelectedIndex = 0;
            }
            else if ( mediaPlayer.Source != null )
            {
                if ( mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing )
                {
                    mediaPlayer.Pause();
                    timer.Stop();
                    UpdatePlayPauseButton(false);
                }
                else
                {
                    mediaPlayer.Play();
                    timer.Start();
                    UpdatePlayPauseButton(true);
                }
            }
        }

private void UpdatePlayPauseButton (bool isPlaying)
{
    // 更新播放/暂停按钮图标
    PlayPauseIcon.Glyph = isPlaying ? "\uE769" : "\uE768";
}

        private void NextButton_Click (object sender, RoutedEventArgs e)
        {
            if ( playlist.Count > 0 )
            {
                int nextIndex = PlaylistsListView.SelectedIndex + 1;
                if ( nextIndex >= playlist.Count )
                    nextIndex = 0;

                PlaylistsListView.SelectedIndex = nextIndex;
            }
        }

        private void PreviousButton_Click (object sender, RoutedEventArgs e)
        {
            if ( playlist.Count > 0 )
            {
                int prevIndex = PlaylistsListView.SelectedIndex - 1;
                if ( prevIndex < 0 )
                    prevIndex = playlist.Count - 1;

                PlaylistsListView.SelectedIndex = prevIndex;
            }
        }

private void MediaPlayer_MediaEnded (MediaPlayer sender, object args)
{
    if ( isRepeating )
    {
        mediaPlayer.Play();
    }
    else
    {
        // 自动播放下一首
        _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,( ) =>
        {
            NextButton_Click(null, null);
        });
    }
}

        private void MediaPlayer_MediaFailed (MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ( ) =>
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "播放错误",
                    Content = $"无法播放当前音乐: {args.ErrorMessage}",
                    CloseButtonText = "确定"
                };
                _ = dialog.ShowAsync();
            });
        }

        private void Volume_ValueChanged (object sender, RangeBaseValueChangedEventArgs e)
        {
            if ( mediaPlayer is null )
                return;
            mediaPlayer.Volume = e.NewValue / 100;
        }
    }

    public class MusicItem
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public StorageFile File { get; set; }

        public static string CombineArtistAndAlbum (string artist, string album)
        {
            if ( album?.Length == 0 )
                return artist;
            if ( artist?.Length == 0 )
                return album;
            return $"{artist} - {album}";
        }
    }
}
