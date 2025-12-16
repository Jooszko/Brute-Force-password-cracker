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
using System.Threading;
using System.Diagnostics;

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
            tbxTitle.Text = "Searching password... Please wait.";
            btnExecute.IsEnabled = false;

            if (pwdZip.Password != null && pwdZip.Password != "") {
                if (VerifyPassword(pwdZip.Password, tbInfo.Text))
                    MessageBox.Show($"Znalezione hasło to {pwdZip.Password}");
                else
                    MessageBox.Show($"Hasło nieprawidłowe");

                tbxTitle.Text = "Brute Force password cracker";
                btnExecute.IsEnabled = true;
            }
            else
            {
                BruteForce();
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


        private void BruteForce()
        {
            //EncryptedFileDictionaryPassword();
            int minPassLen = 0, maxPassLen = 0;

            bool lettersPass = CbLeters.IsChecked == true;
            bool numbersPass = CbNumbers.IsChecked == true;
            bool symbolsPass = CbSymbols.IsChecked == true;

            int.TryParse(InputMin.Text, out minPassLen);
            int.TryParse(InputMax.Text, out maxPassLen);
            if (minPassLen > maxPassLen)
            {
                MessageBox.Show("Minimalna długość hasła nie może byc wieksza od maksymalnej");
                return;
            }

            string dictionaryLetters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string dictionaryNumbers = "0123456789";
            string dictionarySigns = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~\\ ";

            string currentCharacterBase = "";

            if (lettersPass) currentCharacterBase += dictionaryLetters;
            if (numbersPass) currentCharacterBase += dictionaryNumbers;
            if (symbolsPass) currentCharacterBase += dictionarySigns;

            char[] charSet = currentCharacterBase.ToCharArray();
            string zipPath = tbInfo.Text;
            bool passwordFound = false;


            if(CbIteratively.IsChecked == true)
            {
                new Thread(() =>
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    BruteForceIteratively(charSet, minPassLen, maxPassLen, zipPath);

                    sw.Stop();
                    string time = sw.Elapsed.ToString(@"mm\:ss\.ff");
                    MessageBox.Show($"Czas trwania ataku: {time}");

                    ResultsList.Dispatcher.Invoke(() =>
                    {
                        tbxTitle.Text = "Brute Force password cracker";
                        btnExecute.IsEnabled = true;
                    });

                }).Start();
            }
            else if (CbRecursively.IsChecked == true)
            {
                new Thread(() =>
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    for (int length = minPassLen; length <= maxPassLen; length++)
                    {
                        char[] password = new char[length];
                        if (BruteForceAttack(charSet, password, 0, zipPath))
                        {
                            passwordFound = true;
                            break;
                        }
                    }

                    sw.Stop();
                    string time = sw.Elapsed.ToString(@"mm\:ss\.ff");
                    MessageBox.Show($"Czas trwania ataku: {time}");

                    ResultsList.Dispatcher.Invoke(() =>
                    {
                        tbxTitle.Text = "Brute Force password cracker";
                        btnExecute.IsEnabled = true;
                    });

                    if (!passwordFound)
                        MessageBox.Show("Hasła nie znaleziono w podanym zakresie");
                }).Start();
            }
            else
            {
                ResultsList.Dispatcher.Invoke(() =>
                {
                    tbxTitle.Text = "Brute Force password cracker";
                    btnExecute.IsEnabled = true;
                });
                MessageBox.Show("Wybierz metodę łamania hasła: rekurencyjnie lub iteracyjnie.");
            }
            
        }

        private bool IsMatch(string candidateText, string zipPath)
        {
            return VerifyPassword(candidateText, zipPath);
        }

        private bool BruteForceIteratively(char[]charSet, int minPassLen, int maxPassLen, string zipPath)
        {

            for (int length = minPassLen; length <= maxPassLen; length++)
            {
                long total = 1;
                for (int k = 0; k < length; k++)
                    total *= charSet.Length;

                for (long i = 0; i < total; i++)
                {
                    char[] password = new char[length];
                    long t = i;

                    for (int pos = length - 1; pos >= 0; pos--)
                    {
                        password[pos] = charSet[t % charSet.Length];
                        t /= charSet.Length;
                    }

                    string passwordToTest = new string(password);
                    ResultsList.Dispatcher.Invoke(() => AddLog(new string(password)));

                    if (VerifyPassword(passwordToTest, zipPath))
                    {
                        MessageBox.Show($"Złamane hasło to: {passwordToTest}");
                        return true;
                    }
                }
                              
            }
            return false;
        }

        private bool BruteForceAttack(char[] charSet, char[] password, int index, string zipPath) //recursive
        {
            if (index == password.Length)
            {
                string candidate = new string(password);
                if (IsMatch(candidate, zipPath))
                {
                    MessageBox.Show($"Password found: {candidate}");
                    return true;
                }
                return false;
            }
            foreach (char c in charSet)
            {
                password[index] = c;
                ResultsList.Dispatcher.Invoke(() => AddLog(new string(password)));


                if (BruteForceAttack(charSet, password, index + 1, zipPath))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddLog(string text) //displaying checked passwords
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string entry = $"{DateTime.Now:HH:mm:ss}: {text}";
                ResultsList.Items.Add(entry);

                if (ResultsList.Items.Count > 0)
                {
                    var lastItem = ResultsList.Items[ResultsList.Items.Count - 1];
                    ResultsList.ScrollIntoView(lastItem);
                }
            });

            if (ResultsList.Items.Count > 1000)
            {
                ResultsList.Items.RemoveAt(0);
    
            }
            ResultsList.ScrollIntoView(ResultsList.Items[ResultsList.Items.Count - 1]);
        }
    }
}