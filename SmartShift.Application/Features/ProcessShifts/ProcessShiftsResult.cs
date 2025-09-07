namespace SmartShift.Application.Features.ProcessShifts
{
    public class ProcessShiftsResult
    {
        public List<object> Results { get; set; } = new();
        public string? Message { get; set; }
    }
}
