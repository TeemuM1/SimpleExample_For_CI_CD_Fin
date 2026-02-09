using FluentAssertions;
using Moq;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using SimpleExample.Application.Services;
using SimpleExample.Domain.Entities;
using Xunit;
using Xunit.Sdk;

namespace SimpleExample.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        // Mock: Email ei ole käytössä
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        UserDto result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti@example.com");

        // Varmista että AddAsync kutsuttiin kerran
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "existing@example.com"
        };

        User existingUser = new User("Maija", "Virtanen", "existing@example.com");

        // Mock: Email on jo käytössä!
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*jo olemassa*");

        // Varmista että AddAsync EI kutsuttu
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    // 1. GetByIdAsync - löytyy
    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnUserById()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User existingUser = new User("Matti", "Meikäläinen", "matti@example.com");

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        //Act
        UserDto? result = await _service.GetByIdAsync(userId);

        //Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti@example.com");

        _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
    }

    // 2. GetByIdAsync - ei löydy
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        //Arrange
        Guid nullId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.GetByIdAsync(nullId))
            .ReturnsAsync((User?)null);

        //Act
        UserDto? result = await _service.GetByIdAsync(nullId);

        //Assert
        result.Should().BeNull();

        _mockRepository.Verify(x => x.GetByIdAsync(nullId), Times.Once);
    }

    // 3. GetAllAsync - palauttaa listan
    [Fact]

    public async Task GetAllAsync_ReturnsList()
    {
        //Arrange
        List<User> users = new List<User>
        {
            new User("Matti", "Meikäläinen", "matti@example.com"),
            new User("Maija", "Meikäläinen", "maija@example.com"),
            new User("Pekka", "Pouta", "pekka@example.com")
        };

        _mockRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        //Act
        IEnumerable<UserDto> result = await _service.GetAllAsync();

        //Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        result.Should().ContainSingle(u => u.FirstName == "Matti" && u.Email == "matti@example.com");
        result.Should().ContainSingle(u => u.FirstName == "Maija" && u.Email == "maija@example.com");
        result.Should().ContainSingle(u => u.FirstName == "Pekka" && u.Email == "pekka@example.com");

        _mockRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    // 4. UpdateAsync - onnistuu
    [Fact]

    public async Task UpdateAsync_WithValidData_ShouldUpdate()
    {
        //Arrange
        Guid userId = Guid.NewGuid();
        User existingUser = new User("Matti", "Meikäläinen", "matti@example.com");

        UpdateUserDto dto = new UpdateUserDto
        {
            FirstName = "Pekka",
            LastName = "Pouta",
            Email = "pekka@example.com"
        };

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        _mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        //Act
        UserDto result = await _service.UpdateAsync(userId, dto);

        //Assert

        result.Should().NotBeNull();
        result.FirstName.Should().Be("Pekka");
        result.LastName.Should().Be("Pouta");
        result.Email.Should().Be("pekka@example.com");

        _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    // 5. UpdateAsync - käyttäjää ei löydy
    [Fact]

    public async Task UpdateAsync_UserNotFound_ShouldReturnNotFound()
    {
        //Arrange
        Guid invalidId = Guid.NewGuid();

        UpdateUserDto dto = new UpdateUserDto
        {
            FirstName = "Pekka",
            LastName = "Pouta",
            Email = "pekka@example.com"
        };

        _mockRepository
            .Setup(x => x.GetByIdAsync(invalidId))
            .ReturnsAsync((User?)null);

        //Act
        UserDto result = await _service.UpdateAsync(invalidId, dto);

        //Assert
        result.Should().BeNull();

        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);

    }

    // 6. DeleteAsync - onnistuu
    [Fact]

    public async Task DeleteAsync_UserFound_ShouldDeleteUser()
    {
        //Arrange
        Guid userId = Guid.NewGuid();

        User existingUser = new User("Matti", "Meikäläinen", "matti@example.com");

        _mockRepository
            .Setup(x => x.ExistsAsync(userId))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(x => x.DeleteAsync(userId))
            .Returns(Task.CompletedTask);

        //Act
        bool result = await _service.DeleteAsync(userId);

        //Assert
        result.Should().Be(true);

        _mockRepository.Verify(x => x.DeleteAsync(userId), Times.Once);
        _mockRepository.Verify(x => x.ExistsAsync(userId), Times.Once);

    }

    // 7. DeleteAsync - käyttäjää ei löydy
    [Fact]

    public async Task DeleteAsync_UserNotFound_ShouldReturnNotFound()
    {

        //Arrange
        Guid invalidId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.ExistsAsync(invalidId))
            .ReturnsAsync(false);

        //Act
        bool result = await _service.DeleteAsync(invalidId);

        //Assert
        result.Should().Be(false);

        _mockRepository.Verify(x => x.ExistsAsync(invalidId), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(invalidId), Times.Never);

    }
}