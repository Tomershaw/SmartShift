using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShift.Domain.Features.Employees
{
    /// <summary>
    /// זמינות עובד למשמרות לפי שעות
    /// </summary>
    public enum EmployeeShiftAvailability
    {
        /// <summary>
        /// זמין רק למשמרות ערב (18:30)
        /// </summary>
        Regular = 1,

        /// <summary>
        /// זמין למשמרות הקמה (04:30) וגם ערב (18:30)
        /// המשמעות: בחר הקמה אז אוטומטית זמין גם לערב
        /// </summary>
        Early = 2
    }
}
