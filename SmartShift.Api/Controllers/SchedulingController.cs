    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;

    namespace SmartShift.Api.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class SchedulingController : ControllerBase
    {
        [HttpGet("employees")]
        public IActionResult GetEmployees()
        {
            Console.WriteLine("GetEmployees called");
            var employees = new[]
            {
                new { id = "1", name = "ישראל ישראלי", priorityRating = 3, email = "israel@example.com", phoneNumber = "050-1234567" },
                new { id = "2", name = "שרה כהן", priorityRating = 4, email = "sarah@example.com", phoneNumber = "050-2345678" },
                new { id = "3", name = "דוד לוי", priorityRating = 2, email = "david@example.com", phoneNumber = "050-3456789" }
            };
            return Ok(employees);
        }

        [HttpGet("shifts")]
        public IActionResult GetShifts([FromQuery] string startDate, [FromQuery] string endDate)
        {
            var shifts = new[]
            {
                new { 
                    id = "1", 
                    startTime = DateTime.Now.AddHours(1), 
                    endTime = DateTime.Now.AddHours(9),
                    requiredPriorityRating = 3,
                    assignedEmployeeId = "1",
                    status = "Assigned"
                },
                new { 
                    id = "2", 
                    startTime = DateTime.Now.AddDays(1), 
                    endTime = DateTime.Now.AddDays(1).AddHours(8),
                    requiredPriorityRating = 2,
                    assignedEmployeeId = "2",
                    status = "Assigned"
                }
            };
            return Ok(shifts);
        }
    } 