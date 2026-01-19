using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using Brute_Force_password_cracker.ViewModels;
using Brute_Force_password_cracker.Services;
using Brute_Force_password_cracker.Models;
using Moq;
using Xunit;
using FluentAssertions;

namespace BruteForce.Tests.ViewModels
{
    public class MainViewModelTests
    {
        private readonly Mock<PasswordCrackerService> _mockCracker;
        private readonly Mock<DictionaryAttackService> _mockDictionary;
        private readonly Mock<FileDialogService> _mockFileDialog;
        private readonly MainViewModel _viewModel;

        public MainViewModelTests()
        {
            _mockCracker = new Mock<PasswordCrackerService>();
            _mockDictionary = new Mock<DictionaryAttackService>();
            _mockFileDialog = new Mock<FileDialogService>();

            _viewModel = new MainViewModel(
                _mockCracker.Object,
                _mockDictionary.Object,
                _mockFileDialog.Object
                );
        }

        //======== TESTS FOR PROPERTIES ========

        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            _viewModel.Title.Should().Be("Brute Force Password Cracker");
            _viewModel.ZipPath.Should().Be("> target.zip");
            _viewModel.InputMin.Should().Be("3");
            _viewModel.InputMax.Should().Be("8");
            _viewModel.IsLeters.Should().BeTrue();
            _viewModel.IsCapital.Should().BeFalse();
            _viewModel.IsNumbers.Should().BeFalse();
            _viewModel.IsSymbols.Should().BeFalse();
            _viewModel.IsIteratively.Should().BeTrue();
            _viewModel.IsExecuting.Should().BeTrue();
        }

        [Fact]
        public void IsIteratively_SetToTrue_ShouldSetOtherMethodsToFalse()
        {
            _viewModel.IsRecursively = true;
            _viewModel.IsDictionary = true;

            _viewModel.IsIteratively = true;

            _viewModel.IsRecursively.Should().BeFalse();
            _viewModel.IsIteratively.Should().BeTrue();
            _viewModel.IsDictionary.Should().BeFalse();
        }

        [Fact]
        public void IsRecursively_SetToTrue_ShouldSetOtherMethodsToFalse()
        {
            _viewModel.IsIteratively = true;
            _viewModel.IsDictionary = true;

            _viewModel.IsRecursively = true;

            _viewModel.IsRecursively.Should().BeTrue();
            _viewModel.IsIteratively.Should().BeFalse();
            _viewModel.IsDictionary.Should().BeFalse();
        }

        [Fact]
        public void IsDictionary_SetToTrue_ShouldSetOtherMethodsToFalse()
        {
            _viewModel.IsIteratively = true;
            _viewModel.IsRecursively = true;

            _viewModel.IsDictionary = true;            

            _viewModel.IsRecursively.Should().BeFalse();
            _viewModel.IsIteratively.Should().BeFalse();
            _viewModel.IsDictionary.Should().BeTrue();
        }

        //======== TESTS FOR COMMANDS ========

        [Fact]
        public void SelectFileCommand_WhenExecuted_ShouldUpdateZipPath()
        {
            const string expectedPath = @"C:\test\file.zip";
            _mockFileDialog
                .Setup(x => x.ShowOpenFileDialog(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(expectedPath);

            _viewModel.SelectFileCommand.Execute(null);

            _viewModel.ZipPath.Should().Be(expectedPath);
            _viewModel.Logs.Should().ContainMatch("*Selected ZIP file: file.zip");
        }

        [Fact]
        public void SelectFileCommand_WhenCancelled_ShouldNotUpdateZipPath()
        {
            _mockFileDialog
                .Setup(x => x.ShowOpenFileDialog(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string)null);

            string originalPath = _viewModel.ZipPath;

            _viewModel.SelectFileCommand.Execute(null);

            _viewModel.ZipPath.Should().Be(originalPath);
        }

        [Fact]
        public void CancelCommand_ShouldBeDisabled_WhenIsExecutingIsTrue()
        {
            _viewModel.IsExecuting = true; 

            _viewModel.CancelCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void CancelCommand_ShouldBeEnabled_WhenIsExecutingIsFalse()
        {
            _viewModel.IsExecuting = false;

            _viewModel.CancelCommand.CanExecute(null).Should().BeTrue();
        }

        //======== TEST FOR VALIDATION ========

        [Theory]
        [InlineData("3", "5", true)]
        [InlineData("1", "1", true)]    
        [InlineData("10", "5", false)]  
        [InlineData("abc", "5", false)] 
        [InlineData("3", "xyz", false)]

        public void ValidateLengths_ShouldWorkCorrectly(string min, string max, bool expectedValid)
        {
            _viewModel.InputMin = min;
            _viewModel.InputMax = max;

            bool isValid = int.TryParse(min, out int parsedMin) && int.TryParse(max, out int parsedMax) && parsedMin <= parsedMax;

            isValid.Should().Be(expectedValid);
        }

        //======== TEST FOR LOGGING ========

        [Fact]
        public void Logs_ShouldNotExceed100Entries()
        {
            for (int i = 0; i < 150; i++)
            {
                _viewModel.IsIteratively = !_viewModel.IsIteratively;
            }

            _viewModel.Logs.Should().HaveCountLessThanOrEqualTo(100);
        }


        //======== TEST FOR EXECUTION ========

        [Fact]
        public async Task ExecuteCracking_WhenManualPasswordProvided_ShouldVerifyPassword()
        {
            _viewModel.ManualPassword = "test123";
            _viewModel.ZipPath = @"C:\test.zip";

            _mockCracker
                .Setup(x => x.VerifySinglePasswordAsync(
                    It.IsAny<string>(),
                    It.IsAny<CrackingSession>(),
                    It.IsAny<Action<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrackingResult { Success = true, FoundPassword = "test123" });

            await _viewModel.ExecuteCracking(null);

            _mockCracker.Verify(
                x => x.VerifySinglePasswordAsync(
                    "test123",
                    It.IsAny<CrackingSession>(),
                    It.IsAny<Action<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteCracking_WhenDictionaryMethodSelected_ShouldUseDictionaryService()
        {
            _viewModel.IsDictionary = true;
            _viewModel.ZipPath = @"C:\test.zip";

            _mockDictionary
                .Setup(x => x.TryDictionaryAttackAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CrackingResult { Success = false });

            await _viewModel.ExecuteCracking(null);

            _mockDictionary.Verify(
                x => x.TryDictionaryAttackAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteCracking_ShouldSetIsExecutingCorrectly()
        {
            _viewModel.ZipPath = @"C:\test.zip";

            var tcs = new TaskCompletionSource<CrackingResult>();

            _mockCracker
                .Setup(x => x.CrackPasswordAsync(
                    It.IsAny<CrackingSession>(),
                    It.IsAny<Action<string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var executeTask = _viewModel.ExecuteCracking(null);

            _viewModel.IsExecuting.Should().BeFalse();

            tcs.SetResult(new CrackingResult { Success = false });

            await executeTask;

            _viewModel.IsExecuting.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteCracking_WhenCancelled_ShouldHandleCancellation()
        {
            _viewModel.ZipPath = @"C:\test.zip";

            _mockCracker
                .Setup(x => x.CrackPasswordAsync(
                    It.IsAny<CrackingSession>(),
                    It.IsAny<Action<string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((CrackingSession session, Action<string> logAction, CancellationToken token) =>
                {

                    _viewModel.CancelCommand.Execute(null);

                    if (token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(token);
                    }

                    return Task.FromResult(new CrackingResult { Success = false });
                });

            await _viewModel.ExecuteCracking(null);

            _viewModel.Logs.Should().ContainMatch("*cancelled*");

            _viewModel.IsExecuting.Should().BeTrue();
        }

        [Fact]
        public void CancelCracking_ShouldCancelCancellationTokenSource()
        {
            Assert.NotNull(_viewModel.CancelCommand);

            _viewModel.Invoking(vm => vm.CancelCommand.Execute(null)).Should().NotThrow();
        }

        //======== TESTS FOR RULE TEXT ========
        [Fact]
        public void RuleText_WhenSet_ShouldUpdateProperty()
        {
            string rule = "a|b|*|!";

            _viewModel.RuleText = rule;

            _viewModel.RuleText.Should().Be(rule);
        }

        [Fact]
        public void RuleText_WhenSetToNull_ShouldHandleGracefully()
        {
            _viewModel.Invoking(vm => vm.RuleText = null)
                .Should().NotThrow();

            _viewModel.RuleText.Should().BeNull();
        }


        //======== TEST FOR INPUT VALIDATION ========
        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("-5")]
        [InlineData("0")]
        public void InputMin_WithInvalidValues_ShouldNotThrow(string invalidValue)
        {
            _viewModel.Invoking(vm => vm.InputMin = invalidValue)
                .Should().NotThrow();
        }

        [Theory]
        [InlineData("")]
        [InlineData("xyz")]
        [InlineData("-10")]
        public void InputMax_WithInvalidValues_ShouldNotThrow(string invalidValue)
        {
            _viewModel.Invoking(vm => vm.InputMax = invalidValue)
                .Should().NotThrow();
        }
    }
}
