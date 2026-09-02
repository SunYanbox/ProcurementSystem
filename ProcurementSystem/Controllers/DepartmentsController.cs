using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.DTOs;
using ProcurementSystem.Services;

namespace ProcurementSystem.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController(IDepartmentService departmentService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> List()
    {
        var departments = await departmentService.ListAsync();
        return Ok(departments);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> Create(CreateDepartmentRequest dto)
    {
        var result = await departmentService.CreateAsync(dto);
        return result.Error switch
        {
            DepartmentError.NameTaken => Conflict(),
            null => StatusCode(StatusCodes.Status201Created, result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }
}
