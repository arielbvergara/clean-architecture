using Moq;
using TodoApp.Application.Interfaces;
using TodoApp.Application.Services;
using TodoApp.Domain.Entities;
using Xunit;

namespace TodoApp.Application.Tests;

public class ToDoItemServiceTests
{
    private readonly Mock<IToDoItemRepository> _mockRepository;
    private readonly ToDoItemService _service;
    public ToDoItemServiceTests()
    {
        _mockRepository = new Mock<IToDoItemRepository>();
        _service = new ToDoItemService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetAllItemsAsync_ReturnsItems()
    {
        //Arrange
        var expectedItems = new List<ToDoItem>
        {
            new() { ToDoItemId = 1, Title = "Task 1", Description = "Description 1" },
            new() { ToDoItemId = 2, Title = "Task 2", Description = "Description 2" }
        };

        _mockRepository.Setup(repo => repo.GetAllItemsAsync()).ReturnsAsync(expectedItems);

        //Act
        var result = await _service.GetAllItemsAsync();

        //Assert
        Assert.NotNull(result);
        var toDoItems = result.ToList();
        Assert.Equal(2, toDoItems.Count);
        Assert.Equal(expectedItems, toDoItems);
    }

    [Fact]
    public async Task GetItemByIdAsync_ReturnsItem()
    {
        // Arrange
        var expectedItem = new ToDoItem { ToDoItemId = 1, Title = "Task 1", Description = "Description 1" };
        _mockRepository.Setup(repo => repo.GetItemByIdAsync(1)).ReturnsAsync(expectedItem);

        // Act
        var result = await _service.GetItemByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedItem.ToDoItemId, result.ToDoItemId);
    }

    [Fact]
    public async Task AddItemAsync_CallsRepositoryMethod()
    {
        // Arrange
        var newItem = new ToDoItem { ToDoItemId = 3, Title = "Task 3", Description = "Description 3" };

        // Act
        await _service.AddItemAsync(newItem);

        // Assert
        _mockRepository.Verify(repo => repo.AddItemAsync(newItem), Times.Once);
    }

    [Fact]
    public async Task UpdateItemAsync_CallsRepositoryMethod()
    {
        // Arrange
        var updatedItem = new ToDoItem { ToDoItemId = 1, Title = "Updated Task", Description = "Updated Description" };

        // Act
        await _service.UpdateItemAsync(updatedItem);

        // Assert
        _mockRepository.Verify(repo => repo.UpdateItemAsync(updatedItem), Times.Once);
    }

    [Fact]
    public async Task DeleteItemAsync_CallsRepositoryMethod()
    {
        // Arrange
        int itemIdToDelete = 1;

        // Act
        await _service.DeleteItemAsync(itemIdToDelete);

        // Assert
        _mockRepository.Verify(repo => repo.DeleteItemAsync(itemIdToDelete), Times.Once);
    }
}