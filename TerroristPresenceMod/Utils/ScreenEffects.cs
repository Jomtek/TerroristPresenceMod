using GTA;
using GTA.UI;

namespace TerroristPresenceMod.Utils
{
    class ScreenEffects
    {
        private static bool stopEffects = false;
        private static int stopEffectsDelay = 200;

        public static void Tick()
        {
            if (stopEffects)
            {
                if (stopEffectsDelay == 0)
                {
                    GTA.UI.Screen.StopEffects();
                    stopEffects = false;
                    stopEffectsDelay = 200;
                }
                else
                {
                    stopEffectsDelay--;
                }
            }
        }

        public static void ZoneCaptured()
        {
            GTA.UI.Screen.StartEffect(ScreenEffect.HeistCelebEnd);
            stopEffects = true;
        }

        public static void ZoneReclaimed()
        {
            GTA.UI.Screen.StartEffect(ScreenEffect.SwitchSceneFranklin, 10);
            stopEffects = true;
        }

        public static void ZoneLost()
        {
            GTA.UI.Screen.StartEffect(ScreenEffect.MinigameEndTrevor, 10);
            stopEffects = true;
        }
    }
}