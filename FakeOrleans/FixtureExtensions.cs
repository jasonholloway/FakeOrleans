using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOrleans
{
    public static class FixtureExtensions
    {        
        public static void ClearReminders(this Fixture fx) {
            fx.Reminders.CancelAll();
        }





    }
}
