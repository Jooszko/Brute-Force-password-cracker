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
using System.Text.RegularExpressions;

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
            int minPassLen = 0;
            int maxPassLen = 0;

            bool lettersPass = CbLeters.IsChecked == true;
            bool numbersPass = CbNumbers.IsChecked == true;
            bool symbolsPass = CbSymbols.IsChecked == true;

            int.TryParse(InputMin.Text, out minPassLen);
            int.TryParse(InputMax.Text, out maxPassLen);

            string dictionaryLetters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string dictionaryNumbers = "0123456789";

            string dictionarySigns = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~\\ ";

            string currentCharacterBase = "";

            if (lettersPass)
            {
                currentCharacterBase += dictionaryLetters;
            }

            if (numbersPass)
            {
                currentCharacterBase += dictionaryNumbers;
            }

            if (symbolsPass)
            {
                currentCharacterBase += dictionarySigns;
            }

            //currentCharacterBase  - słownik z którego finalnie tworzymy słowa do testowania

            MessageBox.Show($"{currentCharacterBase}");


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


                    ZipEntry entry = zip.FirstOrDefault(e => e.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

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

        private void DictionaryAttack_Click(object sender, RoutedEventArgs e)
        {
            EncryptedFileDictionaryPassword();
        }


        private bool VerifyPassword(string passwordToTest, string zipPath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            try
            {
                using (ZipFile zip = ZipFile.Read(zipPath))
                {
                    zip.Password = passwordToTest;

                    ZipEntry entry = zip.Entries.FirstOrDefault(e => !e.IsDirectory);

                    if (entry == null) return false;

                    entry.Extract(Stream.Null);
                }

                return true;
            }
            catch (BadPasswordException)
            {
                return false;
            }
            catch (ZipException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void EncryptedFileDictionaryPassword()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string dictionaryPath = Path.Combine(baseDirectory, "password-wordlist.txt");

            string[] txt = File.ReadAllLines(dictionaryPath);
            string zipPath = tbInfo.Text;

            if (!File.Exists(zipPath))
            {
                MessageBox.Show("Choose correct ZIP file.");
                return;
            }

            try
            {
                bool checke = false;
                foreach (var entry in txt)
                {
                    bool check = VerifyPassword(entry, zipPath);
                    if (check)
                    {
                        MessageBox.Show($"Password found: {entry}");
                        checke = true;
                        break;
                    }
                }
                if(!checke)
                    MessageBox.Show("Password not found in dictionary.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ValidateCheckBox(object sender, RoutedEventArgs e)
        {
            if (CbLeters.IsChecked == false &&
                CbNumbers.IsChecked == false &&
                CbSymbols.IsChecked == false)
            {
                ((CheckBox)sender).IsChecked = true;
            }
        }


    }
}