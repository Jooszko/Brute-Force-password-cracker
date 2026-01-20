using Xunit;
using Brute_Force_password_cracker.Services;
using Ionic.Zip;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace BruteForce.Tests.Services
{
    public class DictionaryAttackServiceTests : IDisposable
    {
        private readonly DictionaryAttackService _service;

        private string _tempZipPath;
        private string _tempDictPath;

        public DictionaryAttackServiceTests()
        {

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            _service = new DictionaryAttackService();
        }

        private void SetupFiles(string zipPassword, string[] dictWords)
        {
            _tempZipPath = Path.Combine(Path.GetTempPath(), $"zip_{Guid.NewGuid()}.zip");
            _tempDictPath = Path.Combine(Path.GetTempPath(), $"dict_{Guid.NewGuid()}.txt");


            using (var zip = new ZipFile())
            {
                zip.Password = zipPassword;
                zip.AddEntry("data.txt", "content");
                zip.Save(_tempZipPath);
            }

            File.WriteAllLines(_tempDictPath, dictWords);
        }

        [Fact]
        public async Task TryDictionaryAttack_ShouldFindPassword_IfInDictionary()
        {

            string correctPass = "admin123";
            string[] dictionary = { "123456", "password", "admin123", "qwerty" };
            SetupFiles(correctPass, dictionary);


            var result = await _service.TryDictionaryAttackAsync(
                _tempZipPath,
                _tempDictPath,
                _ => { },
                CancellationToken.None);


            Assert.True(result.Success);
            Assert.Equal(correctPass, result.FoundPassword);
            Assert.True(result.AttemptsCount > 0);
        }

        [Fact]
        public async Task TryDictionaryAttack_ShouldFail_IfNotInDictionary()
        {
            SetupFiles("supersecret", new[] { "cat", "dog", "bird" });

            var result = await _service.TryDictionaryAttackAsync(
                _tempZipPath,
                _tempDictPath,
                _ => { },
                CancellationToken.None);


            Assert.False(result.Success);
            Assert.Null(result.FoundPassword);
        }

        public void Dispose()
        {
            if (File.Exists(_tempZipPath)) File.Delete(_tempZipPath);
            if (File.Exists(_tempDictPath)) File.Delete(_tempDictPath);

        }
    }
}