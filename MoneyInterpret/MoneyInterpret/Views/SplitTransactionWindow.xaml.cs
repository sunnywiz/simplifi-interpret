using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MoneyInterpret.Models;

namespace MoneyInterpret.Views
{
    public partial class SplitTransactionWindow : Window
    {
        public ObservableCollection<string> AvailableAccounts { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<Transaction> OriginalTransactions { get; set; } = new ObservableCollection<Transaction>();
        public ObservableCollection<Transaction> SplitTransactions { get; set; } = new ObservableCollection<Transaction>();
        
        private List<Transaction> _allTransactions;
        private List<Regex> _splittablePatterns;

        public SplitTransactionWindow(List<Transaction> transactions)
        {
            InitializeComponent();
            DataContext = this;
            
            _allTransactions = transactions;
            
            // Initialize regex patterns for matching splittable transactions
            _splittablePatterns = new List<Regex>
            {
                new Regex(@"Interest:\s*\$?(\d+\.\d+);\s*Extra\s*Principal:\s*\$?(\d+\.\d+)", RegexOptions.IgnoreCase),
                // Add more patterns as needed
            };
            
            // Populate available accounts
            var accounts = transactions
                .Select(t => t.AccountNumber)
                .Distinct()
                .OrderBy(a => a)
                .ToList();
            
            foreach (var account in accounts)
            {
                AvailableAccounts.Add(account);
            }
        }

        private void AccountsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OriginalTransactions.Clear();
            SplitTransactions.Clear();
            
            if (AccountsListBox.SelectedItem == null)
                return;
                
            string selectedAccount = AccountsListBox.SelectedItem.ToString();
            
            // Filter transactions by the selected account
            var accountTransactions = _allTransactions
                .Where(t => t.AccountNumber == selectedAccount)
                .ToList();
                
            // Find splittable transactions
            foreach (var transaction in accountTransactions)
            {
                if (IsSplittableTransaction(transaction))
                {
                    OriginalTransactions.Add(transaction);
                }
            }
            
            // Automatically split transactions
            if (OriginalTransactions.Count > 0)
            {
                foreach (var transaction in OriginalTransactions)
                {
                    var splitResult = SplitTransaction(transaction);
                    foreach (var splitTrans in splitResult)
                    {
                        SplitTransactions.Add(splitTrans);
                    }
                }
            }
        }
        
        private bool IsSplittableTransaction(Transaction transaction)
        {
            if (string.IsNullOrEmpty(transaction.Description))
                return false;
        
            // Skip transactions that have already been split
            if (transaction.Description.StartsWith("WHOLE:") || transaction.Description.StartsWith("SPLIT:"))
                return false;
        
            foreach (var pattern in _splittablePatterns)
            {
                if (pattern.IsMatch(transaction.Description))
                    return true;
            }
    
            return false;
        }

        
        private List<Transaction> SplitTransaction(Transaction original)
        {
            var result = new List<Transaction>();
            
            // Create a copy of the original transaction for the whole amount
            var wholeTransaction = new Transaction
            {
                AccountNumber = original.AccountNumber,
                PostDate = original.PostDate,
                CheckNum = original.CheckNum,
                Description = "WHOLE: " + original.Description,
                Amount = original.Amount,
                Status = original.Status,
                Balance = original.Balance
            };
            
            // Extract interest amount using regex
            foreach (var pattern in _splittablePatterns)
            {
                var match = pattern.Match(original.Description);
                if (match.Success && match.Groups.Count >= 3)
                {
                    if (decimal.TryParse(match.Groups[1].Value, out decimal interestAmount))
                    {
                        // Create interest transaction (negative to back out the interest)
                        var interestTransaction = new Transaction
                        {
                            AccountNumber = original.AccountNumber,
                            PostDate = original.PostDate,
                            CheckNum = original.CheckNum,
                            Description = "SPLIT: Interest",
                            Amount = -interestAmount,  // Negative amount to back out the interest
                            Status = original.Status
                        };
                        
                        result.Add(wholeTransaction);
                        result.Add(interestTransaction);
                        return result;
                    }
                }
            }
            
            // If we couldn't split it, just return the original
            result.Add(original);
            return result;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Return the split transactions
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
