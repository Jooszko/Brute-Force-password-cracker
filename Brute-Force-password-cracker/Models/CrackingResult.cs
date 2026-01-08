using System;

namespace Brute_Force_password_cracker.Models
{
    public class CrackingResult
    {
        public bool Success { get; set; }
        public string FoundPassword { get; set; }
        public int AttemptsCount { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string FilePath { get; set; }
        public string ErrorMessage { get; set; }
    }
}
