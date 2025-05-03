using System;

namespace MoneyInterpret.Models
{
    public class Transaction : IEquatable<Transaction>
    {
        public string AccountNumber { get; set; }
        public DateTime PostDate { get; set; }
        public string CheckNum { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public decimal? Balance { get; set; }

        public bool Equals(Transaction other)
        {
            if (other == null)
                return false;

            return AccountNumber == other.AccountNumber &&
                   PostDate == other.PostDate &&
                   CheckNum == other.CheckNum &&
                   Description == other.Description &&
                   Amount == other.Amount &&
                   Status == other.Status &&
                   Balance == other.Balance;
        }

        public override bool Equals(object obj)
        {
            if (obj is Transaction transaction)
                return Equals(transaction);
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (AccountNumber?.GetHashCode() ?? 0);
                hash = hash * 23 + PostDate.GetHashCode();
                hash = hash * 23 + (CheckNum?.GetHashCode() ?? 0);
                hash = hash * 23 + (Description?.GetHashCode() ?? 0);
                hash = hash * 23 + Amount.GetHashCode();
                hash = hash * 23 + (Status?.GetHashCode() ?? 0);
                hash = hash * 23 + (Balance?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static bool operator ==(Transaction left, Transaction right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(Transaction left, Transaction right)
        {
            return !(left == right);
        }
    }
}
