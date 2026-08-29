using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.DTOs;
using ProcurementSystem.Models;

namespace ProcurementSystem.Services;

public interface ITodoService
{
    Task<List<Todo>> GetAllAsync();
    Task<Todo?> GetByIdAsync(int id);
    Task<Todo> CreateAsync(CreateTodoDto dto);
    Task<Todo?> UpdateAsync(int id, UpdateTodoDto dto);
    Task<bool> DeleteAsync(int id);
}

public class TodoService(ProcurementDbContext db) : ITodoService
{
    public Task<List<Todo>> GetAllAsync() => db.Todos.ToListAsync();

    public Task<Todo?> GetByIdAsync(int id) => db.Todos.FindAsync(id).AsTask();

    public async Task<Todo> CreateAsync(CreateTodoDto dto)
    {
        var todo = new Todo
        {
            Name = dto.Name,
            IsComplete = dto.IsComplete
        };

        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return todo;
    }

    public async Task<Todo?> UpdateAsync(int id, UpdateTodoDto dto)
    {
        var todo = await db.Todos.FindAsync(id);
        if (todo is null) return null;

        todo.Name = dto.Name;
        todo.IsComplete = dto.IsComplete;
        await db.SaveChangesAsync();
        return todo;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var todo = await db.Todos.FindAsync(id);
        if (todo is null) return false;

        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return true;
    }
}
