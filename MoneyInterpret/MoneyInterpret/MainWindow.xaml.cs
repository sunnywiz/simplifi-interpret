using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using MoneyInterpret.Converters;
using MoneyInterpret.Models;
using MoneyInterpret.Services;
using MoneyInterpret.ViewModels;
using MoneyInterpret.Views;
using System.Collections.Generic;
using DataGridExtensions; // Add this using statement

namespace MoneyInterpret
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly CsvImportService _importService;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            _importService = new CsvImportService();
            
            // No need to set IsAutoFilterEnabled in code since we're setting it in XAML
            // The XAML attribute dgx:DataGridFilter.IsAutoFilterEnabled="True" handles this
        }

        private void ImportFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Multiselect = true,
                Title = "Select CSV Files to Import"
            };

            if (dialog.ShowDialog() == true)
            {
                var allTransactions = new List<Transaction>(_viewModel.Transactions);
                int initialCount = allTransactions.Count;
                int duplicatesSkipped = 0;
                
                foreach (var fileName in dialog.FileNames)
                {
                    try
                    {
                        // Pass existing transactions to avoid duplicates
                        var newTransactions = _importService.ImportTransactions(fileName, allTransactions);
                        
                        // Track how many duplicates were skipped
                        var potentialTransactions = _importService.ImportTransactions(fileName);
                        duplicatesSkipped += potentialTransactions.Count - newTransactions.Count;
                        
                        allTransactions.AddRange(newTransactions);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing file {Path.GetFileName(fileName)}: {ex.Message}", 
                            "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                
                // Sort by date descending
                var sorted = allTransactions.OrderByDescending(t => t.PostDate).ToList();
                _viewModel.Transactions = new ObservableCollection<Transaction>(sorted);
                
                int newTransactionsAdded = _viewModel.Transactions.Count - initialCount;
                
                MessageBox.Show($"Import complete:\n" +
                                $"- {newTransactionsAdded} new transactions added\n" +
                                $"- {duplicatesSkipped} duplicates skipped\n" +
                                $"- {_viewModel.Transactions.Count} total transactions", 
                    "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void SplitInterestPrincipal_Click(object sender, RoutedEventArgs e)
        {
            // If called from context menu, get the selected transaction
            Transaction selectedTransaction = TransactionsGrid.SelectedItem as Transaction;
            
            // Open the split window
            var splitWindow = new SplitTransactionWindow(_viewModel.Transactions);
            splitWindow.Owner = this;
            
            if (splitWindow.ShowDialog() == true)
            {
                // Get the split transactions
                var splitTransactions = splitWindow.SplitTransactions.ToList();
                if (splitTransactions.Count == 0)
                    return;
                
                // Create a new list with all transactions
                var allTransactions = _viewModel.Transactions.ToList();
                
                // Remove original transactions that were split
                var accountNumbers = splitTransactions.Select(t => t.AccountNumber).Distinct().ToList();
                var postDates = splitTransactions.Select(t => t.PostDate).Distinct().ToList();
                
                // Find the original transactions that were split (by matching date and amount)
                var originalTransactionsToRemove = new List<Transaction>();
                
                foreach (var splitGroup in splitTransactions.GroupBy(t => new { t.PostDate, t.AccountNumber }))
                {
                    // Get the total amount of this split group
                    decimal totalAmount = splitGroup.Sum(t => t.Amount);
                    
                    // Find the original transaction with matching date, account, and amount
                    var originalTransaction = allTransactions.FirstOrDefault(t => 
                        t.PostDate == splitGroup.Key.PostDate && 
                        t.AccountNumber == splitGroup.Key.AccountNumber && 
                        Math.Abs(t.Amount - totalAmount) < 0.01m);
                    
                    if (originalTransaction != null)
                    {
                        originalTransactionsToRemove.Add(originalTransaction);
                    }
                }
                
                // Remove the original transactions
                foreach (var transaction in originalTransactionsToRemove)
                {
                    allTransactions.Remove(transaction);
                }
                
                // Add the split transactions
                allTransactions.AddRange(splitTransactions);
                
                // Sort by date descending
                var sorted = allTransactions.OrderByDescending(t => t.PostDate).ToList();
                _viewModel.Transactions = new ObservableCollection<Transaction>(sorted);
                
                MessageBox.Show($"Successfully split {originalTransactionsToRemove.Count} transactions into {splitTransactions.Count} transactions.", 
                    "Split Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
