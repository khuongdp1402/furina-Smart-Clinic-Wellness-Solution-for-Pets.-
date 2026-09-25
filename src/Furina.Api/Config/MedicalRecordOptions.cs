namespace Furina.Api.Config;

/// <summary>TASK-19: "khoá sửa sau X giờ (cấu hình được, mặc định 24h)".</summary>
public class MedicalRecordOptions
{
    public const string SectionName = "MedicalRecord";

    public int EditLockHours { get; set; } = 24;
}
