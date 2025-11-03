namespace SmartShift.Api.Requests;

public class UpdateEmployeeParametersRequest
{
    public int SkillLevel { get; set; }
    public int PriorityRating { get; set; }
    public int MaxShiftsPerWeek { get; set; }
    public string? AdminNotes { get; set; }
}
