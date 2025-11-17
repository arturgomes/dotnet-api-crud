using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}



// POST /users (Create)
app.MapPost("/users", async ([FromBody] CreateUserRequest request, ApplicationDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest("Name and Email are required.");
    }

    var timestamp = DateTime.UtcNow;

    var newUser = new User(
        Id: Guid.NewGuid(),
        Name: request.Name,
        Email: request.Email,
        timestamp,
        timestamp
    );

    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    // Returns a 201 Created status with the location header pointing to the new resource
    return Results.Created($"/users/{newUser.Id}", newUser);
})
.WithName("CreateUser")
.WithSummary("Creates a new user.");


// GET /users (Read All)
app.MapGet("/users", async (ApplicationDbContext db) =>
{
    var users = await db.Users.ToListAsync();
    return Results.Ok(users);

})
.WithName("GetAllUsers")
.WithSummary("Retrieves a list of all users.");


// GET /users/{id} (Read Single)
app.MapGet("/users/{id:guid}", async (Guid id, ApplicationDbContext db) =>
{
    var user = await db.Users.FindAsync(id);

    if (user is null)
    {
        return Results.NotFound($"User with ID {id} not found.");
    }

    return Results.Ok(user);
})
.WithName("GetUserById")
.WithSummary("Retrieves a single user by their ID.");


// PUT /users/{id} (Update)
app.MapPut("/users/{id:guid}", async (Guid id, [FromBody] UpdateUserRequest request, ApplicationDbContext db) =>
{
        var existingUser = await db.Users.FindAsync(id);


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

    db.Entry(existingUser).CurrentValues.SetValues(updatedUser);
    await db.SaveChangesAsync();
    // Returns a 200 OK status with the updated user data
    return Results.Ok(updatedUser);
})
.WithName("UpdateUser")
.WithSummary("Updates an existing user by ID.");


// DELETE /users/{id} (Delete)
app.MapDelete("/users/{id:guid}", async (Guid id, ApplicationDbContext db) =>
{
    var userToRemove = await db.Users.FindAsync(id);

    if (userToRemove is null)
    {
        // Indicate success even if the user didn't exist (idempotency), but inform the user.
        return Results.NoContent();
    }

    db.Users.Remove(userToRemove);
    await db.SaveChangesAsync();

    // Returns a 204 No Content status, which is standard for a successful DELETE
    return Results.NoContent();
})
.WithName("DeleteUser")
.WithSummary("Deletes a user by their ID.");


app.Run();


public record User(Guid Id, 
    string Name, 
    string Email, 
    DateTime CreatedAt,
    DateTime UpdatedAt);

record CreateUserRequest(string Name, string Email);

record UpdateUserRequest(string Name, string Email);
