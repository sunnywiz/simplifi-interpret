using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MoneyInterpret.Models;

namespace MoneyInterpret.Services
{
    public class CsvImportService
    {
        public List<Transaction> ImportTransactions(string filePath, IEnumerable<Transaction> existingTransactions = null)
        {
            var transactions = new List<Transaction>();
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length <= 1)
                return transactions;

            var header = lines[0];
            
            // Determine file type based on header
            if (header.Contains("Debit") && header.Contains("Credit"))
            {
                // Standard bank account format
                transactions = ParseStandardBankFormat(lines);
            }
            else if (header.Contains("Account Number") && header.Contains("Post Date") && !header.Contains("Balance"))
            {
                // HELOC account format
                transactions = ParseHelocFormat(lines);
            }
            
            // Filter out duplicates if existing transactions were provided
            if (existingTransactions != null && existingTransactions.Any())
            {
                var existingSet = new HashSet<Transaction>(existingTransactions);
                transactions = transactions.Where(t => !existingSet.Contains(t)).ToList();
            }
            
            return transactions;
        }

        private List<Transaction> ParseStandardBankFormat(string[] lines)
        {
            var transactions = new List<Transaction>();
            
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                var fields = ParseCsvLine(line);
                
                if (fields.Length < 7)
                    continue;
                
                var transaction = new Transaction
                {
                    AccountNumber = fields[0].Trim('"'),
                    PostDate = DateTime.Parse(fields[1], CultureInfo.InvariantCulture),
                    CheckNum = fields[2],
                    Description = fields[3],
                    Status = fields[6],
                };
                
                // Parse Debit/Credit into a single Amount
                if (!string.IsNullOrWhiteSpace(fields[4]))
                {
                    transaction.Amount = -decimal.Parse(fields[4], CultureInfo.InvariantCulture); // Debit is negative
                }
                else if (!string.IsNullOrWhiteSpace(fields[5]))
                {
                    transaction.Amount = decimal.Parse(fields[5], CultureInfo.InvariantCulture); // Credit is positive
                }
                
                // Parse Balance if available
                if (fields.Length > 7 && !string.IsNullOrWhiteSpace(fields[7]))
                {
                    transaction.Balance = decimal.Parse(fields[7], CultureInfo.InvariantCulture);
                }
                
                transactions.Add(transaction);
            }
            
            return transactions;
        }

        private List<Transaction> ParseHelocFormat(string[] lines)
        {
            var transactions = new List<Transaction>();
            
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                var fields = ParseCsvLine(line);
                
                if (fields.Length < 6)
                    continue;
                
                var transaction = new Transaction
                {
                    AccountNumber = fields[0].Trim('"'),
                    PostDate = DateTime.Parse(fields[1], CultureInfo.InvariantCulture),
                    CheckNum = fields[2],
                    Description = fields[3],
                    Status = fields[5],
                };
                
                // Parse Debit/Credit into a single Amount
                if (!string.IsNullOrWhiteSpace(fields[4]))
                {
                    transaction.Amount = -decimal.Parse(fields[4], CultureInfo.InvariantCulture); // Debit is negative
                }
                else if (fields.Length > 5 && !string.IsNullOrWhiteSpace(fields[5]))
                {
                    transaction.Amount = decimal.Parse(fields[5], CultureInfo.InvariantCulture); // Credit is positive
                }
                
                transactions.Add(transaction);
            }
            
            return transactions;
        }

        private string[] ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            int startIndex = 0;
            
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(line.Substring(startIndex, i - startIndex));
                    startIndex = i + 1;
                }
            }
            
            // Add the last field
            result.Add(line.Substring(startIndex));
            
            return result.ToArray();
        }
    }
}
