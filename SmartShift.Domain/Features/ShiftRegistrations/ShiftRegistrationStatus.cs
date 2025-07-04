using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    namespace SmartShift.Domain.Features.ShiftRegistrations
    {
        public enum ShiftRegistrationStatus
        {
            Pending,    // ממתין לאישור
            Approved,   // אושר 
            Rejected,   // נדחה
            Cancelled   // בוטל על ידי העובד
        }
    }

