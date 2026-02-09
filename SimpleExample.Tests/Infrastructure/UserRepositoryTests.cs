using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SimpleExample.Application.DTOs;
using SimpleExample.Domain.Entities;
using SimpleExample.Infrastructure.Data;
using SimpleExample.Infrastructure.Repositories;
using Xunit;

namespace SimpleExample.Tests.Infrastructure;

public class UserRepositoryIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryIntegrationTests()
    {
        // Käytä in-memory databasea testaukseen
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        User user = new User("Matti", "Meikäläinen", "matti@example.com");

        // Act
        User result = await _repository.AddAsync(user);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);

        // Varmista että tallentui tietokantaan
        User? savedUser = await _context.Users.FindAsync(result.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("Matti", savedUser.FirstName);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldFindUserByEmail()
    {
        // Arrange
        User user = new User("Matti", "Meikäläinen", "test@example.com");
        await _repository.AddAsync(user);

        // Act
        User? result = await _repository.GetByEmailAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Matti", result.FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFindUserById()
    {
        //Arrange
        User user = new User("Matti", "Meikäläinen", "test@example.com");
        await _repository.AddAsync(user);

        //Act
        User? result = await _repository.GetByIdAsync(user.Id);

        //Assert
        Assert.NotNull(result);
        Assert.Equal("Matti", result.FirstName);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        //Arrange

        List<User> users = new List<User>
        {
            new User("Matti", "Meikäläinen", "matti@example.com"),
            new User("Maija", "Meikäläinen", "maija@example.com"),
            new User("Pekka", "Pouta", "pekka@example.com")
        };

        foreach (var u in users)
            await _repository.AddAsync(u);

        //Act
        IEnumerable<User> result = await _repository.GetAllAsync();

        //Assert
        Assert.NotNull(result);
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser()
    {
        //Arrange
        var existingUser = new User("Matti", "Meikäläinen", "matti@example.com");
        await _repository.AddAsync(existingUser);

        existingUser.UpdateBasicInfo("Matukka", "Meikäleissön");

        //Act
        User? result = await _repository.UpdateAsync(existingUser);

        //Assert
        result.Should().NotBeNull();

        var fromDb = await _repository.GetByEmailAsync("matti@example.com");
        Assert.NotNull(fromDb);
        Assert.Equal("Matukka", fromDb!.FirstName);
        Assert.Equal("Meikäleissön", fromDb!.LastName);
        Assert.Equal("matti@example.com", fromDb!.Email);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUser()
    {
        //Arrange
        User user = new User("Matti", "Meikäläinen", "matti@example.com");
        await _repository.AddAsync(user);

        //Act
        await _repository.DeleteAsync(user.Id);

        //Assert 
        var fromDb = await _repository.GetByEmailAsync("matti@example.com");
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnExists()
    {
        //Arrange
        User user = new User("Matti", "Meikäläinen", "matti@example.com");
        await _repository.AddAsync(user);

        //Act
        bool result = await _repository.ExistsAsync(user.Id);

        //Assert
        Assert.True(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
