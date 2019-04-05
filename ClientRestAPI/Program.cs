using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ClientRestAPI
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    internal class Program
    {
        public static HttpClient Client { get; } = new HttpClient();


        private static void ShowBook(Book book)
        {
            Console.WriteLine($"Id:{book.Id} {book.Title}");
        }

        private static async Task<Uri> CreateBookAsync(Book book)
        {
            var response = await Client.PostAsJsonAsync(
                "api/books", book);
            response.EnsureSuccessStatusCode();

            // return URI of the created resource.
            return response.Headers.Location;
        }

        private static async Task<Book> GetBookAsync(string path)
        {
            Book book = null;
            var response = await Client.GetAsync(path);
            if (response.IsSuccessStatusCode)
                book = await response.Content.ReadAsAsync<Book>();

            return book;
        }

        private static async Task<IEnumerable<Book>> GetBooksAsync(string path)
        {
            var response = await Client.GetAsync(path);
            var dataObjects = await response.Content.ReadAsAsync<IEnumerable<Book>>();
            var booksAsync = dataObjects as Book[] ?? dataObjects.ToArray();

            return booksAsync;
        }

        private static async Task<Book> AddBookAsync(Book book)
        {
            var response = await Client.PostAsJsonAsync(
                "api/books", book);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated book from the response body.
            book = await response.Content.ReadAsAsync<Book>();
            return book;
        }

        private static async Task<Book> UpdateBookAsync(Book book)
        {
            var response = await Client.PutAsJsonAsync(
                $"api/books/{book.Id}", book);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated book from the response body.
            book = await response.Content.ReadAsAsync<Book>();
            return book;
        }

        private static async Task<HttpStatusCode> DeleteBookAsync(int id)
        {
            var response = await Client.DeleteAsync(
                $"api/books/{id}");
            return response.StatusCode;
        }

        private static string GetAbsUrl(Uri url)
        {
            return url.Segments[0] + url.Segments[1] + url.Segments[2];
        }

        private static async Task RunAsync()
        {
            Client.BaseAddress = new Uri("https://eugenetestwebapp.azurewebsites.net");
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                // Create a new book
                var url =
                    await CreateBookAsync(new Book {Id = 8000, Title = "Star Wars"});
                Console.WriteLine($"Created at {url}");

                // Get the book
                var book = await GetBookAsync(url.PathAndQuery);
                ShowBook(book);

                // Update the book
                Console.WriteLine("Updating Title...");
                book.Title = "Star Wars 0";
                await UpdateBookAsync(book);

                // Get the updated book
                book = await GetBookAsync(url.PathAndQuery);
                ShowBook(book);

                var repository = new List<Book>
                {
                    new Book {Id = 8001, Title = "Star Wars 1"},
                    new Book {Id = 8002, Title = "Star Wars 2"},
                    new Book {Id = 8003, Title = "Star Wars 3"},
                    new Book {Id = 8004, Title = "Star Wars 4"}
                };

                // Add the book list from repository to server
                foreach (var bk in repository) await AddBookAsync(bk);

                // Get all books
                Console.WriteLine("All books...");
                var books = await GetBooksAsync(GetAbsUrl(url));
                repository.Clear();
                foreach (var bk in books)
                {
                    repository.Add(bk);
                    ShowBook(bk);
                }

                Console.WriteLine(
                    "In the next step all book entities will be deleted from the server...");
                Console.ReadLine();

                // Delete the books
                foreach (var bk in repository)
                {
                    var statusCode = await DeleteBookAsync(bk.Id);
                    Console.WriteLine($"Deleted (HTTP Status = {(int) statusCode})");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }

        private static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();
        }
    }
}