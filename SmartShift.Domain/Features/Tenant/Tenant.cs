using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using System;
using System.Collections.Generic;

namespace SmartShift.Domain.Data;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid(); 
    public string Name { get; set; }   = string.Empty;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
