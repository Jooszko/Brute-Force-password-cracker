using Brute_Force_password_cracker.Models;
using Xunit;
using FluentAssertions;
using System;

namespace BruteForce.Tests.Models
{
    public class CrackingResultTests
    {
        [Fact]
        public void DefaultConstructor_ShouldSetTimestampToNow()
        {
            var result = new CrackingResult();

            result.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Success.Should().BeFalse();
            result.FoundPassword.Should().BeNull();
            result.AttemptsCount.Should().Be(0);
            result.Duration.Should().Be(TimeSpan.Zero);
            result.FilePath.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            var result = new CrackingResult();
            var duration = TimeSpan.FromMinutes(5.5);

            result.Success = true;
            result.FoundPassword = "password123";
            result.AttemptsCount = 1000;
            result.Duration = duration;
            result.FilePath = @"C:\test.zip";
            result.ErrorMessage = "Test error";

            result.Success.Should().BeTrue();
            result.FoundPassword.Should().Be("password123");
            result.AttemptsCount.Should().Be(1000);
            result.Duration.Should().Be(duration);
            result.FilePath.Should().Be(@"C:\test.zip");
            result.ErrorMessage.Should().Be("Test error");
        }

        [Fact]
        public void Duration_CanBeTimeSpan()
        {
            var result = new CrackingResult();
            var expectedDuration = new TimeSpan(1, 2, 3, 4, 567); 

            result.Duration = expectedDuration;

            result.Duration.Should().Be(expectedDuration);
            result.Duration.TotalMilliseconds.Should().Be(93784567);
        }
    }
}
