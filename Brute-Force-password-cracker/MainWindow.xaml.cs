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
            bool capitalPass = CbCapital.IsChecked == true;

            int.TryParse(InputMin.Text, out minPassLen);
            int.TryParse(InputMax.Text, out maxPassLen);
            if (minPassLen > maxPassLen)
            {
                MessageBox.Show("Minimalna długość hasła nie może byc wieksza od maksymalnej");
                return;
            }

            string dictionaryLetters = "abcdefghijklmnopqrstuvwxyz";
            string dictionaryCapital = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string dictionaryNumbers = "0123456789";
            string dictionarySigns = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~\\ ";

            string currentCharacterBase = "";

            if (lettersPass) currentCharacterBase += dictionaryLetters;
            if (numbersPass) currentCharacterBase += dictionaryNumbers;
            if (symbolsPass) currentCharacterBase += dictionarySigns;
            if (capitalPass) currentCharacterBase += dictionaryCapital;

            char[] charSet = currentCharacterBase.ToCharArray();
            string zipPath = tbInfo.Text;
            bool passwordFound = false;


            if(CbIteratively.IsChecked == true)
            {
                var rule = txtZip.Text;
                new Thread(() =>
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    
                    BruteForceIteratively(minPassLen, maxPassLen, capitalPass, lettersPass, numbersPass, symbolsPass, rule, zipPath, dictionaryLetters, dictionaryNumbers, dictionarySigns, dictionaryCapital);

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

        static string[] SplitTokens(string input)
                => input.Split('|', StringSplitOptions.RemoveEmptyEntries);


        static char[] AllowedForToken(string token, string dictionaryLetters, string dictionaryCapital, string dictionaryNumbers, string dictionarySigns)
        {
            switch (token)
            {
                case "*":
                    return dictionaryLetters.ToCharArray();

                case "&":
                    return dictionaryCapital.ToCharArray();

                case "!":
                    return dictionaryNumbers.ToCharArray();

                case "#":
                    return dictionarySigns.ToCharArray();

                case "*&":
                    return (dictionaryLetters + dictionaryCapital).ToCharArray();

                case "*!":
                    return (dictionaryLetters + dictionaryNumbers).ToCharArray();

                case "*#":
                    return (dictionaryLetters + dictionarySigns).ToCharArray();

                case "&!":
                    return (dictionaryCapital + dictionaryNumbers).ToCharArray();

                case "&#":
                    return (dictionaryCapital + dictionarySigns).ToCharArray();

                case "!#":
                    return (dictionaryNumbers + dictionarySigns).ToCharArray();

                case "*&!":
                    return (dictionaryLetters + dictionaryCapital + dictionaryNumbers).ToCharArray();

                case "*&#":
                    return (dictionaryLetters + dictionaryCapital + dictionarySigns).ToCharArray();

                case "*!#":
                    return (dictionaryLetters + dictionaryNumbers + dictionarySigns).ToCharArray();

                case "&!#":
                    return (dictionaryCapital + dictionaryNumbers + dictionarySigns).ToCharArray();

                case "*&!#":
                    return (dictionaryLetters + dictionaryCapital + dictionaryNumbers + dictionarySigns).ToCharArray();

                default:
                    throw new ArgumentException($"Nieznany token reguły: '{token}'");
            }
        }

        // ===== POJEDYNCZE =====
        // *     -> małe litery
        // &     -> duże litery
        // !     -> liczby
        // #     -> znaki specjalne

        // ===== PARY =====
        // *&    -> małe + duże litery
        // *!    -> małe litery + liczby
        // *#    -> małe litery + znaki specjalne
        // &!    -> duże litery + liczby
        // &#    -> duże litery + znaki specjalne
        // !#    -> liczby + znaki specjalne

        // ===== TRÓJKI =====
        // *&!   -> małe + duże + liczby
        // *&#   -> małe + duże + znaki specjalne
        // *!#   -> małe + liczby + znaki specjalne
        // &!#   -> duże + liczby + znaki specjalne

        // ===== WSZYSTKIE =====
        // *&!#  -> małe + duże + liczby + znaki specjalne
        private bool BruteForceIteratively(int min, int max, bool withCapital, bool withLetters, bool withNumbers, bool withSigns, string rule, string zipPath, string dictionaryLetters, string dictionaryCapital, string dictionaryNumbers, string dictionarySigns)
        {

            var baseTokens = SplitTokens(rule);
            

            string defalutToken = "";
            if (withCapital) defalutToken += dictionaryCapital;
            if (withLetters) defalutToken += dictionaryLetters;
            if (withNumbers) defalutToken += dictionaryNumbers;
            if (withSigns) defalutToken += dictionarySigns;

            if (defalutToken.Length == 0)
                throw new ArgumentException("Nie wybrano żadnego zestawu znaków.");

            for (int length = min; length <= max; length++)
            {
                var tokens = baseTokens.Take(length).ToArray();
                char[][] allowed = new char[length][];
                for (int pos = 0; pos < length; pos++)
                {
                    if (pos >= tokens.Length)
                    {
                        allowed[pos] = defalutToken.ToCharArray();
                        continue;
                    }
                    string tok = tokens[pos];
                    bool isLiteral = tok.Length == 1 && tok[0] != '*' && tok[0] != '&' && tok[0] != '!' && tok[0] != '#';

                    if (isLiteral) allowed[pos] = new[] { tok[0] };
                    else
                    {
                        allowed[pos] = AllowedForToken(tok, dictionaryLetters, dictionaryCapital, dictionaryNumbers, dictionarySigns);

                        if (allowed[pos].Length == 0)
                            throw new ArgumentException($"Token '{tok}' na pozycji {pos} zwrócił pusty zbiór znaków.");
                    }
                }

                long total = 1;
                for (int pos = 0; pos < length; pos++)
                {
                    total *= allowed[pos].Length;
                }
                for (long j = 0; j < total; j++)
                {
                    long t = j;
                    char[] password = new char[length];

                    for (int pos = length - 1; pos >= 0; pos--)
                    {
                        var set = allowed[pos];
                        password[pos] = set[t % set.Length];
                        t /= set.Length;
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