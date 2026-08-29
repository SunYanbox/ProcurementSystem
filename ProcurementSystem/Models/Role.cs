namespace ProcurementSystem.Models;

/// <summary>
/// <see href="https://en.wikipedia.org/wiki/Role-based_access_control">
/// Two-level RBAC
/// </see>
/// </summary>
public enum Role
{
    /// <summary>Manages catalog, stock, approvals, purchases, and employee accounts.</summary>
    Admin,

    /// <summary>Submits procurement requests and views their own progress.</summary>
    Employee
}
