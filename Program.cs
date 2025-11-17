using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

// --- 2. In-Memory Data Store ---
var now = DateTime.UtcNow;
// Use a static list as an in-memory database replacement for demonstration
var users = new List<User>
{
    new(Guid.NewGuid(), "Jane Doe", "jane.doe@example.com", now, now),
    new(Guid.NewGuid(), "John Smith", "john.smith@example.com",now, now),
};


// POST /users (Create)
app.MapPost("/users", ([FromBody] CreateUserRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest("Name and Email are required.");
    }

    var timestamp = DateTime.UtcNow;
    if(users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.Conflict($"A user with the email {request.Email} already exists.");
    }
    
    var newUser = new User(
        Id: Guid.NewGuid(),
        Name: request.Name,
        Email: request.Email,
        timestamp,
        timestamp
    );

    users.Add(newUser);

    // Returns a 201 Created status with the location header pointing to the new resource
    return Results.Created($"/users/{newUser.Id}", newUser);
})
.WithName("CreateUser")
.WithSummary("Creates a new user.");


// GET /users (Read All)
app.MapGet("/users", () => Results.Ok(users))
.WithName("GetAllUsers")
.WithSummary("Retrieves a list of all users.");


// GET /users/{id} (Read Single)
app.MapGet("/users/{id:guid}", (Guid id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);

    if (user is null)
    {
        // Returns a 404 Not Found status
        return Results.NotFound($"User with ID {id} not found.");
    }

    // Returns a 200 OK status with the user data
    return Results.Ok(user);
})
.WithName("GetUserById")
.WithSummary("Retrieves a single user by their ID.");


// PUT /users/{id} (Update)
app.MapPut("/users/{id:guid}", (Guid id, [FromBody] UpdateUserRequest request) =>
{
    var existingUser = users.FirstOrDefault(u => u.Id == id);

    if (existingUser is null)
    {
        return Results.NotFound($"User with ID {id} not found.");
    }

    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest("Name and Email are required for update.");
    }

    // Create a new User record with the updated fields (records are immutable)
    var updatedUser = existingUser with
    {
        Name = request.Name,
        Email = request.Email,
        UpdatedAt = DateTime.UtcNow
    };

    // Find the index of the old user and replace it with the updated one
    var index = users.FindIndex(u => u.Id == id);
    users[index] = updatedUser;

    // Returns a 200 OK status with the updated user data
    return Results.Ok(updatedUser);
})
.WithName("UpdateUser")
.WithSummary("Updates an existing user by ID.");


// DELETE /users/{id} (Delete)
app.MapDelete("/users/{id:guid}", (Guid id) =>
{
    var userToRemove = users.FirstOrDefault(u => u.Id == id);

    if (userToRemove is null)
    {
        // Indicate success even if the user didn't exist (idempotency), but inform the user.
        return Results.NoContent();
    }

    users.Remove(userToRemove);

    // Returns a 204 No Content status, which is standard for a successful DELETE
    return Results.NoContent();
})
.WithName("DeleteUser")
.WithSummary("Deletes a user by their ID.");


app.Run();


record User(Guid Id, 
    string Name, 
    string Email, 
    DateTime CreatedAt,
    DateTime UpdatedAt);

record CreateUserRequest(string Name, string Email);

record UpdateUserRequest(string Name, string Email);
