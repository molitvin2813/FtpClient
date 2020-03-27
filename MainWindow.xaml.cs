using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;


namespace FtpClient
{
    public partial class MainWindow : Window
    {
        private Client client;
        string prevAdress = "ftp://";

        public MainWindow()
        {
            InitializeComponent();

        }

        private void btn_connect_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем объект подключения по FTP
                Client client = new Client(txt_adres.Text, txt_login.Text, txt_password.Password);

                // Регулярное выражение, которое ищет информацию о папках и файлах 
                // в строке ответа от сервера
                Regex regex = new Regex(@"^([d-])([rwxt-]{3}){3}\s+\d{1,}\s+.*?(\d{1,})\s+(\w+\s+\d{1,2}\s+(?:\d{4})?)(\d{1,2}:\d{2})?\s+(.+?)\s?$",
                    RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

                // Получаем список корневых файлов и папок
                // Используется LINQ to Objects и регулярные выражения
                List<FileDirectoryInfo> list = client.ListDirectoryDetails()
                                                     .Select(s =>
                                                     {
                                                         Match match = regex.Match(s);
                                                         if (match.Length > 5)
                                                         {
                                                             // Устанавливаем тип, чтобы отличить файл от папки (используется также для установки рисунка)
                                                             string type = match.Groups[1].Value == "d" ? "DIR.png" : "FILE.png";

                                                             // Размер задаем только для файлов, т.к. для папок возвращается
                                                             // размер ярлыка 4кб, а не самой папки
                                                             string size = "";
                                                             if (type == "FILE.png")
                                                                 size = (Int32.Parse(match.Groups[3].Value.Trim()) / 1024).ToString() + " кБ";

                                                             return new FileDirectoryInfo(size, type, match.Groups[6].Value, match.Groups[4].Value, txt_adres.Text);
                                                         }
                                                         else return new FileDirectoryInfo();
                                                     }).ToList();

                // Добавить поле, которое будет возвращать пользователя на директорию выше
                list.Add(new FileDirectoryInfo("", "DEFAULT.png", "...", "", txt_adres.Text));
                list.Reverse();

                // Отобразить список в ListView
                lbx_files.DataContext = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString() + ": \n" + ex.Message);
            }
        }

        private void folder_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                FileDirectoryInfo fdi = (FileDirectoryInfo)(sender as StackPanel).DataContext;
                if (fdi.Type == "DIR.png")
                {
                    prevAdress = fdi.adress;
                    txt_adres.Text = fdi.adress + fdi.Name + "/";
                    btn_connect_Click_1(null, null);
                }
            }
        }



        private void MakeDirectory_Click_1(object sender, RoutedEventArgs e)
        {
            //FileDirectoryInfo fdi = (FileDirectoryInfo)(sender as StackPanel).DataContext;
            client = new Client(txt_adres.Text, txt_login.Text, txt_password.Password);
            client.MakeDirectory(txt_dir.Text);
            btn_connect_Click_1(null, null);
            var request = client.createRequest(client.combine(client.uri, txt_dir.Text), WebRequestMethods.Ftp.MakeDirectory);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            client = new Client(txt_adres.Text, txt_login.Text, txt_password.Password);
            client.RemoveDirectory(txt_dir.Text);
            btn_connect_Click_1(null, null);
            var request = client.createRequest(client.combine(client.uri, txt_dir.Text), WebRequestMethods.Ftp.RemoveDirectory);

            // return getStatusDescription(request);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            client = new Client(txt_adres.Text, txt_login.Text, txt_password.Password);
            client.Rename(txt_dir.Text, txt_dir2.Text);
            btn_connect_Click_1(null, null);
            var request = client.createRequest(client.combine(client.uri, txt_dir.Text), WebRequestMethods.Ftp.Rename);
            //request.RenameTo = txt_dir2.Text;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            client = new Client(txt_adres.Text, txt_login.Text, txt_password.Password);
            client.PrintWorkingDirectory();
            btn_connect_Click_1(null, null);
            var request = client.createRequest(WebRequestMethods.Ftp.PrintWorkingDirectory);
            if (wh.Content == "")
            {
                wh.Content = txt_adres.Text;
            }
            else
            {
                wh.Content = "";
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            client = new Client(txt_adres.Text, txt_login.Text, txt_password.Password);
            client.DeleteFile(fl.Text);
            btn_connect_Click_1(null, null);
            var request = client.createRequest(client.combine(client.uri, fl.Text), WebRequestMethods.Ftp.DeleteFile);
        }

        private void lbx_files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public void UploadF(string path)
        {
            client = new Client(txt_adres.Text, txt_login.Text, txt_password.Password);
            FileInfo fileInf = new FileInfo(path);
            string name = path.Remove(0, path.LastIndexOf("\\") + 1);
            FileStream uploadedFile = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] fileBytes = new byte[uploadedFile.Length];
            uploadedFile.Read(fileBytes, 0, fileBytes.Length);
            uploadedFile.Close();
            string absolutePath = "ftp://" + "127.0.0.1" + "/" + fileInf.Name;
            FtpWebRequest ftpRequest = client.createRequest(absolutePath, WebRequestMethods.Ftp.UploadFile);
            Stream writer = ftpRequest.GetRequestStream();
            writer.Write(fileBytes, 0, fileBytes.Length);
            writer.Close();
        }

        public void DownloadF(string serverPath, string localPath = "")
        {
            client = new Client(txt_adres.Text, txt_login.Text, txt_password.Password);
            int bufferSize = 2048;
            if (string.IsNullOrEmpty(localPath))
                localPath = serverPath.Substring(serverPath.LastIndexOf("/") + 1);

            var ftpRequest = client.createRequest(localPath, WebRequestMethods.Ftp.DownloadFile);

            using (var response = (FtpWebResponse)ftpRequest.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var fileSteam = new FileStream(localPath, FileMode.Open))
                    {
                        byte[] buffer = new byte[bufferSize];
                        int readCount = stream.Read(buffer, 0, bufferSize);
                        while (readCount > 0)
                        {
                            fileSteam.Write(buffer, 0, readCount);
                            readCount = stream.Read(buffer, 0, bufferSize);
                        }
                    }
                }
            }

        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string sourceFile = openFileDialog.FileName;
                UploadF(sourceFile);
                btn_connect_Click_1(null, null);
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            string FileName = fl.Text;
            string sPath = @"C:\server\";
            string tPath = @"C:\server_save_def\";
            string dFile = Path.Combine(tPath, FileName);
            string sFile = Path.Combine(sPath, FileName);
            System.IO.File.Copy(sFile, dFile, true);
        }

        private void Lbx_files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }
    }
}



