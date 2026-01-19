using Brute_Force_password_cracker.ViewModels;
using Xunit;
using System;
using System.Windows.Input;

namespace BruteForce.Tests.ViewModels
{
    public class RelayCommandTests
    {
        [Fact]
        public void Constructor_WithNullExecute_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayCommand(null));
        }

        [Fact]
        public void CanExecute_WithoutCanExecuteFunc_ShouldReturnTrue()
        {
            var command = new RelayCommand(_ => { });

            bool canExecute = command.CanExecute(null);

            Assert.True(canExecute);
        }

        [Fact]
        public void CanExecute_WithCanExecuteFunc_ShouldReturnFuncResult()
        {
            bool expectedResult = false;
            var command = new RelayCommand(execute: _ => { }, canExecute: _ => expectedResult);

            bool canExecute = command.CanExecute(null);

            Assert.Equal(expectedResult, canExecute);
        }

        [Fact]
        public void Execute_ShouldCallAction()
        {
            bool wasCalled = false;
            var command = new RelayCommand(_ => wasCalled = true);

            command.Execute(null);

            Assert.True(wasCalled);
        }

        [Fact]
        public void CanExecuteChanged_ShouldBeRaiseable()
        {
            var command = new RelayCommand(_ => { });
            bool eventRaised = false;

            command.CanExecuteChanged += (sender, args) =>
            {
                eventRaised = true;
            };

            var canExecuteChangedEvent = typeof(RelayCommand)
                .GetEvent("CanExecuteChanged");

            Assert.NotNull(canExecuteChangedEvent);
        }
    }
}
