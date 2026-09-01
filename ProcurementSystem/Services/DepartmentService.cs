using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.DTOs;

namespace ProcurementSystem.Services;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> ListAsync();
}

public class DepartmentService(ProcurementDbContext db) : IDepartmentService
{
    public async Task<IReadOnlyList<DepartmentDto>> ListAsync()
    {
        var departments = await db.Departments
            .OrderBy(d => d.Id)
            .ToListAsync();

        return departments
            .Select(d => new DepartmentDto { Id = d.Id, Name = d.Name })
            .ToList();
    }
}
