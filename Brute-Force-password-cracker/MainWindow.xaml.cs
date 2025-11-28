using Ionic.Zip;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Brute_Force_password_cracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnFire_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "ZIP file (*.zip)|*.zip";
            bool? success = fileDialog.ShowDialog();
            if(success== true)
            {
                string path = fileDialog.FileName;
                string fileName = fileDialog.FileName;

                tbInfo.Text = path;
            }
            else
            {

            }
        }

        private void ReadEncryptedFile_Click(object sender, RoutedEventArgs e)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string zipPath = tbInfo.Text;
            string password = pwdZip.Password;

            if (!File.Exists(zipPath))
            {
                MessageBox.Show("Choose correct ZIP file.");
                return;
            }

            try
            {
                using (ZipFile zip = ZipFile.Read(zipPath))
                {
                    zip.Password = password;


                    ZipEntry entry = zip
                        .FirstOrDefault(e => e.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

                    if (entry == null)
                    {
                        MessageBox.Show("File .txt not found");
                        return;
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        entry.Extract(ms);
                        ms.Position = 0;

                        string content;
                        using (StreamReader sr = new StreamReader(ms))
                        {
                            content = sr.ReadToEnd();
                        }

                        MessageBox.Show(
                            $"File found: {entry.FileName}\n\nContent of file:\n{content}"
                        );
                    }
                }
            }
            catch (BadPasswordException)
            {
                MessageBox.Show("Error bad password");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}