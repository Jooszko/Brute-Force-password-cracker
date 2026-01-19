using Brute_Force_password_cracker.Models;
using Brute_Force_password_cracker.Common;
using Xunit;
using FluentAssertions;

namespace BruteForce.Tests.Models
{
    public class CrackingSessionTests
    {
        [Fact]
        public void DefaultConstructor_ShouldSetDefaultValues()
        {
            var session = new CrackingSession();

            session.MinLength.Should().Be(3);
            session.MaxLength.Should().Be(20);
            session.IncludeLowercase.Should().BeTrue();
            session.IncludeUppercase.Should().BeFalse();
            session.IncludeNumbers.Should().BeFalse();
            session.IncludeSymbols.Should().BeFalse();
            session.FilePath.Should().BeNull();
            session.RulePattern.Should().BeNull();
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            var session = new CrackingSession();

            session.FilePath = @"C:\test.zip";
            session.MinLength = 1;
            session.MaxLength = 10;
            session.IncludeLowercase = false;
            session.IncludeUppercase = true;
            session.IncludeNumbers = true;
            session.IncludeSymbols = true;
            session.RulePattern = "a|b|c";
            session.Method = CrackingMethod.Recursive;

            session.FilePath.Should().Be(@"C:\test.zip");
            session.MinLength.Should().Be(1);
            session.MaxLength.Should().Be(10);
            session.IncludeLowercase.Should().BeFalse();
            session.IncludeUppercase.Should().BeTrue();
            session.IncludeNumbers.Should().BeTrue();
            session.IncludeSymbols.Should().BeTrue();
            session.RulePattern.Should().Be("a|b|c");
            session.Method.Should().Be(CrackingMethod.Recursive);
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(5, 5)]
        [InlineData(3, 20)]
        public void MinMaxLength_ShouldAcceptValidRanges(int min, int max)
        {
            var session = new CrackingSession
            {
                MinLength = min,
                MaxLength = max
            };

            session.MinLength.Should().Be(min);
            session.MaxLength.Should().Be(max);
        }
    }
}
