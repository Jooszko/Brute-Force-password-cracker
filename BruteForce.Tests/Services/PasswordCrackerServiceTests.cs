using Xunit;
using Brute_Force_password_cracker.Services;
using Brute_Force_password_cracker.Models;
using Brute_Force_password_cracker.Common; // Do enum CrackingMethod
using Ionic.Zip;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace BruteForce.Tests.Services
{
    public class PasswordCrackerServiceTests : IDisposable
    {
        private readonly PasswordCrackerService _service;
        private string _tempZipPath;
        private string _tempDictPath;

        public PasswordCrackerServiceTests()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            _service = new PasswordCrackerService();
        }


        private void CreateTestZip(string password)
        {
            _tempZipPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.zip");

            using (var zip = new ZipFile())
            {
                zip.Password = password;
                zip.AddEntry("test.txt", "tajna wiadomosc");
                zip.Save(_tempZipPath);
            }
        }

        [Fact]
        public async Task CrackPasswordAsync_Iterative_ShouldFindSimplePassword()
        {

            string password = "abc";
            CreateTestZip(password);

            var session = new CrackingSession
            {
                FilePath = _tempZipPath,
                Method = CrackingMethod.Iterative,

                MinLength = 3,
                MaxLength = 3,

                IncludeLowercase = true,
                IncludeUppercase = false,
                IncludeNumbers = false,
                IncludeSymbols = false
            };


            var result = await _service.CrackPasswordAsync(session, (log) => { }, CancellationToken.None);


            Assert.True(result.Success);
            Assert.Equal(password, result.FoundPassword);
        }

        [Fact]
        public async Task CrackPasswordAsync_Recursive_ShouldFindPassword()
        {

            string password = "x1";
            CreateTestZip(password);

            var session = new CrackingSession
            {
                FilePath = _tempZipPath,
                Method = CrackingMethod.Recursive,
                MinLength = 1,
                MaxLength = 2,
                IncludeLowercase = true,
                IncludeNumbers = true
            };


            var result = await _service.CrackPasswordAsync(session, (msg) => { }, CancellationToken.None);


            Assert.True(result.Success);
            Assert.Equal(password, result.FoundPassword);
        }

        [Fact]
        public async Task CrackPasswordAsync_ShouldFail_WhenPasswordOutOfRange()
        {

            CreateTestZip("longpassword");

            var session = new CrackingSession
            {
                FilePath = _tempZipPath,
                Method = CrackingMethod.Iterative,
                MinLength = 1,
                MaxLength = 3,
                IncludeLowercase = true
            };


            var result = await _service.CrackPasswordAsync(session, _ => { }, CancellationToken.None);


            Assert.False(result.Success);
            Assert.Null(result.FoundPassword);
        }

        [Fact]
        public async Task VerifySinglePassword_ShouldReturnTrue_ForCorrectPassword()
        {

            string pass = "test";
            CreateTestZip(pass);

            var session = new CrackingSession { FilePath = _tempZipPath };

            var result = await _service.VerifySinglePasswordAsync(pass, session, _ => { }, CancellationToken.None);


            Assert.True(result.Success);
            Assert.Equal(pass, result.FoundPassword);
        }

        public void Dispose()
        {
            if (File.Exists(_tempZipPath)) File.Delete(_tempZipPath);
            if (File.Exists(_tempDictPath)) File.Delete(_tempDictPath);
        }
    }
}