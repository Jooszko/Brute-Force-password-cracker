using Brute_Force_password_cracker.Models;
using Brute_Force_password_cracker.Services;
using Brute_Force_password_cracker.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Brute_Force_password_cracker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Services
        private readonly PasswordCrackerService _passwordCracker;
        private readonly DictionaryAttackService _dictionaryService;
        private readonly FileDialogService _fileDialogService;
        private CancellationTokenSource _cancellationTokenSource;

        // Commands
        public ICommand SelectFileCommand { get; }
        public ICommand ExecuteCommand { get; }
        public ICommand CancelCommand { get; }

        // Properties
        private string _title = "Brute Force Password Cracker";
        public string Title
        {
            get => _title;
            set => SetField(ref _title, value);
        }

        private string _zipPath = "> target.zip";
        public string ZipPath
        {
            get => _zipPath;
            set => SetField(ref _zipPath, value);
        }

        private string _inputMin = "3";
        public string InputMin
        {
            get => _inputMin;
            set => SetField(ref _inputMin, value);
        }

        private string _inputMax = "8";
        public string InputMax
        {
            get => _inputMax;
            set => SetField(ref _inputMax, value);
        }

        private bool _isLeters = true;
        public bool IsLeters
        {
            get => _isLeters;
            set => SetField(ref _isLeters, value);
        }

        private bool _isCapital;
        public bool IsCapital
        {
            get => _isCapital;
            set => SetField(ref _isCapital, value);
        }

        private bool _isNumbers;
        public bool IsNumbers
        {
            get => _isNumbers;
            set => SetField(ref _isNumbers, value);
        }

        private bool _isSymbols;
        public bool IsSymbols
        {
            get => _isSymbols;
            set => SetField(ref _isSymbols, value);
        }

        private string _ruleText;
        public string RuleText
        {
            get => _ruleText;
            set => SetField(ref _ruleText, value);
        }

        private bool _isIteratively = true;
        public bool IsIteratively
        {
            get => _isIteratively;
            set
            {
                if (SetField(ref _isIteratively, value) && value)
                {
                    IsRecursively = false;
                    IsDictionary = false;
                    AddLog("Method changed to Iterative");
                }
            }
        }

        private bool _isRecursively;
        public bool IsRecursively
        {
            get => _isRecursively;
            set
            {
                if (SetField(ref _isRecursively, value) && value)
                {
                    IsIteratively = false;
                    IsDictionary = false;
                    AddLog("Method changed to Recursive");
                }
            }
        }

        private bool _isDictionary;
        public bool IsDictionary
        {
            get => _isDictionary;
            set
            {
                if (SetField(ref _isDictionary, value) && value)
                {
                    IsIteratively = false;
                    IsRecursively = false;
                    AddLog("Method changed to Dictionary Attack");
                }
            }
        }

        private bool _isExecuting = true;
        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                if (SetField(ref _isExecuting, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _manualPassword;
        public string ManualPassword
        {
            get => _manualPassword;
            set => SetField(ref _manualPassword, value);
        }

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public MainViewModel(PasswordCrackerService passwordCracker, DictionaryAttackService dictionaryService, FileDialogService fileDialogService)
        {
            _passwordCracker = passwordCracker;
            _dictionaryService = dictionaryService;
            _fileDialogService = fileDialogService;

            SelectFileCommand = new RelayCommand(SelectFile, _ => IsExecuting);
            ExecuteCommand = new RelayCommand(async p => await ExecuteCracking(p), _ => IsExecuting && !string.IsNullOrEmpty(ZipPath) && File.Exists(ZipPath));
            CancelCommand = new RelayCommand(CancelCracking, _ => !IsExecuting);
        }

        public MainViewModel() : this(new PasswordCrackerService(), new DictionaryAttackService(), new FileDialogService())
        {
        }

        private void SelectFile(object parameter)
        {
            var filePath = _fileDialogService.ShowOpenFileDialog("ZIP files (*.zip)|*.zip", "Select ZIP file");
            if (!string.IsNullOrEmpty(filePath))
            {
                ZipPath = filePath;
                AddLog($"Selected ZIP file: {Path.GetFileName(filePath)}");
            }
        }

        public async Task ExecuteCracking(object parameter)
        {
            if (!IsExecuting) return;

            Title = "Searching password... Please wait.";
            IsExecuting = false;
            Logs.Clear();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                CrackingResult result = null;

                if (!string.IsNullOrEmpty(ManualPassword))
                {
                    AddLog($"Testing manual password: {ManualPassword}");
                    var session = new CrackingSession { FilePath = ZipPath };
                    result = await _passwordCracker.VerifySinglePasswordAsync(
                        ManualPassword,
                        session,
                        AddLog,
                        _cancellationTokenSource.Token);
                }
                else if (IsDictionary)
                {
                    string dictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "password-wordlist.txt");

                    if (File.Exists(dictionaryPath))
                    {
                        AddLog($"Using dictionary: {Path.GetFileName(dictionaryPath)}");
                        AddLog("Starting dictionary attack...");

                        result = await _dictionaryService.TryDictionaryAttackAsync(
                            ZipPath,
                            dictionaryPath,
                            AddLog,
                            _cancellationTokenSource.Token
                            );
                    }
                    else
                    {
                        AddLog($"ERROR: Dictionary file not found at: {dictionaryPath}");
                        AddLog("Please make sure 'password-wordlist.txt' is in the same folder as the executable.");
                        return;
                    }
                }
                else
                {
                    if (!int.TryParse(InputMin, out int min) || !int.TryParse(InputMax, out int max))
                    {
                        AddLog("Error: Invalid length values!");
                        return;
                    }

                    if (min > max)
                    {
                        AddLog("Error: Min length cannot be greater than Max length!");
                        return;
                    }

                    var session = new CrackingSession
                    {
                        FilePath = ZipPath,
                        MinLength = min,
                        MaxLength = max,
                        IncludeLowercase = IsLeters,
                        IncludeUppercase = IsCapital,
                        IncludeNumbers = IsNumbers,
                        IncludeSymbols = IsSymbols,
                        RulePattern = RuleText,
                        Method = IsIteratively ? CrackingMethod.Iterative : CrackingMethod.Recursive
                    };

                    AddLog($"Starting {session.Method} attack...");
                    AddLog($"File: {Path.GetFileName(ZipPath)}");
                    AddLog($"Length range: {min}-{max}");
                    AddLog($"Characters: Lowercase={IsLeters}, Uppercase={IsCapital}, Numbers={IsNumbers}, Symbols={IsSymbols}");
                    AddLog($"Rule pattern: {RuleText ?? "None"}");

                    result = await _passwordCracker.CrackPasswordAsync(
                        session,
                        AddLog,
                        _cancellationTokenSource.Token);
                }

                if (result != null)
                {
                    if (result.Success)
                    {
                        AddLog($"SUCCESS! Password found: {result.FoundPassword}");
                        AddLog($"Duration: {result.Duration:mm\\:ss\\.ff}");
                        ShowMessage($"Password found: {result.FoundPassword}\nDuration: {result.Duration:mm\\:ss\\.ff}");
                    }
                    else if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        AddLog($"Error: {result.ErrorMessage}");
                    }
                    else
                    {
                        AddLog("No password found.");
                    }
                }
                else
                {
                    AddLog("No result returned from cracking service.");
                }
            }
            catch (OperationCanceledException)
            {
                AddLog("Operation cancelled by user.");
            }
            catch (Exception ex)
            {
                AddLog($"Error: {ex.Message}");
            }
            finally
            {
                Title = "Brute Force Password Cracker";
                IsExecuting = true;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void CancelCracking(object parameter)
        {
            AddLog("Cancelling operation...");
            _cancellationTokenSource?.Cancel();
        }

        private void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var fullMessage = $"{timestamp}: {message}";

            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Logs.Add(fullMessage);
                    if (Logs.Count > 100) Logs.RemoveAt(0);
                });
            }
            else
            {
                Logs.Add(fullMessage);
            }
        }

        private void ShowMessage(string message)
        {
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(message, "Result", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}