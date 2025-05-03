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
    }
}
