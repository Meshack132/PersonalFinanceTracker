namespace PersonalFinanceTracker.Models;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Uncategorized";
    public TransactionType Type { get; set; }

    /// <summary>
    /// Returns the signed value of the transaction:
    /// positive for income, negative for expense.
    /// </summary>
    public decimal SignedAmount => Type == TransactionType.Income ? Amount : -Amount;

    public override string ToString()
    {
        var sign = Type == TransactionType.Income ? "+" : "-";
        return $"{Date:yyyy-MM-dd} | {sign}{Amount,10:C} | {Category,-15} | {Description}";
    }
}
