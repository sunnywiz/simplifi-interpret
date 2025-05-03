namespace MoneyInterpret.Models;

public class Transaction
{
    public string AccountNumber { get; set; }
    public DateTime PostDate { get; set; }
    public string CheckNum { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public decimal? Balance { get; set; }
}