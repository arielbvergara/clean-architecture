using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.WebAPI.Controllers;
using Xunit;

namespace TodoApp.WebAPI.Tests;

public class ToDoItemControllerTests
{
    private readonly Mock<IToDoItemService> _mockService;
    private readonly ToDoItemController _controller;

    public ToDoItemControllerTests()
    {
        _mockService = new Mock<IToDoItemService>();
        _controller = new ToDoItemController(_mockService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithItems()
    {
        // Arrange
        var items = new List<ToDoItem>
        {
            new() { ToDoItemId = 1, Title = "Task 1" },
            new() { ToDoItemId = 2, Title = "Task 2" }
        };
        _mockService.Setup(service => service.GetAllItemsAsync()).ReturnsAsync(items);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItems = Assert.IsType<List<ToDoItem>>(okResult.Value);
        Assert.Equal(2, returnedItems.Count);
    }
    [Fact]
    public async Task GetById_ReturnsNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        _mockService.Setup(service => service.GetItemByIdAsync(1)).ReturnsAsync((ToDoItem)null!);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ReturnsOkResult_WithItem()
    {
        // Arrange
        var item = new ToDoItem { ToDoItemId = 1, Title = "Task 1" };
        _mockService.Setup(service => service.GetItemByIdAsync(1)).ReturnsAsync(item);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItem = Assert.IsType<ToDoItem>(okResult.Value);
        Assert.Equal(item.ToDoItemId, returnedItem.ToDoItemId);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionResult()
    {
        // Arrange
        var newItem = new ToDoItem { ToDoItemId = 3, Title = "New Task" };

        // Act
        var result = await _controller.Create(newItem);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("GetById", createdResult.ActionName);
        Assert.Equal(newItem.ToDoItemId, createdResult.RouteValues!["id"]);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenIdsDoNotMatch()
    {
        // Arrange
        var item = new ToDoItem { ToDoItemId = 1, Title = "Updated Task" };

        // Act
        var result = await _controller.Update(2, item);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsNoContent()
    {
        // Arrange
        var item = new ToDoItem { ToDoItemId = 1, Title = "Updated Task" };

        // Act
        var result = await _controller.Update(1, item);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        int itemId = 1;

        // Act
        var result = await _controller.Delete(itemId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}