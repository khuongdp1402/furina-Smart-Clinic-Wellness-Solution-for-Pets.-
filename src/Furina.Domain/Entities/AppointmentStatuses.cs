namespace Furina.Domain.Entities;

/// <summary>TASK-23: the appointment state machine's states.</summary>
public static class AppointmentStatuses
{
    public const string Booked = "Booked";
    public const string CheckedIn = "CheckedIn";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// TASK-23: "Validate transition Ở TẦNG SERVICE, không tin dữ liệu trạng
/// thái gửi từ client" — the allowed-transitions table lives here, one
/// place, so nothing else in the app can invent its own rules. Booked →
/// CheckedIn → InProgress → Completed is the happy path; Cancelled is
/// only reachable from Booked or CheckedIn (a customer who no-shows or
/// changes their mind before or after arriving) — NOT from InProgress
/// (once treatment starts, it runs to Completed) and NOT from Completed
/// (terminal, no reversal, per AC-2).
/// </summary>
public static class AppointmentStatusRules
{
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        [AppointmentStatuses.Booked] = [AppointmentStatuses.CheckedIn, AppointmentStatuses.Cancelled],
        [AppointmentStatuses.CheckedIn] = [AppointmentStatuses.InProgress, AppointmentStatuses.Cancelled],
        [AppointmentStatuses.InProgress] = [AppointmentStatuses.Completed],
        [AppointmentStatuses.Completed] = [],
        [AppointmentStatuses.Cancelled] = [],
    };

    public static bool CanTransition(string from, string to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>TASK-23 test case 4: cancelling requires a reason; other transitions don't.</summary>
    public static bool RequiresReason(string to) => to == AppointmentStatuses.Cancelled;
}
