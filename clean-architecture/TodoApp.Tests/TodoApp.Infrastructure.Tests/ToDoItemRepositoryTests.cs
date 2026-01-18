using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Repositories;
using Xunit;

namespace TodoApp.Infrastructure.Tests;

public class ToDoItemRepositoryTests
{
    private readonly AppDbContext _context;
    private readonly ToDoItemRepository _repository;

    public ToDoItemRepositoryTests()
    {
        // Set up an in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new AppDbContext(options);
        _repository = new ToDoItemRepository(_context);
    }

    [Fact]
    public async Task AddItemAsync_ShouldAddItem()
    {
        // Arrange
        var newItem = new ToDoItem
        {
            ToDoItemId = 1,
            ToDoListId = 1,
            Title = "Test Item",
            Description = "This is a test item",
            DueDate = DateTime.Now,
            IsCompleted = false,
            Priority = PriorityLevel.Medium,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Act
        await _repository.AddItemAsync(newItem);
        var items = await _repository.GetItemByIdAsync(1);

        // Assert
        //Assert.Single(items);
        Assert.Equal("Test Item", items!.Title);
    }

    [Fact]
    public async Task GetAllItemsAsync_ShouldReturnAllItems()
    {
        // Arrange
        _context.ToDoItems.AddRange(
            new ToDoItem
            {
                ToDoItemId = 2,
                ToDoListId = 1,
                Title = "Item 1",
                Description = "This is a test item",
                DueDate = DateTime.Now,
                IsCompleted = false,
                Priority = PriorityLevel.Medium,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new ToDoItem
            {
                ToDoItemId = 3,
                ToDoListId = 1,
                Title = "Item 2",
                DueDate = DateTime.Now,
                IsCompleted = false,
                Priority = PriorityLevel.Medium,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Description = "This is a test item"
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var items = await _repository.GetAllItemsAsync();

        // Assert
        Assert.True(items.Count() >= 2);
    }

    [Fact]
    public async Task GetItemByIdAsync_ShouldReturnItem()
    {
        // Arrange
        var item = new ToDoItem
        {
            ToDoItemId = 5,
            ToDoListId = 1,
            Title = "Item 1",
            Description = "This is a test item",
            DueDate = DateTime.Now,
            IsCompleted = false,
            Priority = PriorityLevel.Medium,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        await _context.ToDoItems.AddAsync(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetItemByIdAsync(5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Item 1", result.Title);
    }

    [Fact]
    public async Task DeleteItemAsync_ShouldRemoveItem()
    {
        // Arrange
        var item = new ToDoItem
        {
            ToDoItemId = 4,
            ToDoListId = 1,
            Title = "Item 1",
            Description = "This is a test item",
            DueDate = DateTime.Now,
            IsCompleted = false,
            Priority = PriorityLevel.Medium,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        await _context.ToDoItems.AddAsync(item);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteItemAsync(4);
        var result = await _repository.GetItemByIdAsync(4);

        // Assert
        Assert.Null(result);
    }

    // Add more tests for GetItemsByListIdAsync and UpdateItemAsync as needed
}