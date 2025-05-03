using System;
using System.Windows;
using MoneyInterpret.Models;

namespace MoneyInterpret.Views
{
    public partial class SplitAmountWindow : Window
    {
        public decimal PrincipalAmount { get; private set; }
        public decimal InterestAmount { get; private set; }
        private Transaction _originalTransaction;

        public SplitAmountWindow(Transaction transaction)
        {
            InitializeComponent();
            _originalTransaction = transaction;
            
            // Initialize with default values
            TotalAmountTextBlock.Text = $"{transaction.Amount:C}";
            PrincipalTextBox.Text = "0.00";
            InterestTextBox.Text = $"{Math.Abs(transaction.Amount):F2}";
            
            // Set window title with transaction info
            Title = $"Split Transaction - {transaction.PostDate:MM/dd/yyyy} - {transaction.Description}";
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(PrincipalTextBox.Text, out decimal principalAmount) ||
                !decimal.TryParse(InterestTextBox.Text, out decimal interestAmount))
            {
                MessageBox.Show("Please enter valid decimal values for both fields.", 
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Ensure the sum matches the original amount
            decimal originalAmount = Math.Abs(_originalTransaction.Amount);
            if (Math.Abs(principalAmount + interestAmount - originalAmount) > 0.01m)
            {
                MessageBox.Show($"The sum of principal and interest must equal {originalAmount:C}.", 
                    "Invalid Split", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Set the values with correct signs (matching original transaction sign)
            decimal sign = Math.Sign(_originalTransaction.Amount);
            PrincipalAmount = -principalAmount * sign; // Negative for payments
            InterestAmount = -interestAmount * sign;   // Negative for payments
            
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
