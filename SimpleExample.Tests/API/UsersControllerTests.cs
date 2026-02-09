using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SimpleExample.API.Controllers;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using SimpleExample.Domain.Entities;
using Xunit;

namespace SimpleExample.Tests.API;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockService = new Mock<IUserService>();
        _controller = new UsersController(_mockService.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithUsers()
    {
        // Arrange
        List<UserDto> users = new List<UserDto>
        {
            new UserDto { Id = Guid.NewGuid(), FirstName = "Matti", LastName = "M", Email = "m@m.com" },
            new UserDto { Id = Guid.NewGuid(), FirstName = "Maija", LastName = "V", Email = "m@v.com" }
        };

        _mockService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        ActionResult<IEnumerable<UserDto>> result = await _controller.GetAll();

        // Assert
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        IEnumerable<UserDto> returnedUsers = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_WhenUserExists_ShouldReturnOk()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        UserDto user = new UserDto { Id = userId, FirstName = "Matti", LastName = "M", Email = "m@m.com" };

        _mockService
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        ActionResult<UserDto> result = await _controller.GetById(userId);

        // Assert
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        UserDto returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetById_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _mockService
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((UserDto?)null);

        // Act
        ActionResult<UserDto> result = await _controller.GetById(userId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        CreateUserDto createDto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        UserDto createdUser = new UserDto
        {
            Id = Guid.NewGuid(),
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email
        };

        _mockService
            .Setup(x => x.CreateAsync(createDto))
            .ReturnsAsync(createdUser);

        // Act
        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        CreatedAtActionResult createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        UserDto returnedUser = createdResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.FirstName.Should().Be("Matti");
    }

    // 1. Create - InvalidOperationException (duplicate) → 409 Conflict
    [Fact]
    public async Task Create_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange

        CreateUserDto createDto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        _mockService
            .Setup(x => x.CreateAsync(createDto))
            .ThrowsAsync(new InvalidOperationException());

        // Act
        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    // 2. Create - ArgumentException (validation) → 400 BadRequest
    [Fact]
    public async Task Create_WithValidationError_ShouldReturnBadRequest()
    {
        //Arrange

        CreateUserDto createDto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        _mockService
            .Setup(x => x.CreateAsync(createDto))
            .ThrowsAsync(new ArgumentException());

        // Act
        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
    // 3. Update - onnistuu → 200 OK
    [Fact]
    public async Task Update_WithValidData_ShouldReturnUpdated()
    {
        //Arrange
        Guid userId = Guid.NewGuid();

        UpdateUserDto updateUser = new UpdateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        UserDto updatedUser = new UserDto
        {
            Id = userId,
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        _mockService
            .Setup(x => x.UpdateAsync(userId, updateUser))
            .ReturnsAsync(updatedUser);

        //Act
        ActionResult<UserDto> result = await _controller.Update(userId, updateUser);

        //Asser
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        UserDto returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.LastName.Should().Be("Meikäläinen");
        returnedUser.Email.Should().Be("matti@example.com");
    }

    // 4. Update - käyttäjää ei löydy → 404 NotFound
    [Fact]
    public async Task Update_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        Guid invalidId = Guid.NewGuid();
        UpdateUserDto updateDto = new UpdateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        _mockService
            .Setup(x => x.UpdateAsync(invalidId, updateDto))
            .ReturnsAsync((UserDto?)null);

        // Act
        ActionResult<UserDto> result = await _controller.Update(invalidId, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // 5. Update - ArgumentException → 400 BadRequest
    [Fact]
    public async Task Update_WithValidationError_ShouldReturnBadRequest()
    {
        //Arrange
        Guid userId = Guid.NewGuid();
        UpdateUserDto updateDto = new UpdateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        _mockService
            .Setup(x => x.UpdateAsync(userId, updateDto))
            .ThrowsAsync(new ArgumentException());

        // Act
        ActionResult<UserDto> result = await _controller.Update(userId, updateDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // 6. Delete - onnistuu → 204 NoContent
    [Fact]
    public async Task Delete_WithoutError_ShouldReturnNoContent()
    {
        //Arrange
        Guid userId = Guid.NewGuid();


        _mockService
            .Setup(x => x.DeleteAsync(userId))
            .ReturnsAsync(true);

        //Act
        ActionResult result = await _controller.Delete(userId);

        //Assert
        result.Should().BeOfType<NoContentResult>();

    }

    // 7. Delete - käyttäjää ei löydy → 404 NotFound
    [Fact]
    public async Task Delete_WhenUserNotFound_ShouldReturnNotFound()
    {
        //Arrange
        Guid userId = Guid.NewGuid();


        _mockService
            .Setup(x => x.DeleteAsync(userId))
            .ReturnsAsync(false);

        //Act
        ActionResult result = await _controller.Delete(userId);

        //Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }


}


    // TEHTÄVÄ: Kirjoita itse testit seuraaville:
    // 7. Delete - käyttäjää ei löydy → 404 NotFound