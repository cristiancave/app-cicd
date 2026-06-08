namespace AppCicd.Models;

public enum Genre
{
    Fiction,
    NonFiction,
    Science,
    Technology,
    History,
    Art,
    Philosophy,
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public Genre Genre { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int PublishedYear { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
