using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans
{
    public static class FixtureExtensions
    {        
        public static void ClearReminders(this MockFixture fx) {
            fx.Reminders.CancelAll();
        }





    }
}
