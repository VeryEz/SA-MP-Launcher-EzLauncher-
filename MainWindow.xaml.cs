using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos.Streams;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Path = System.IO.Path;
using Image = System.Drawing.Image;
using ColorNew = System.Windows.Media.Color;
using Rectangle = System.Windows.Shapes.Rectangle;
using PointNew = System.Windows.Point;
using MessageBox = System.Windows.MessageBox;
using System.ComponentModel;
using System.Windows.Data;
using System.Diagnostics;
using System.Text;

/*
MENU_ID = 0 / MAIN MENU
*/

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public enum eMenus
    {
    
        MENU_MAIN = 0, //servers
        MENU_YOUTUBE = 1,
        MENU_RADIO = 2,
        MENU_SETTINGS
    }

    public partial class MainWindow : Window
    {

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(int hProcess,
          int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Example: Finding a window by class name
        //  IntPtr hwnd = WindowFinder.FindWindowByClassName("Notepad");
        //  if (hwnd != IntPtr.Zero)
        //   continue...
        public class WindowFinder
        {
            [DllImport("user32.dll")]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            public static IntPtr FindWindowByClassName(string className)
            {
                return FindWindow(className, null);
            }

            public static IntPtr FindWindowByTitle(string title)
            {
                return FindWindow(null, title);
            }
        }

        /// <summary>
        /// //////////////////////////////////////////////////////////////////////////////
        /// </summary>
        private YoutubeClient youtube = new YoutubeClient();

        private int iCountDownloadMP3 = 0, iCountDownloadMP4 = 0, typeDownload = 0;

        private const int SW_SHOWNOACTIVATE = 4;
        private const int HWND_BOTTOM = 1;
        private int MENU_ID = 0;

        private bool isDragging = false, bPulseBackgroundColor = true;

        private string? defaultURL = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        private string? authorChannelURL;
        private string? getVideoThumbnailURL;
        private string? getHighResThumbnailURL;
        //private string defaultSavePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private string? getSavedThumbnailPath;
        private string? getSelectedBitrate = "192";
        private string? getDownloadedFilePathForHistory;
        public string? defaultYTURL;

        private PointNew lastMousePosition;

        public SolidColorBrush btnHoverColor = new SolidColorBrush(ColorNew.FromArgb(180, 10, 100, 10));
       // private Timer pingTimer;
        private string? szCurrentServerIp;
        private string? szNickname = "myname_tmp";
        private string? szPathGame;
        private string? szGTAExe;
        // Constants for minimum and maximum name length
        public const int MinNameLength = 3;
        public const int MaxNameLength = 20;

    public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            TopBar.Background = new SolidColorBrush(ColorNew.FromArgb(230, 0, 0, 0));

            SolidColorBrush backgroundBrush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
            RowContextMenu.Background = backgroundBrush;

            SampRegistry sampRegistry = new SampRegistry();
            PlayerNameTextBox.Text = sampRegistry.getPlayerNameFromRegistry();
            szPathGame = sampRegistry.getGTAPathFromRegistry();
            string pathedit = szPathGame;
            szGTAExe = string.Format("{0}\\samp.dll", pathedit.Remove(pathedit.IndexOf("gta_sa.exe")));

            ScrapeData();
            // Create a SolidColorBrush with black color and 80% opacity
            // SolidColorBrush blackBrush = new SolidColorBrush(Color.FromArgb(204, 0, 0, 0));
            // Set the window's background to the SolidColorBrush
            if (bPulseBackgroundColor)
                StartPulsatingAnimation();
            else this.Background = new SolidColorBrush(ColorNew.FromArgb(220, 0, 0, 0));
        }

        #region MENU_CONTROLS
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                PointNew newLocationOffset = (PointNew)(e.GetPosition(this) - lastMousePosition);
                this.Left += newLocationOffset.X;
                this.Top += newLocationOffset.Y;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isDragging = false;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isDragging = true;
                lastMousePosition = e.GetPosition(this);
            }
        }
        #endregion

        #region MENU_NAVIGATION_SYSTEM

        private void ChangeMenu(int iMenuID = (int)eMenus.MENU_MAIN)
        {
            if (MENU_ID != iMenuID)
            {
                MENU_ID = iMenuID;
            }

            if (MENU_ID != 0)
                GridMenuMain.Visibility = Visibility.Hidden;
            else GridMenuMain.Visibility = Visibility.Visible;

            switch (MENU_ID)
            {
                case (int)eMenus.MENU_MAIN:
                    gMenuYoutube.Visibility = Visibility.Hidden;
                    gMenuRadio.Visibility = Visibility.Hidden;
                    break;

                case (int)eMenus.MENU_YOUTUBE:
                    gMenuYoutube.Visibility = Visibility.Visible;
                    break;

                case (int)eMenus.MENU_RADIO:
                    gMenuRadio.Visibility = Visibility.Visible;
                    break;

                default:
                    MENU_ID = 0;
                    //   GridMenuMain.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Window_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (MENU_ID != (int)eMenus.MENU_MAIN)
                {
                    MENU_ID = (int)eMenus.MENU_MAIN;
                    ChangeMenu(MENU_ID);
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // backspace <
            if (e.Key == Key.Back)
            {
                if (MENU_ID != (int)eMenus.MENU_MAIN)
                {
                    MENU_ID = (int)eMenus.MENU_MAIN;
                    ChangeMenu(MENU_ID);
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e) { }

        private void btnYoutube_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(defaultYTURL))
                defaultYTURL = "https://www.youtube.com/watch?v=jNQXAC9IVRw";


            textBoxYTURL.AppendText(defaultYTURL);
            ChangeMenu((int)eMenus.MENU_YOUTUBE);
        }

        private void btnRadio_Click(object sender, RoutedEventArgs e) { ChangeMenu((int)eMenus.MENU_RADIO); }
        #region UI_STYLE_COLORS_AND_STUFF
        private void btnYTDownload_MouseEnter(object sender, MouseEventArgs e) { btnYTDownload.MouseOverBackground = btnHoverColor; }

        #endregion

        #endregion
        private void StartPulsatingAnimation()
        {
            Background = new SolidColorBrush(ColorNew.FromArgb(210, 0, 0, 0));
            // Create a DoubleAnimation for opacity
            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = 0.30,  // Initial opacity
                To = Convert.ToDouble(245),    // Target opacity (100%)
                Duration = TimeSpan.FromSeconds(2), // Animation duration in seconds
                AutoReverse = true,  // Animation reverses back and forth
                RepeatBehavior = RepeatBehavior.Forever // Loop animation indefinitely
            };

            // Apply the animation to the Window's background
            backgroundRect.BeginAnimation(Rectangle.OpacityProperty, opacityAnimation);
        }

        private static readonly int PROCESS_WM_READ = 0x0010;
        private string getPlayerMoney()
        {
            Process gta = Process.GetProcessesByName("GTA_SA".ToLower()).ToList().FirstOrDefault();
            if (gta != null)
            {
                IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, gta.Id);
                int obj = 0;
                byte[] mem = new byte[11];
                ReadProcessMemory((int)processHandle, 0xB7CE50, mem, mem.Length, ref obj);
                //MessageBox.Show(proc.MainWindowTitle);
                return Encoding.Unicode.GetString(mem);
            }
            return null;
        }


        private bool CheckSettingsValue(string valueName)
        {
            if (string.IsNullOrEmpty(valueName))
                return false;

            using (RegistryKey reg = Registry.CurrentUser.OpenSubKey("Software\\EzLauncher\\Settings"))
            {
                if (reg.GetValue(valueName).Equals("true"))
                    return true;
            }

            return false;
        }

        private void SaveSettings(string settingsName, object settingsValue)
        {
            RegistryKey rgSet = Registry.CurrentUser.OpenSubKey("Software\\EzLauncher\\Settings", true);
            if (rgSet != null)
            {
                rgSet.SetValue(settingsName, Convert.ToString(settingsValue));
                rgSet.Close();
            }
        }

        public object getSettingsValue(string settingsValueName)
        {
            RegistryKey rgGetSet = Registry.CurrentUser.OpenSubKey("Software\\EzLauncher\\Settings", false);
            if (rgGetSet != null)
            {
                return rgGetSet.GetValue(settingsValueName);
            }
            else rgGetSet.Close();

            return null;
        }

        private bool IsSAMPDLLPresent()
        {
            SampRegistry sampRegistry = new SampRegistry();
            string gtapath = sampRegistry.getGTAPathFromRegistry();
            string filepath = string.Format("{0}\\samp.dll", gtapath.Remove(gtapath.IndexOf("gta_sa.exe")));

            if (File.Exists(filepath))
                return true;

            return false;
        }

        private string getSAMPInstalledVersion()
        {
            SampRegistry sampRegistry = new SampRegistry();
            string gtapath = sampRegistry.getGTAPathFromRegistry();

            string filepath = string.Format("{0}\\samp.dll", gtapath.Remove(gtapath.IndexOf("gta_sa.exe")));

            if (IsSAMPDLLPresent() == true)
                return FileVersionInfo.GetVersionInfo(filepath).ProductVersion.Replace(',', '.');
            else return "samp.dll missing!";
        }

        private int getNumberOfFolderFiles(string folder_path)
        {
            return Directory.GetFiles(folder_path).Length;
        }

        public void sampRegistryAddSubKey(string subkey, string value_name = null, object value_data = null)
        {
            RegistryKey registry = Registry.CurrentUser.CreateSubKey(string.Format("Software\\UIFLauncher\\{0}", subkey), RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None);
            if (registry != null)
            {
                if (value_name != null)
                    if (!registry.GetValueNames().Contains(value_name))
                        registry.SetValue(value_name, value_data ?? "");
            }
        }

        private bool IsWindowActive(string WinName = "GTA:SA:MP")
        {
            if (string.IsNullOrEmpty(WinName))
                return false;

            try
            {
                Process[] process = Process.GetProcesses();
                foreach (Process process1 in process)
                {
                    if (process1.MainWindowTitle == WinName)
                    {
                        return true;
                    }
                }
            }
            catch (Exception errprocess)
            {
                Console.WriteLine(errprocess.Message);
                //MessageBox.Show(errprocess.Message, "EzLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        private bool IsURLHTTP(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return false;
            Uri tmp;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out tmp))
                return false;
            return tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps;
        }

        private void OpenUri(string uri)
        {
            if (!string.IsNullOrEmpty(uri))
                Process.Start("explorer", uri);
            else Console.WriteLine("Invalid URL");
            //else MessageBox.Show(null, "Invalid URL", "EzLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool TypeCommand(string where, string getCmd)
        {
            if (string.IsNullOrEmpty(where) || string.IsNullOrEmpty(getCmd))
                return false;

            foreach (char c in where)
            {
                if (c.ToString() == "/")
                {
                    if (where == getCmd)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //IF you wonder how class below gets required data just check ScrapeData functions below
        public class ServerData
        {
            public string Ip { get; set; } //address
            public string Hn { get; set; } //hostanme 
            public string Gm { get; set; } //gamemode
            public int Pm { get; set; } //maxplayers slots
            public int Pc { get; set; } //online players count
            public string La { get; set; } //language
            public bool Pa { get; set; } //locked with password ?
            public string Vn { get; set; } //server game version
            public bool Omp { get; set; } //is server using open mp
            public bool Pr { get; set; } //open mp partner
        }


        //Here is where magic happens he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he he 
        private async void ScrapeData()
        {
            string apiUrl = "https://api.open.mp/servers";

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        List<ServerData> servers = JsonConvert.DeserializeObject<List<ServerData>>(jsonContent);

                        // Create a CollectionViewSource and bind it to the ServerDataGrid
                        CollectionViewSource serverDataViewSource = new CollectionViewSource();
                        serverDataViewSource.Source = servers; // Assuming 'servers' is your data collection
                        ServerDataGrid.ItemsSource = serverDataViewSource.View;

                        // Customize the title cells
                        // Calculate the total players online
                        int totalPlayersOnline = 0;
                        int totalSlotsAvailable = 0;

                        foreach (ServerData server in servers)
                        {
                            totalPlayersOnline += server.Pc;
                            totalSlotsAvailable += server.Pm;
                        }
                        // Update the label's content with the total count
                        //Should be put withing one label but who cares ehm
                        TotalPlayersLabel.Content = "Online: " + totalPlayersOnline;
                        TotalSlotsLabel.Content = "Slots: " + totalSlotsAvailable;
                        TotalServersLabel.Content = "Servers: " + servers.Count;

                        foreach (DataGridColumn column in ServerDataGrid.Columns)
                        {
                            string headerText = column.Header.ToString();
                            column.Header = new TextBlock { Text = headerText, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
                        }
                    }
                    else
                    {
                        Console.WriteLine($"HTTP Error: {response.StatusCode} - {response.ReasonPhrase}");
                        //ResultTextBlock.Visibility = Visibility.Visible;
                        //ResultTextBlock.Text = $"HTTP Error: {response.StatusCode} - {response.ReasonPhrase}";
                    }
                }
            }
            catch (Exception ex)
            {
                ResultTextBlock.Visibility = Visibility.Visible;
                ResultTextBlock.Text = "Fix your internet and restart the launcher:\n" + ex.Message;
                ResultTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }


        private async void ServerDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerDataGrid.SelectedItem is ServerData server)
            {
                // Get the data from the first cell of the selected row
                string ipAddress = server.Ip;

                // Update the label in the context menu
                IpLabel.Content = "IP: " + ipAddress;
                szCurrentServerIp = ipAddress;

                if (server.Pa == true)
                    isLocked.Content = "Locked: Yes";
                else isLocked.Content = "Locked: No";

              //  await Task.Run(() => GetPingForIpAddressAsync(server.Ip));

            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //ah the famous search box 
            string searchText = SearchTextBox.Text.ToLower(); // Convert the search text to lowercase for case-insensitive filtering

            ICollectionView serverDataView = CollectionViewSource.GetDefaultView(ServerDataGrid.ItemsSource);

            if (serverDataView != null)
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // Clear the filter if the search text is empty or contains only whitespace
                    serverDataView.Filter = null;
                }
                else
                {
                    // Apply a filter to show only rows where the 'Hn' property contains the search text
                    serverDataView.Filter = item =>
                    {
                        if (item is ServerData server)
                        {
                            
                            return server.Hn.ToLower().Contains(searchText);
                        }
                        return false;
                    };
                }
            }
        }

        private void cToggleSwitchLocked_Checked(object sender, RoutedEventArgs e) { FilterRemoveLockedServers(true); }
        private void cToggleSwitchLocked_Unchecked(object sender, RoutedEventArgs e) { FilterRemoveLockedServers(false); }
        private void FilterRemoveLockedServers(bool isCheckedLocked = false)
        {
            ICollectionView serverDataView = CollectionViewSource.GetDefaultView(ServerDataGrid.ItemsSource);

            if (serverDataView != null)
            {
                if (isCheckedLocked)
                {
                    // Apply a filter to hide rows where the 'Password' property is true
                    serverDataView.Filter = item =>
                    {
                        if (item is ServerData server)
                        {
                            return server.Pa != true;
                        }
                        return false;
                    };
                }
                else
                {
                    serverDataView.Filter = null;
                }
            }
        }

        private void cToggleSwitch_Checked(object sender, RoutedEventArgs e) { FilterRemoveEmptyServers(true); }
        private void cToggleSwitch_Unchecked(object sender, RoutedEventArgs e) { FilterRemoveEmptyServers(false); }

        private void FilterRemoveEmptyServers(bool isCheckedEmpty = false)
        {
            ICollectionView serverDataView = CollectionViewSource.GetDefaultView(ServerDataGrid.ItemsSource);

            if (serverDataView != null)
            {
                if (isCheckedEmpty)
                {
                    // Apply a filter to hide rows where the 'Pc' property is 0
                    serverDataView.Filter = item =>
                    {
                        if (item is ServerData server)
                        {
                            return server.Pc != 0;
                        }
                        return false;
                    };
                }
                else
                {
                    // Clear the filter to show all rows
                    serverDataView.Filter = null;
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected server address from the IpLabel
            string selectedServerAddress = szCurrentServerIp;

            if (IsSAMPDLLPresent() == false) { OpenUri("https://files.samp-sc.com/downloader.php?filename=sa-mp-0.3.7-R5-1-install.exe"); return; }

            if (string.IsNullOrWhiteSpace(selectedServerAddress))
            {
                // Display an error message or handle the case when the address is invalid
                Console.WriteLine("Invalid SA:MP server address.");
                return;
            }

            // Separate the address and port
            string[] addressAndPort = selectedServerAddress.Split(':');

            if (addressAndPort.Length != 2 || !int.TryParse(addressAndPort[1], out int serverPort))
            {
                // Handle the case when the address or port is not in the expected format
                Console.WriteLine("Invalid SA:MP server address format.");
                return;
            }

            string serverAddress = addressAndPort[0];
            string playerName = PlayerNameTextBox.Text;

            if (playerName.Length >= 3 && playerName.Length <= 20)
            {
                // Launch the SA:MP client with the separated server address, port, and player name
                SampRegistry registry = new SampRegistry();
                var getGtaPath = registry.getGTAPathFromRegistry();
                Process.Start(getGtaPath.Remove(getGtaPath.IndexOf("gta_sa.exe")) + "samp.exe", $"{serverAddress}:{serverPort} -n{playerName}");
                this.WindowState = WindowState.Minimized; // Minimize the application window
            }
            else
            {
                // Handle the case when the player name is not within the valid length range
                Console.WriteLine("Name can't be less than 3 or more than 20 characters.");
            }
        }







        /// <summary>
        /// //***///////////////////// DONT MIND THE CODE BELOW ITS FOR SCIENCE PURPOSES
        /// 









        public bool CheckIfLinksExistInRegistry()
        {
            // Registry key path
            string keyPath = "Software\\Dafaq\\DownloadHistory";

            try
            {
                // Open the registry key for reading
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    // Check if the key exists
                    if (key != null)
                    {
                        // Check if there are any value names (link names)
                        if (key.GetValueNames().Length > 0)
                        {
                            return true; // Links exist in the registry
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while reading from the registry: " + ex.Message);
            }

            return false; // No links found in the registry
        }



        public ImageSource ConvertDrawingImageToImageSource(System.Drawing.Image drawingImage)
        {
            if (drawingImage == null)
                return null;

            using (MemoryStream stream = new MemoryStream())
            {
                // Save the System.Drawing.Image to a MemoryStream in a format that WPF can understand
                drawingImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                // Create a new BitmapImage
                BitmapImage bitmapImage = new BitmapImage();

                // Set the MemoryStream as the source for the BitmapImage
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }


        private async void DisplayORSaveVideoThumbnail(string url, int mode = 0)
        {
            try
            {
                WebClient client = new WebClient();
                byte[] thumbnailData = await client.DownloadDataTaskAsync(url);
                using (MemoryStream ms = new MemoryStream(thumbnailData))
                {
                    Image thumbnail = Image.FromStream(ms);

                    if (mode == 0)
                    {
                        ImageSource imageSource = ConvertDrawingImageToImageSource(thumbnail);
                        panelImageYoutubeThumb.Source = imageSource;
                        panelImageYoutubeThumb.Stretch = Stretch.UniformToFill;
                    }
                    else if (mode == 1)
                    {
                        // Display a save file dialog to allow the user to choose a filename and file format
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.Filter = "JPEG Image|*.jpg|Bitmap Image|*.bmp|PNG Image|*.png";
                        saveFileDialog.Title = "Save Thumbnail";
                        saveFileDialog.FileName = $"{"thumbnail"}";
                        bool? result = saveFileDialog.ShowDialog();

                        if (result == true) { return; }

                        if (saveFileDialog.FileName != "")
                        {
                            // Get the image format based on the file extension
                            ImageFormat format;
                            switch (Path.GetExtension(saveFileDialog.FileName).ToLower())
                            {
                                case ".bmp":
                                    format = ImageFormat.Bmp;
                                    break;
                                case ".jpg":
                                case ".jpeg":
                                    format = ImageFormat.Jpeg;
                                    break;
                                case ".png":
                                    format = ImageFormat.Png;
                                    break;
                                default:
                                    MessageBox.Show("Invalid file format selected.");
                                    return;
                            }

                            // Save the panel picture to the selected file with the chosen format
                            thumbnail.Save(saveFileDialog.FileName, format);
                            getSavedThumbnailPath = saveFileDialog.FileName;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                //MessageBox.Show("Error loading video thumbnail: " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                //MessageBox.Show("Error loading video thumbnail: " + ex.Message);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Error loading video thumbnail: " + ex.Message);
            }
        }

        private async void textBoxYTURL_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxYTURL.Text)) return;

            try
            {
                var videoS2 = await youtube.Videos.GetAsync(textBoxYTURL.Text);
                if (videoS2 != null)
                {
                    var streamInfoSet2a = await youtube.Videos.Streams.GetManifestAsync(textBoxYTURL.Text);
                    var audioStreamInfo2a = streamInfoSet2a.GetAudioOnlyStreams().GetWithHighestBitrate();
                    var videoStreamInfo = streamInfoSet2a.GetMuxedStreams().GetWithHighestBitrate();

                    getVideoThumbnailURL = videoS2.Thumbnails.First().Url;

                    var videoId2 = getVideoThumbnailURL.Remove(0, "https://i.ytimg.com/vi/".Length);
                    videoId2 = videoId2.Substring(0, videoId2.IndexOf('/'));
                    var maxResThumbnailUrl2 = $"https://i.ytimg.com/vi/{videoId2}/hqdefault.jpg";
                    getHighResThumbnailURL = $"https://i.ytimg.com/vi/{videoId2}/maxresdefault.jpg";
                    DisplayORSaveVideoThumbnail(maxResThumbnailUrl2);

                    // labelVideoDuration.Text = videoS2.Duration.Value.ToString();
                    //labelVideoTitle.Text = videoS2.Title;
                    // labelVideoAuthor.Text = $"Uploader: {videoS2.Author}";
                    authorChannelURL = videoS2.Author.ChannelUrl;
                    lblVideoSize.Content = $"Video: {Convert.ToInt16(videoStreamInfo.Size.MegaBytes)}mb";

                    int sizeOFAudio = Convert.ToInt16(audioStreamInfo2a.Size.MegaBytes);
                    string sizeLBLAudio = null;
                    switch (sizeOFAudio)
                    {
                        case var _ when sizeOFAudio < 1:
                            sizeLBLAudio = $"Audio: {Convert.ToInt16(audioStreamInfo2a.Size.KiloBytes)}kb";
                            break;
                        default:
                            sizeLBLAudio = $"Audio: {sizeOFAudio}mb";
                            break;
                    }
                    lblAudioSize.Content = sizeLBLAudio;
                }
                else
                {
                    //MessageBox.Show("Video not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (VideoUnavailableException)
            {
                // MessageBox.Show("Video or Audio is unavailable", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
