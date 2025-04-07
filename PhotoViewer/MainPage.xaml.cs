using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace PhotoViewer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public List<ImageInfo> Pictures { get; set; } = new List<ImageInfo>();

        public MainPage ( )
        {
            this.InitializeComponent();
        }

        private void Page_Loaded (object sender, RoutedEventArgs e)
        {
            Pictures.Add(new ImageInfo("ms-appx:///Images/116722157.png"));
            Pictures.Add(new ImageInfo("ms-appx:///Images/48382267.png"));
            Pictures.Add(new ImageInfo("ms-appx:///Images/74627599.png"));
            ImageList.ItemsSource = Pictures;
        }

        private void ImageList_SelectionChanged (object sender, SelectionChangedEventArgs e)
        {
            ImageSource image;
            var item = ImageList.SelectedItem;
            if ( item is ImageInfo info )
            {
                image = new BitmapImage(new Uri(info.Path));
            }
            else image = new BitmapImage();
            ImageView.Source = image;
        }
    }

    public class ImageInfo
    {
        public ImageInfo (string path )
        {
            Path = path;
            Name = System.IO.Path.GetFileName(Path);
        }
        public string Name { get; set; }
        public string Path { get; set; }
    }
}
