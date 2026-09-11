namespace Furina.Domain.Entities;

/// <summary>
/// A recurring weekly shift for one <see cref="Staff"/> assignment (TASK-16
/// scope decision: only recurring weekly patterns, matching the concrete
/// test cases in the task — "xếp ca thứ 2-6", "xem lịch tuần"; one-off
/// date-specific overrides are not part of this task's scope).
/// </summary>
public class Shift : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid StaffId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public Staff Staff { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
