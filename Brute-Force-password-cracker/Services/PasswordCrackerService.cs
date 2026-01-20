using Brute_Force_password_cracker.Models;
using Ionic.Zip;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Brute_Force_password_cracker.Services
{
    public class PasswordCrackerService
    {
        private readonly string dLetters = "abcdefghijklmnopqrstuvwxyz";
        private readonly string dCapital = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly string dNumbers = "0123456789";
        private readonly string dSigns = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~\\ ";

        private volatile bool _stopRecursion = false;
        private string _foundPassword = null;
        private Action<string> _logAction;
        private string _zipPath;
        private string _ruleText;

        private long counter = 0;

        public virtual async Task<CrackingResult> CrackPasswordAsync(CrackingSession session, Action<string> logAction, CancellationToken cancellationToken)
        {
            _logAction = logAction;
            _zipPath = session.FilePath;
            _ruleText = session.RulePattern ?? "";
            _stopRecursion = false;
            _foundPassword = null;

            var result = new CrackingResult
            {
                FilePath = session.FilePath,
                Timestamp = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await Task.Run(() =>
                {
                    int min = session.MinLength;
                    int max = session.MaxLength;

                    if (min > max)
                    {
                        throw new ArgumentException("Min > Max!");
                    }

                    string charBase = BuildCharacterSet(session);

                    if (!string.IsNullOrEmpty(_ruleText))
                    {
                        _logAction?.Invoke($"Using rule pattern: {_ruleText}");
                    }

                    if (session.Method == Common.CrackingMethod.Iterative)
                    {
                        _logAction?.Invoke("Starting iterative cracking...");
                        BruteForceIteratively(min, max, charBase, Math.Max(1, Environment.ProcessorCount - 1), cancellationToken);
                    }
                    else if (session.Method == Common.CrackingMethod.Recursive)
                    {
                        _logAction?.Invoke("Starting recursive cracking...");
                        BruteForceRecursiveMultiThreaded(min, max, charBase, cancellationToken);
                    }

                }, cancellationToken);

                stopwatch.Stop();
                result.Success = _foundPassword != null;
                result.FoundPassword = _foundPassword;
                result.Duration = stopwatch.Elapsed;
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Operation cancelled";
                _logAction?.Invoke("Cracking cancelled.");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private string BuildCharacterSet(CrackingSession session)
        {
            string charBase = "";
            if (session.IncludeLowercase) charBase += dLetters;
            if (session.IncludeUppercase) charBase += dCapital;
            if (session.IncludeNumbers) charBase += dNumbers;
            if (session.IncludeSymbols) charBase += dSigns;
            return charBase;
        }

        private bool BruteForceIteratively(int min, int max, string defaultToken, int workerCount, CancellationToken cancellationToken)
        {
            var baseTokens = (_ruleText ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries);

            for (int len = min; len <= max; len++)
            {
                if (cancellationToken.IsCancellationRequested || _stopRecursion)
                {
                    _logAction?.Invoke("Cracking cancelled.");
                    return false;
                }

                _logAction?.Invoke($"Testing length {len}...");

                if (baseTokens.Length > 0 && baseTokens.Length > len)
                {
                    continue;
                }

                char[][] allowed = new char[len][];
                for (int i = 0; i < len; i++)
                {
                    if (i >= baseTokens.Length || string.IsNullOrEmpty(baseTokens[i]))
                    {
                        allowed[i] = defaultToken.ToCharArray();
                        continue;
                    }

                    string tok = baseTokens[i].Trim();
                    bool isLit = tok.Length == 1 && !"*&!#".Contains(tok[0]);
                    allowed[i] = isLit ? new[] { tok[0] } : GetFromToken(tok);
                }

                long total = allowed.Aggregate(1L, (acc, arr) => acc * arr.Length);
                _logAction?.Invoke($"Total combinations for length {len}: {total:N0}");

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
                            if (cancellationToken.IsCancellationRequested || _stopRecursion)
                                break;

                            long t = j;
                            for (int p = len - 1; p >= 0; p--)
                            {
                                pass[p] = allowed[p][(int)(t % allowed[p].Length)];
                                t /= allowed[p].Length;
                            }

                            string sPass = new string(pass);

                            if (j % 1000 == 0)
                            {
                                _logAction?.Invoke($"[W{id}] Testing: {sPass}");
                            }

                            if (VerifyPassword(sPass, _zipPath))
                            {
                                if (Interlocked.Exchange(ref found, 1) == 0)
                                {
                                    result = sPass;
                                    _foundPassword = sPass;
                                    _logAction?.Invoke($"FOUND PASSWORD: {sPass}");
                                }
                                break;
                            }
                        }
                    }, cancellationToken);
                }

                try
                {
                    Task.WaitAll(tasks, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logAction?.Invoke("Task cancelled.");
                    return false;
                }

                if (found == 1)
                {
                    return true;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logAction?.Invoke("Cracking cancelled between lengths.");
                    return false;
                }
            }
            return false;
        }

        private bool BruteForceRecursiveMultiThreaded(int min, int max, string defaultToken, CancellationToken cancellationToken)
        {
            _stopRecursion = false;
            var tokens = (_ruleText ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1),
                CancellationToken = cancellationToken
            };

            for (int len = min; len <= max; len++)
            {
                if (_stopRecursion || cancellationToken.IsCancellationRequested)
                {
                    _logAction?.Invoke("Cracking cancelled.");
                    break;
                }

                _logAction?.Invoke($"Recursive testing length {len}...");
                double total = Math.Pow(defaultToken.Length, len);
                _logAction?.Invoke($"Total combinations for length {len}: {total:N0}");

                if (tokens.Length > 0 && tokens.Length > len)
                {
                    continue;
                }

                char[] level0 = GetCharsForLevel(0, tokens, defaultToken);

                if (len == 1)
                {
                    try
                    {
                        Parallel.ForEach(level0, parallelOptions, (c, state) =>
                        {
                            if (_stopRecursion || cancellationToken.IsCancellationRequested)
                                state.Stop();

                            if (VerifyPassword(c.ToString(), _zipPath))
                            {
                                _foundPassword = c.ToString();
                                _stopRecursion = true;
                                state.Stop();
                                _logAction?.Invoke($"FOUND PASSWORD: {c}");
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        _logAction?.Invoke("Parallel loop cancelled.");
                    }
                }
                else
                {
                    char[] level1 = GetCharsForLevel(1, tokens, defaultToken);

                    var combinations = from c0 in level0
                                       from c1 in level1
                                       select new { C0 = c0, C1 = c1 };

                    try
                    {
                        Parallel.ForEach(combinations, parallelOptions, (pair, state) =>
                        {
                            if (_stopRecursion || cancellationToken.IsCancellationRequested)
                                state.Stop();

                            char[] threadLocalPass = new char[len];
                            threadLocalPass[0] = pair.C0;
                            threadLocalPass[1] = pair.C1;

                            RecursiveWorker(defaultToken.ToCharArray(), threadLocalPass, 2, tokens, state, cancellationToken);
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        _logAction?.Invoke("Parallel loop cancelled.");
                    }
                }

                if (_stopRecursion || cancellationToken.IsCancellationRequested)
                {
                    _logAction?.Invoke("Breaking recursive loop.");
                    break;
                }
            }

            return _foundPassword != null;
        }

        private void RecursiveWorker(char[] defaultSet, char[] pass, int idx, string[] tokens, ParallelLoopState state, CancellationToken cancellationToken)
        {
            if (_stopRecursion || cancellationToken.IsCancellationRequested)
            {
                state.Stop();
                return;
            }

            if (idx == pass.Length)
            {
                string cand = new string(pass);

                if (Interlocked.Increment(ref counter) % 1000 == 0)
                {
                    _logAction?.Invoke($"Testing: {cand}");
                }

                if (VerifyPassword(cand, _zipPath))
                {
                    _foundPassword = cand;
                    _stopRecursion = true;
                    state.Stop();
                    _logAction?.Invoke($"FOUND PASSWORD: {cand}");
                }
                return;
            }

            char[] currentSet = GetCharsForLevel(idx, tokens, new string(defaultSet));

            foreach (char c in currentSet)
            {
                if (_stopRecursion || cancellationToken.IsCancellationRequested)
                {
                    state.Stop();
                    return;
                }

                pass[idx] = c;
                RecursiveWorker(defaultSet, pass, idx + 1, tokens, state, cancellationToken);

                if (_stopRecursion || cancellationToken.IsCancellationRequested)
                    return;
            }
        }

        private char[] GetFromToken(string t)
        {
            string res = "";
            t = t.Trim();

            if (t.Contains("*")) res += dLetters;
            if (t.Contains("&")) res += dCapital;
            if (t.Contains("!")) res += dNumbers;
            if (t.Contains("#")) res += dSigns;

            return res.Distinct().ToArray();
        }

        private char[] GetCharsForLevel(int idx, string[] tokens, string defaultToken)
        {
            if (idx < tokens.Length && !string.IsNullOrEmpty(tokens[idx]))
            {
                string tok = tokens[idx].Trim();
                if (tok.Length == 1 && !"*&!#".Contains(tok[0]))
                {
                    return new[] { tok[0] };
                }
                return GetFromToken(tok);
            }
            return defaultToken.ToCharArray();
        }

        private bool VerifyPassword(string pass, string path)
        {
            try
            {
                using (ZipFile zip = ZipFile.Read(path))
                {
                    zip.Password = pass;
                    var entry = zip.Entries.FirstOrDefault(e => !e.IsDirectory);
                    if (entry == null) return false;
                    //using (var stream = new System.IO.MemoryStream())
                    //{
                    //    entry.Extract(stream);
                    //}

                    using (var stream = System.IO.Stream.Null)
                    {
                        entry.Extract(stream);
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public virtual async Task<CrackingResult> VerifySinglePasswordAsync(string password, CrackingSession session, Action<string> logAction, CancellationToken cancellationToken)
        {
            var result = new CrackingResult
            {
                FilePath = session.FilePath,
                Timestamp = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                bool success = await Task.Run(() => VerifyPassword(password, session.FilePath), cancellationToken);

                stopwatch.Stop();
                result.Success = success;
                result.FoundPassword = success ? password : null;
                result.Duration = stopwatch.Elapsed;
                result.AttemptsCount = 1;
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Operation cancelled";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }
}