using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.DTOs;
using ProcurementSystem.Models;
using ProcurementSystem.Services;

namespace ProcurementSystem.Controllers;

[ApiController]
[Route("todos")]
public class TodoController(ITodoService todoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoDto>>> GetTodos()
    {
        var todos = await todoService.GetAllAsync();
        return Ok(todos.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoDto>> GetTodo(int id)
    {
        var todo = await todoService.GetByIdAsync(id);
        return todo is null ? NotFound() : Ok(ToDto(todo));
    }

    [HttpPost]
    public async Task<ActionResult<TodoDto>> CreateTodo(CreateTodoDto dto)
    {
        var todo = await todoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, ToDto(todo));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(int id, UpdateTodoDto dto)
    {
        var todo = await todoService.UpdateAsync(id, dto);
        return todo is null ? NotFound() : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        var deleted = await todoService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    private static TodoDto ToDto(Todo todo) => new()
    {
        Id = todo.Id,
        Name = todo.Name,
        IsComplete = todo.IsComplete
    };
}
