using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Ionic.Zip;
using Microsoft.Win32;

namespace Brute_Force_password_cracker.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _title = "Brute Force password cracker";
        public string Title { get => _title; set { _title = value; OnPropertyChanged(nameof(Title)); } }

        private string _zipPath = "> target.zip";
        public string ZipPath { get => _zipPath; set { _zipPath = value; OnPropertyChanged(nameof(ZipPath)); } }

        private string _inputMin = "3";
        public string InputMin { get => _inputMin; set { _inputMin = value; OnPropertyChanged(nameof(InputMin)); } }

        private string _inputMax = "20";
        public string InputMax { get => _inputMax; set { _inputMax = value; OnPropertyChanged(nameof(InputMax)); } }

        private bool _isLeters = true;
        public bool IsLeters { get => _isLeters; set { _isLeters = value; OnPropertyChanged(nameof(IsLeters)); } }

        public bool IsCapital { get; set; }
        public bool IsNumbers { get; set; }
        public bool IsSymbols { get; set; }
        public bool IsIteratively { get; set; }
        public bool IsRecursively { get; set; }

        private string _manualPass;
        public string ManualPass { get => _manualPass; set { _manualPass = value; OnPropertyChanged(nameof(ManualPass)); } }

        private string _ruleText;
        public string RuleText { get => _ruleText; set { _ruleText = value; OnPropertyChanged(nameof(RuleText)); } }

        private bool _isExecuting = true;
        public bool IsExecuting { get => _isExecuting; set { _isExecuting = value; OnPropertyChanged(nameof(IsExecuting)); } }

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public ICommand FireCommand { get; }
        public ICommand ExecuteCommand { get; }

        private readonly string dLetters = "abcdefghijklmnopqrstuvwxyz";
        private readonly string dCapital = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly string dNumbers = "0123456789";
        private readonly string dSigns = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~\\ ";

        public MainViewModel()
        {
            FireCommand = new RelayCommand(o => SelectFile());
            ExecuteCommand = new RelayCommand(o => RunCracker());
        }

        private void SelectFile()
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "ZIP file (*.zip)|*.zip" };
            if (dialog.ShowDialog() == true) ZipPath = dialog.FileName;
        }

        private async void RunCracker()
        {
            Title = "Searching password... Please wait.";
            IsExecuting = false;
            Logs.Clear();

            await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(ManualPass))
                {
                    if (VerifyPassword(ManualPass, ZipPath))
                        MessageBox.Show($"Znalezione hasło to {ManualPass}");
                    else
                        MessageBox.Show("Hasło nieprawidłowe");
                }
                else
                {
                    EncryptedFileDictionaryPassword();
                    StartBruteForceLogic();
                }
            });

            Title = "Brute Force password cracker";
            IsExecuting = true;
        }

        private void EncryptedFileDictionaryPassword()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string dictionaryPath = Path.Combine(baseDirectory, "password-wordlist.txt");

            if (!File.Exists(dictionaryPath)) return;

            string[] txt = File.ReadAllLines(dictionaryPath);
            string zipPath = ZipPath;

            foreach (var entry in txt)
            {
                AddLog($"(Słownik) {entry}");
                if (VerifyPassword(entry, zipPath))
                {
                    MessageBox.Show($"Password found: {entry}");
                    break;
                }
            }
        }

        private void StartBruteForceLogic()
        {
            int.TryParse(InputMin, out int min);
            int.TryParse(InputMax, out int max);

            if (min > max) { MessageBox.Show("Min > Max!"); return; }

            string charBase = "";
            if (IsLeters) charBase += dLetters;
            if (IsNumbers) charBase += dNumbers;
            if (IsSymbols) charBase += dSigns;
            if (IsCapital) charBase += dCapital;

            int workerCount = Math.Max(1, Environment.ProcessorCount - 1);
            Stopwatch sw = Stopwatch.StartNew();

            if (IsIteratively)
            {
                BruteForceIteratively(min, max, charBase, workerCount);
            }
            else if (IsRecursively)
            {
                for (int len = min; len <= max; len++)
                {
                    if (BruteForceRecursive(charBase.ToCharArray(), new char[len], 0)) break;
                }
            }
            else
            {
                MessageBox.Show("Wybierz metodę!");
            }

            sw.Stop();
            Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Czas: {sw.Elapsed:mm\\:ss\\.ff}"));
        }

        private bool VerifyPassword(string pass, string path)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                using (ZipFile zip = ZipFile.Read(path))
                {
                    zip.Password = pass;
                    var entry = zip.Entries.FirstOrDefault(e => !e.IsDirectory);
                    if (entry == null) return false;
                    entry.Extract(Stream.Null);
                }
                return true;
            }
            catch { return false; }
        }

        private void AddLog(string pass)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Add($"{DateTime.Now:HH:mm:ss}: {pass}");
                if (Logs.Count > 100) Logs.RemoveAt(0);
            });
        }

        private bool BruteForceIteratively(int min, int max, string defalutToken, int workerCount)
        {
            var baseTokens = (RuleText ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries);

            for (int len = min; len <= max; len++)
            {
                char[][] allowed = new char[len][];
                for (int i = 0; i < len; i++)
                {
                    if (i >= baseTokens.Length)
                    {
                        allowed[i] = defalutToken.ToCharArray();
                        continue;
                    }
                    string tok = baseTokens[i];
                    bool isLit = tok.Length == 1 && !"*&!#".Contains(tok[0]);
                    allowed[i] = isLit ? new[] { tok[0] } : GetFromToken(tok);
                }

                long total = allowed.Aggregate(1L, (acc, arr) => acc * arr.Length);
                int found = 0;
                string result = null;
                Task[] tasks = new Task[workerCount];

                for (int workerId = 0; workerId < workerCount; workerId++)
                {
                    int id = workerId;
                    tasks[id] = Task.Run(() =>
                    {
                        char[] pass = new char[len];
                        for (long j = id; j < total && Volatile.Read(ref found) == 0; j += workerCount)
                        {
                            long t = j;
                            for (int p = len - 1; p >= 0; p--)
                            {
                                pass[p] = allowed[p][(int)(t % allowed[p].Length)];
                                t /= allowed[p].Length;
                            }

                            string sPass = new string(pass);
                            AddLog($"[W{id}] {sPass}");

                            if (VerifyPassword(sPass, ZipPath))
                            {
                                if (Interlocked.Exchange(ref found, 1) == 0)
                                    result = sPass;
                                break;
                            }
                        }
                    });
                }

                Task.WaitAll(tasks);

                if (found == 1)
                {
                    MessageBox.Show("Znaleziono: " + result);
                    return true;
                }
            }
            return false;
        }

        private bool BruteForceRecursive(char[] set, char[] pass, int idx)
        {
            if (idx == pass.Length)
            {
                string cand = new string(pass);
                AddLog(cand);
                return VerifyPassword(cand, ZipPath);
            }

            char[] currentSet = set;
            var tokens = (RuleText ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (idx < tokens.Length)
            {
                string tok = tokens[idx];
                currentSet = !"*&!#".Contains(tok[0]) ? tok.ToCharArray() : GetFromToken(tok);
            }

            foreach (char c in currentSet)
            {
                pass[idx] = c;
                if (BruteForceRecursive(set, pass, idx + 1)) return true;
            }
            return false;
        }

        private char[] GetFromToken(string t)
        {
            string res = "";
            if (t.Contains("*")) res += dLetters;
            if (t.Contains("&")) res += dCapital;
            if (t.Contains("!")) res += dNumbers;
            if (t.Contains("#")) res += dSigns;
            return res.ToCharArray();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}