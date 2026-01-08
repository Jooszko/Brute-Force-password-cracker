using System;
using System.Collections.Generic;
using Brute_Force_password_cracker.Common;

namespace Brute_Force_password_cracker.Models
{
    public class CrackingSession
    {
        public string FilePath { get; set; }
        public int MinLength { get; set; } = 3;
        public int MaxLength { get; set; } = 20;
        public string RulePattern { get; set; }
        public CrackingMethod Method { get; set; }
        public bool IncludeLowercase { get; set; } = true;
        public bool IncludeUppercase { get; set; } = false;
        public bool IncludeNumbers { get; set; } = false;
        public bool IncludeSymbols { get; set; } = false;
    }
}
