using Ionic.Zip;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Brute_Force_password_cracker.Models;

namespace Brute_Force_password_cracker.Services
{
    public class DictionaryAttackService
    {
        public virtual async Task<CrackingResult> TryDictionaryAttackAsync(
            string zipPath,
            string dictionaryPath,
            Action<string> logAction,
            CancellationToken cancellationToken
            )
        {
            var result = new CrackingResult
            {
                FilePath = zipPath,
                Timestamp = DateTime.Now
            };

            if (!File.Exists(dictionaryPath))
            {
                result.ErrorMessage = "Dictionary file not found";
                return result;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long attempts = 0;
            string foundPassword = null;

            try
            {
                var lines = File.ReadLines(dictionaryPath);
                long currentLine = 0;

                foreach (var password in lines)
                {
                    currentLine++;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        logAction?.Invoke("Dictionary attack cancelled.");
                        break;
                    }

                    if (currentLine % 1000 == 0)
                        logAction?.Invoke($"{password} password number {currentLine} in dictionary");

                    attempts++;

                    if (await VerifyPasswordAsync(password.Trim(), zipPath))
                    {
                        foundPassword = password.Trim();
                        logAction?.Invoke($"(Dictionary) SUCCESS! Password found: {foundPassword}");

                        break;
                    }
                }

                stopwatch.Stop();
                result.Success = foundPassword != null;
                result.FoundPassword = foundPassword;
                result.AttemptsCount = (int)Math.Min(attempts, int.MaxValue);
                result.Duration = stopwatch.Elapsed;

            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Operation cancelled";
                logAction?.Invoke("Dictionary attack cancelled.");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                logAction?.Invoke($"Dictionary attack error: {ex.Message}");
            }

            return result;
        }

        public virtual async Task<bool> VerifyPasswordAsync(string password, string zipPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var zip = ZipFile.Read(zipPath))
                    {
                        zip.Password = password;
                        var entry = zip.Entries.FirstOrDefault(e => !e.IsDirectory);
                        if (entry == null) return false;

                        using (var stream = new System.IO.MemoryStream())
                        {
                            entry.Extract(stream);
                        }
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}