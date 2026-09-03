using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.DTOs;

public class CreateDepartmentRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class DepartmentDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
