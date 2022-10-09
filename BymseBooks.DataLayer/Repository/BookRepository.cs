using System.Transactions;
using BymseBooks.DataLayer.Database;
using BymseBooks.DataLayer.Entity;
using BymseBooks.DataLayer.Helpers;
using BymseBooks.DataLayer.Models;
using BymseBooks.DataLayer.Models.Extensions;
using Z.EntityFramework.Plus;

namespace BymseBooks.DataLayer.Repository
{
    public class BookRepository : IBookRepository
    {
        private readonly BooksDbContext context;

        public BookRepository(BooksDbContext context)
        {
            this.context = context;
        }

        public IReadOnlyList<BookModel> GetBooks(int userId, out int booksCount,
            int? takeCount = null, int? skipCount = null,
            IList<int>? tagsIds = null)
        {
            using var transaction = new TransactionScope();

            IQueryable<Book> query;

            if (!tagsIds.IsNullOrEmpty())

            {
                var booksIds = context.BookTagLinks
                    .Where(link => tagsIds!.Contains(link.TagId))
                    .GroupBy(r => r.BookId)
                    .Where(g => g.Count() == tagsIds!.Count)
                    .Select(r => r.Key);

                query = GetUserBooks(userId).Where(r => booksIds.Contains(r.BookId));
            }
            else
            {
                query = GetUserBooks(userId);
            }

            booksCount = query.Count();

            var models = query
                .OrderBy(e => e.State == BookState.Active
                    ? 0
                    : e.State + 2)
                .If(skipCount.HasValue, e => e.Skip(skipCount!.Value))
                .If(takeCount.HasValue, e => e.Take(takeCount!.Value))
                .OrderBy(e => e.State == BookState.Active
                    ? 0
                    : e.State + 2)
                .Select(book => new BookModel
                {
                    BookId = book.BookId,
                    AuthorName = book.AuthorName,
                    Title = book.Title,
                    TagsIds = book.BookTags.Select(tag => tag.TagId).ToList(),
                    Url = book.Url,
                    State = book.State,
                })
                .ToArray();

            transaction.Complete();
            return models;
        }


        public bool Exist(string title, string authorName, int userId) =>
            GetUserBooks(userId)
                .Any(e => e.Title == title && e.AuthorName == authorName);

        public bool Exist(int bookId, int userId)
        {
            return GetUserBooks(userId).Any(e => e.BookId == bookId);
        }

        public BookModel? FindBook(int bookId, int userId)
        {
            return GetUserBooks(userId)
                .Where(e => e.BookId == bookId)
                .ToModels()
                .FirstOrDefault();
        }

        private IQueryable<Book> GetUserBooks(int userId) => context
            .Books
            .Where(e => e.UserId == userId);

        public int Insert(Book book)
        {
            context.Books.Add(book);
            context.SaveChanges();
            return book.BookId;
        }

        public void Update(int bookId, string? title, string? author, string? url, IList<int>? tags)
        {
            var book = context.Books.FirstOrDefault(e => e.BookId == bookId) ?? new Book();

            book.Title = title!.FallbackWith(book.Title);
            book.AuthorName = author!.FallbackWith(book.AuthorName);
            book.Url = url!.FallbackWith(book.Url!);

            if (!tags.IsNullOrEmpty())
            {
                context.BookTagLinks.Where(e => e.BookId == bookId).Delete();
                book.BookTags = tags!.Select(e => new BookTagLink
                {
                    BookId = bookId,
                    TagId = e
                }).ToArray();
            }

            context.SaveChanges();
        }

        public void Update(int bookId, BookState state)
        {
            context.Books.Where(r => r.BookId == bookId).Update(r => new Book()
            {
                State = state
            });
        }

        public void Delete(int bookId) => context.Books.Where(e => e.BookId == bookId).Delete();
    }
}