using AppCicd.Models;
using Microsoft.OpenApi.Models;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Library Management API",
        Version = "v1",
        Description = "Library Management API - Book lending system",
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Library Management API v1"));

app.UseHttpMetrics();
app.MapMetrics();

var books = new List<Book>();
var loans = new List<Loan>();
int nextBookId = 1;
int nextLoanId = 1;

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithTags("Health");

// ── Books ──────────────────────────────────────────────────────────────────

app.MapGet("/api/books", () => Results.Ok(books))
    .WithTags("Books");

app.MapGet("/api/books/available", () =>
    Results.Ok(books.Where(b => b.AvailableCopies > 0).ToList()))
    .WithTags("Books");

app.MapGet("/api/books/search", (string query) =>
    Results.Ok(books.Where(b =>
        b.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        b.Author.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList()))
    .WithTags("Books");

app.MapGet("/api/books/genre/{genre}", (Genre genre) =>
    Results.Ok(books.Where(b => b.Genre == genre).ToList()))
    .WithTags("Books");

app.MapGet("/api/books/{id}", (int id) =>
{
    var book = books.FirstOrDefault(b => b.Id == id);
    return book is null ? Results.NotFound() : Results.Ok(book);
}).WithTags("Books");

app.MapPost("/api/books", (Book book) =>
{
    book.Id = nextBookId++;
    book.CreatedAt = DateTime.UtcNow;
    books.Add(book);
    return Results.Created($"/api/books/{book.Id}", book);
}).WithTags("Books");

app.MapPut("/api/books/{id}", (int id, Book updated) =>
{
    var book = books.FirstOrDefault(b => b.Id == id);
    if (book is null)
        return Results.NotFound();

    book.Title = updated.Title;
    book.Author = updated.Author;
    book.Isbn = updated.Isbn;
    book.Genre = updated.Genre;
    book.TotalCopies = updated.TotalCopies;
    book.AvailableCopies = updated.AvailableCopies;
    book.PublishedYear = updated.PublishedYear;
    return Results.Ok(book);
}).WithTags("Books");

app.MapDelete("/api/books/{id}", (int id) =>
{
    var book = books.FirstOrDefault(b => b.Id == id);
    if (book is null)
        return Results.NotFound();

    books.Remove(book);
    return Results.NoContent();
}).WithTags("Books");

// ── Loans ──────────────────────────────────────────────────────────────────

app.MapGet("/api/loans", () => Results.Ok(loans))
    .WithTags("Loans");

app.MapGet("/api/loans/active", () =>
    Results.Ok(loans.Where(l => l.Status == LoanStatus.Active).ToList()))
    .WithTags("Loans");

app.MapGet("/api/loans/overdue", () =>
{
    var now = DateTime.UtcNow;
    var overdue = loans.Where(l => l.Status == LoanStatus.Active && l.DueDate < now).ToList();
    foreach (var loan in overdue)
        loan.Status = LoanStatus.Overdue;
    return Results.Ok(overdue);
}).WithTags("Loans");

app.MapPost("/api/loans", (Loan loan) =>
{
    var book = books.FirstOrDefault(b => b.Id == loan.BookId);
    if (book is null)
        return Results.NotFound(new { error = "Book not found." });
    if (book.AvailableCopies <= 0)
        return Results.BadRequest(new { error = "No available copies." });

    book.AvailableCopies--;
    loan.Id = nextLoanId++;
    loan.LoanDate = DateTime.UtcNow;
    loan.Status = LoanStatus.Active;
    loans.Add(loan);
    return Results.Created($"/api/loans/{loan.Id}", loan);
}).WithTags("Loans");

app.MapPost("/api/loans/{id}/return", (int id) =>
{
    var loan = loans.FirstOrDefault(l => l.Id == id);
    if (loan is null)
        return Results.NotFound();
    if (loan.Status == LoanStatus.Returned)
        return Results.BadRequest(new { error = "Loan already returned." });

    var book = books.FirstOrDefault(b => b.Id == loan.BookId);
    if (book is not null)
        book.AvailableCopies++;

    loan.ReturnDate = DateTime.UtcNow;
    loan.Status = LoanStatus.Returned;
    return Results.Ok(loan);
}).WithTags("Loans");

// ── Stats ──────────────────────────────────────────────────────────────────

app.MapGet("/api/stats", () =>
{
    var topBooks = loans
        .GroupBy(l => l.BookId)
        .Select(g => new
        {
            BookId = g.Key,
            Title = books.FirstOrDefault(b => b.Id == g.Key)?.Title ?? "Unknown",
            LoanCount = g.Count(),
        })
        .OrderByDescending(x => x.LoanCount)
        .Take(5)
        .ToList();

    return Results.Ok(new
    {
        TotalBooks = books.Count,
        TotalLoans = loans.Count,
        ActiveLoans = loans.Count(l => l.Status == LoanStatus.Active),
        MostBorrowedBooks = topBooks,
    });
}).WithTags("Stats");

app.Run();

public partial class Program { }
