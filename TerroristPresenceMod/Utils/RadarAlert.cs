using GTA.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerroristPresenceMod.Utils
{
    public static class RadarAlert
    {
        public static void EnteringZone(string zoneName)
        {
            Notification.Show(
                NotificationIcon.DetonateBomb,
                " RADAR",
                "MESSAGE",
                $"Entering a zone controlled by {zoneName} terrorists",
                false,
                true
            );
        }

        public static void LeavingZone(string zoneName)
        {
            Notification.Show(
                NotificationIcon.Default,
                " RADAR",
                "MESSAGE",
                $"Leaving the {zoneName} zone",
                false,
                true
            );
        }
    }
}
