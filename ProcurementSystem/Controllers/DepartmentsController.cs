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
}
