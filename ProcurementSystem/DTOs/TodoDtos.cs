namespace ProcurementSystem.DTOs;

public class TodoDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}

public class CreateTodoDto
{
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}

public class UpdateTodoDto
{
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}
