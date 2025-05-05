using System;
using System.Collections.Generic;
using System.Linq;
using MoneyInterpret.Models;

namespace MoneyInterpret.Services
{
    public class MatchAdvancesService
    {
        public int MatchAdvances(IList<Transaction> transactions)
        {
            int matchCount = 0;
            
            // Find all principal advance transactions
            var advanceTransactions = transactions
                .Where(t => t.Description != null && t.Description.Contains("Principal Advance:"))
                .ToList();
            
            foreach (var advanceTransaction in advanceTransactions)
            {
                // The amount is already available in the transaction
                decimal advanceAmount = Math.Abs(advanceTransaction.Amount);
                
                // Look for matching transaction on the same day in a different account
                // with the opposite amount (if advance is positive, look for negative and vice versa)
                var matchingTransaction = transactions.FirstOrDefault(t => 
                    t.PostDate.Date == advanceTransaction.PostDate.Date &&
                    t.AccountNumber != advanceTransaction.AccountNumber &&
                    Math.Abs(t.Amount) == advanceAmount &&
                    Math.Sign(t.Amount) != Math.Sign(advanceTransaction.Amount));
                
                if (matchingTransaction != null)
                {
                    // Clean up the description by removing quotes and trimming spaces
                    string cleanLabel = matchingTransaction.Description.Replace("\"", "").Trim();
                    
                    // Set the label on both transactions
                    advanceTransaction.Label = cleanLabel;
                    matchingTransaction.Label = "Advance: " + cleanLabel;
                    
                    matchCount++;
                }
                else
                {
                    advanceTransaction.Label = "Unknown Advance";
                }
            }
            
            return matchCount;
        }
    }
}
