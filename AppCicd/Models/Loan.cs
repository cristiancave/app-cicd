namespace AppCicd.Models;

public enum LoanStatus
{
    Active,
    Returned,
    Overdue,
}

public class Loan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string BorrowerEmail { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
}
