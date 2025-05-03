using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MoneyInterpret.Models;

namespace MoneyInterpret.Views
{
    public partial class SplitTransactionWindow : Window
    {
        public ObservableCollection<Transaction> OriginalTransactions { get; set; } = new ObservableCollection<Transaction>();
        public ObservableCollection<Transaction> SplitTransactions { get; set; } = new ObservableCollection<Transaction>();
        public ObservableCollection<string> AvailableAccounts { get; set; } = new ObservableCollection<string>();
        
        private List<Transaction> _allTransactions;

        public SplitTransactionWindow(IEnumerable<Transaction> allTransactions)
        {
            InitializeComponent();
            DataContext = this;
            _allTransactions = allTransactions.ToList();
            
            // Populate available accounts
            var accounts = _allTransactions
                .Select(t => t.AccountNumber)
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .OrderBy(a => a)
                .ToList();
                
            foreach (var account in accounts)
            {
                AvailableAccounts.Add(account);
            }
        }

        private void AccountsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateOriginalTransactions();
        }

        private void UpdateOriginalTransactions()
        {
            OriginalTransactions.Clear();
            SplitTransactions.Clear();
            
            var selectedAccounts = AccountsListBox.SelectedItems.Cast<string>().ToList();
            if (selectedAccounts.Count == 0)
                return;
                
            var filteredTransactions = _allTransactions
                .Where(t => selectedAccounts.Contains(t.AccountNumber))
                .OrderByDescending(t => t.PostDate)
                .ToList();
                
            foreach (var transaction in filteredTransactions)
            {
                OriginalTransactions.Add(transaction);
            }
        }

        private void SplitButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTransaction = OriginalTransactionsGrid.SelectedItem as Transaction;
            if (selectedTransaction == null)
            {
                MessageBox.Show("Please select a transaction to split.", "No Transaction Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var splitWindow = new SplitAmountWindow(selectedTransaction);
            if (splitWindow.ShowDialog() == true)
            {
                // Create the split transactions
                var principalAmount = splitWindow.PrincipalAmount;
                var interestAmount = splitWindow.InterestAmount;
                
                // Create principal transaction (modify the original)
                var principalTransaction = new Transaction
                {
                    AccountNumber = selectedTransaction.AccountNumber,
                    PostDate = selectedTransaction.PostDate,
                    CheckNum = selectedTransaction.CheckNum,
                    Description = $"Extra Principal: ${Math.Abs(principalAmount):F2}",
                    Amount = principalAmount,
                    Status = selectedTransaction.Status,
                    Balance = selectedTransaction.Balance
                };
                
                // Create interest transaction
                var interestTransaction = new Transaction
                {
                    AccountNumber = selectedTransaction.AccountNumber,
                    PostDate = selectedTransaction.PostDate,
                    CheckNum = selectedTransaction.CheckNum,
                    Description = $"SPLIT Interest Paid: ${Math.Abs(interestAmount):F2}",
                    Amount = interestAmount,
                    Status = selectedTransaction.Status,
                    Balance = null // Balance would be different for this split transaction
                };
                
                // Add to split transactions
                SplitTransactions.Add(principalTransaction);
                SplitTransactions.Add(interestTransaction);
                
                // Remove the original from the display
                OriginalTransactions.Remove(selectedTransaction);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SplitTransactions.Count == 0)
            {
                MessageBox.Show("No transactions have been split.", "No Changes", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
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
