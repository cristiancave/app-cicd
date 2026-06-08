using System.Net;
using System.Net.Http.Json;
using AppCicd.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AppCicd.Tests;

public class InitialStateTests
{
    [Fact]
    public async Task GetBooks_ReturnsEmptyListInitially()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/books");
        response.EnsureSuccessStatusCode();
        var books = await response.Content.ReadFromJsonAsync<List<Book>>();
        Assert.NotNull(books);
        Assert.Empty(books);
    }
}

public class LibraryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LibraryApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── Health ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Books ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostBook_CreatesBookAndReturns201()
    {
        var book = new Book
        {
            Title = "Clean Code",
            Author = "Robert Martin",
            Isbn = "978-0132350884",
            Genre = Genre.Technology,
            TotalCopies = 3,
            AvailableCopies = 3,
            PublishedYear = 2008,
        };

        var response = await _client.PostAsJsonAsync("/api/books", book);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(created);
        Assert.Equal("Clean Code", created.Title);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task GetBookById_ReturnsCreatedBook()
    {
        var book = new Book
        {
            Title = "The Pragmatic Programmer",
            Author = "David Thomas",
            Isbn = "978-0135957059",
            Genre = Genre.Technology,
            TotalCopies = 2,
            AvailableCopies = 2,
            PublishedYear = 2019,
        };
        var postResponse = await _client.PostAsJsonAsync("/api/books", book);
        var created = await postResponse.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task GetBookById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/books/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutBook_UpdatesBook()
    {
        var book = new Book
        {
            Title = "Original Title",
            Author = "Author A",
            Isbn = "111",
            Genre = Genre.Fiction,
            TotalCopies = 1,
            AvailableCopies = 1,
            PublishedYear = 2000,
        };
        var postResponse = await _client.PostAsJsonAsync("/api/books", book);
        var created = await postResponse.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(created);

        created.Title = "Updated Title";
        var putResponse = await _client.PutAsJsonAsync($"/api/books/{created.Id}", created);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
    }

    [Fact]
    public async Task DeleteBook_RemovesBook()
    {
        var book = new Book
        {
            Title = "To Delete",
            Author = "Author B",
            Isbn = "222",
            Genre = Genre.History,
            TotalCopies = 1,
            AvailableCopies = 1,
            PublishedYear = 2010,
        };
        var postResponse = await _client.PostAsJsonAsync("/api/books", book);
        var created = await postResponse.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAvailableBooks_FiltersCorrectly()
    {
        var available = new Book
        {
            Title = "Available Book",
            Author = "Author C",
            Isbn = "333",
            Genre = Genre.Science,
            TotalCopies = 2,
            AvailableCopies = 2,
            PublishedYear = 2015,
        };
        var unavailable = new Book
        {
            Title = "Unavailable Book",
            Author = "Author D",
            Isbn = "444",
            Genre = Genre.Science,
            TotalCopies = 1,
            AvailableCopies = 0,
            PublishedYear = 2015,
        };
        await _client.PostAsJsonAsync("/api/books", available);
        await _client.PostAsJsonAsync("/api/books", unavailable);

        var response = await _client.GetAsync("/api/books/available");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<Book>>();
        Assert.NotNull(result);
        Assert.All(result, b => Assert.True(b.AvailableCopies > 0));
    }

    [Fact]
    public async Task GetBooksByGenre_FiltersCorrectly()
    {
        var book = new Book
        {
            Title = "A Philosophy Book",
            Author = "Plato",
            Isbn = "555",
            Genre = Genre.Philosophy,
            TotalCopies = 1,
            AvailableCopies = 1,
            PublishedYear = 380,
        };
        await _client.PostAsJsonAsync("/api/books", book);

        var response = await _client.GetAsync("/api/books/genre/Philosophy");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<Book>>();
        Assert.NotNull(result);
        Assert.All(result, b => Assert.Equal(Genre.Philosophy, b.Genre));
    }

    [Fact]
    public async Task SearchBooks_FindsByTitle()
    {
        var book = new Book
        {
            Title = "Unique Search Title XYZ",
            Author = "Search Author",
            Isbn = "666",
            Genre = Genre.Art,
            TotalCopies = 1,
            AvailableCopies = 1,
            PublishedYear = 2020,
        };
        await _client.PostAsJsonAsync("/api/books", book);

        var response = await _client.GetAsync("/api/books/search?query=Unique+Search+Title+XYZ");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<Book>>();
        Assert.NotNull(result);
        Assert.Contains(result, b => b.Title.Contains("Unique Search Title XYZ", StringComparison.OrdinalIgnoreCase));
    }

    // ── Loans ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostLoan_CreatesLoanAndReducesAvailableCopies()
    {
        var book = new Book
        {
            Title = "Loan Test Book",
            Author = "Loan Author",
            Isbn = "777",
            Genre = Genre.NonFiction,
            TotalCopies = 2,
            AvailableCopies = 2,
            PublishedYear = 2021,
        };
        var postBook = await _client.PostAsJsonAsync("/api/books", book);
        var createdBook = await postBook.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(createdBook);

        var loan = new Loan
        {
            BookId = createdBook.Id,
            BorrowerName = "Alice",
            BorrowerEmail = "alice@example.com",
            DueDate = DateTime.UtcNow.AddDays(14),
        };

        var loanResponse = await _client.PostAsJsonAsync("/api/loans", loan);
        Assert.Equal(HttpStatusCode.Created, loanResponse.StatusCode);

        var getBook = await _client.GetAsync($"/api/books/{createdBook.Id}");
        var updatedBook = await getBook.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(updatedBook);
        Assert.Equal(1, updatedBook.AvailableCopies);
    }

    [Fact]
    public async Task PostLoan_FailsWhenNoCopiesAvailable()
    {
        var book = new Book
        {
            Title = "No Copies Book",
            Author = "Author Zero",
            Isbn = "888",
            Genre = Genre.Fiction,
            TotalCopies = 1,
            AvailableCopies = 0,
            PublishedYear = 2022,
        };
        var postBook = await _client.PostAsJsonAsync("/api/books", book);
        var createdBook = await postBook.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(createdBook);

        var loan = new Loan
        {
            BookId = createdBook.Id,
            BorrowerName = "Bob",
            BorrowerEmail = "bob@example.com",
            DueDate = DateTime.UtcNow.AddDays(14),
        };

        var loanResponse = await _client.PostAsJsonAsync("/api/loans", loan);
        Assert.Equal(HttpStatusCode.BadRequest, loanResponse.StatusCode);
    }

    [Fact]
    public async Task ReturnLoan_UpdatesStatusAndIncreasesAvailableCopies()
    {
        var book = new Book
        {
            Title = "Return Test Book",
            Author = "Return Author",
            Isbn = "999",
            Genre = Genre.Science,
            TotalCopies = 1,
            AvailableCopies = 1,
            PublishedYear = 2021,
        };
        var postBook = await _client.PostAsJsonAsync("/api/books", book);
        var createdBook = await postBook.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(createdBook);

        var loan = new Loan
        {
            BookId = createdBook.Id,
            BorrowerName = "Carol",
            BorrowerEmail = "carol@example.com",
            DueDate = DateTime.UtcNow.AddDays(14),
        };
        var postLoan = await _client.PostAsJsonAsync("/api/loans", loan);
        var createdLoan = await postLoan.Content.ReadFromJsonAsync<Loan>();
        Assert.NotNull(createdLoan);

        var returnResponse = await _client.PostAsync($"/api/loans/{createdLoan.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);

        var returnedLoan = await returnResponse.Content.ReadFromJsonAsync<Loan>();
        Assert.NotNull(returnedLoan);
        Assert.Equal(LoanStatus.Returned, returnedLoan.Status);
        Assert.NotNull(returnedLoan.ReturnDate);

        var getBook = await _client.GetAsync($"/api/books/{createdBook.Id}");
        var updatedBook = await getBook.Content.ReadFromJsonAsync<Book>();
        Assert.NotNull(updatedBook);
        Assert.Equal(1, updatedBook.AvailableCopies);
    }

    [Fact]
    public async Task GetActiveLoans_FiltersCorrectly()
    {
        var response = await _client.GetAsync("/api/loans/active");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<Loan>>();
        Assert.NotNull(result);
        Assert.All(result, l => Assert.Equal(LoanStatus.Active, l.Status));
    }

    // ── Stats ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStats_ReturnsStatistics()
    {
        var response = await _client.GetAsync("/api/stats");
        response.EnsureSuccessStatusCode();

        var stats = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(stats);
        Assert.True(stats.ContainsKey("totalBooks"));
        Assert.True(stats.ContainsKey("totalLoans"));
        Assert.True(stats.ContainsKey("activeLoans"));
        Assert.True(stats.ContainsKey("mostBorrowedBooks"));
    }
}
