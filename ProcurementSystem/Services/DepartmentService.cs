using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.DTOs;
using ProcurementSystem.Models;

namespace ProcurementSystem.Services;

public enum DepartmentError
{
    NameTaken
}

public record DepartmentResult<T>(T? Value, DepartmentError? Error)
{
    public static DepartmentResult<T> Ok(T value) => new(value, null);
    public static DepartmentResult<T> Fail(DepartmentError error) => new(default, error);
}

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> ListAsync();
    Task<DepartmentResult<DepartmentDto>> CreateAsync(CreateDepartmentRequest dto);
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

    public async Task<DepartmentResult<DepartmentDto>> CreateAsync(CreateDepartmentRequest dto)
    {
        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return DepartmentResult<DepartmentDto>.Fail(DepartmentError.NameTaken);

        // 名称唯一由数据库唯一索引兜底，此处先查一次以返回友好的冲突错误
        if (await db.Departments.AnyAsync(d => d.Name == name))
            return DepartmentResult<DepartmentDto>.Fail(DepartmentError.NameTaken);

        var department = new Department { Name = name };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        return DepartmentResult<DepartmentDto>.Ok(new DepartmentDto { Id = department.Id, Name = department.Name });
    }
}
