﻿using BymseBooks.DataLayer.Entity;
using BymseBooks.DataLayer.Repository;

namespace BymseBooks.Core.Features.BooksList;

public class BooksService
{
    private readonly IBookRepository bookRepository;

    public BooksService(IBookRepository bookRepository)
    {
        this.bookRepository = bookRepository;
    }

    public BookModel[] GetBooks(BookState state, int? takeCount = null, int? skipCount = null)
    {
        var books = bookRepository.GetBooks(state, takeCount, skipCount);
        return books.Select(b =>
        {
            var lastPage = b.Bookmarks
                .LastOrDefault(r => r.BookmarkType == BookmarkType.LastPage)?
                .PageNumber;
            return new BookModel
            {
                Id = b.BookId,
                Title = b.Title,
                Author = b.AuthorName,
                State = b.State,
                Percents = GetPercents(lastPage, b.TotalPages, b.State),
                Tags = b.BookTags.Select(e => e.Tag.Title).ToArray()
            };
        }).ToArray();
    }
    
    private static int? GetPercents(int? lastPage, int? totalPages, BookState state)
    {
        if (state == BookState.New)
        {
            return 0;
        }
        
        if (state == BookState.Finished)
        {
            return 100;
        }
        
        if (!totalPages.HasValue)
        {
            return null;
        }
        
        if (!lastPage.HasValue)
        {
            return 0;
        }
        
        return (int)(Math.Round(((double) lastPage.Value) / totalPages.Value, 2) * 100);
    }
}