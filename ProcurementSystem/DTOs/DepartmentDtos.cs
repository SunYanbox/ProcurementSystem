namespace ProcurementSystem.DTOs;

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
}

public class DepartmentDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
