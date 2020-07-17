﻿using System;
using System.Collections.Generic;
using GTA;
using GTA.UI;
using GTA.Math;
using System.Windows.Forms;
using System.Linq.Expressions;

namespace TerroristPresenceMod
{
    public class Main : Script
    {
        private List<TerroristZone> terroristZones = new List<TerroristZone>() {
            new TerroristZone(
                new Vector3(1354, 3228, 53), 45, "The Desert Fighters",
                new FighterConfiguration(PedHash.Blackops03SMY, WeaponHash.AssaultRifle), 50, false, 3f, 600, 450
            ),
            new TerroristZone(
                new Vector3(1610, 1852, 103), 45, "Mountain Lovers",
                new FighterConfiguration(PedHash.CrisFormageCutscene, WeaponHash.SniperRifle), 60, false, 3f
            ),
            new TerroristZone(
                new Vector3(2257, 1497, 69), 65, "The Ghosts",
                new FighterConfiguration(PedHash.Marine03SMY, WeaponHash.Unarmed), 30, false, 3.5f, 700, 400
            ),
            new TerroristZone(
                new Vector3(2868, 2254, 140), 15, "Anti-Air Fighters",
                new FighterConfiguration(PedHash.Blackops01SMY, WeaponHash.HomingLauncher), 5, false, 2f
            ),
            new TerroristZone(
                new Vector3(3518, 3797, 30), 75, "Humane Occupiers",
                new FighterConfiguration(PedHash.Marine03SMY, WeaponHash.SpecialCarbine), 40, false, 4f, 700, 500
            ),
            new TerroristZone(
                new Vector3(2947, 5325, 101), 40, "Young Hikers",
                new FighterConfiguration(PedHash.Hippy01AMY, WeaponHash.Musket), 50, false, 3f
            ),
            new TerroristZone(
                new Vector3(2624, 6268, 130), 7, "Old Hikers",
                new FighterConfiguration(PedHash.Hippy01AMY, WeaponHash.Musket), 5, false, 2f
            ),
            new TerroristZone(
                new Vector3(2788, 3392, 55), 10, "Road-Emergency",
                new FighterConfiguration(PedHash.Blackops01SMY, WeaponHash.CombatPistol), 5, false, 2.7f, 800, 600
            ),
            new TerroristZone(
                new Vector3(-1541, 1383, 125), 7, "The River Mans",
                new FighterConfiguration(PedHash.Paparazzi, WeaponHash.MiniSMG), 5, false, 2f, 800, 600
            )
        };

        public Main()
        {
            Notification.Show("Terrorist Presence Mod (v1.11) (by Jomtek)");
                    
            foreach (Blip blip in World.GetAllBlips())
                if (blip.Color == BlipColor.RedDark2 || blip.Color == BlipColor.GreenDark || blip.Color == BlipColor.GreyDark)
                    blip.Delete();

            foreach (TerroristZone zone in terroristZones)
                zone.InitBlip();

            GlobalInfo.RELATIONSHIP_TERRORIST = World.AddRelationshipGroup("TERRORIST");
            Utils.SetRelationships();

            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.H)
            {
                foreach (TerroristZone zone in terroristZones)
                    if (zone.spawned)
                        zone.ClearDeadEntities();
            }
        }

        private int spawnZonesDelay = 1000;
        private int deadSoldiersManageDelay = 50;
        private int soldiersPositionFixDelay = 500;

        private void OnTick(object sender, EventArgs e)
        {
            if (spawnZonesDelay == 0)
            {
                foreach (TerroristZone zone in terroristZones)
                {
                    if (zone.inactive) continue;

                    if (!zone.spawned)
                    {
                        if (zone.IsPlayerNearZone())
                        {
                            foreach (TerroristZone z in terroristZones)
                                z.ClearDeadEntities();

                            GTA.UI.Screen.ShowSubtitle("Radar message - Entering a zone controlled by " + zone.groupName + " terrorists");
                            zone.SpawnTerrorists();

                            Notification.Show("! We've been informed that terrorists are near your position !", true);
                            break;
                        }
                    }
                    else if (zone.IsPlayerFarFromZone())
                    {
                        zone.DeleteTerrorists();
                        GTA.UI.Screen.ShowSubtitle("Radar message - Leaving " + zone.groupName + " zone");
                        break;
                    }
                }

                spawnZonesDelay = 350;
            }
            else
            {
                spawnZonesDelay--;
            }

            if (deadSoldiersManageDelay == 0)
            {
                foreach (TerroristZone zone in terroristZones)
                    if (zone.spawned && !zone.inactive)
                        zone.ManageDeadTerrorists();

                deadSoldiersManageDelay = 50;
            }
            else
            {
                deadSoldiersManageDelay--;
            }

            try
            {
                if (soldiersPositionFixDelay == 0)
                {
                    for (int i = 0; i < terroristZones.Count; i++)
                    {
                        TerroristZone zone = terroristZones[i];
                        if (zone.spawned && !zone.inactive)
                            for (int k = 0; k < zone.terrorists.Count; k++)
                            {
                                Ped terrorist = zone.terrorists[k];
                                if (terrorist.Position.X == 0)
                                {
                                    zone.DeleteTerrorists();
                                    break;  
                                }
                            }
                    }

                    soldiersPositionFixDelay = 500;
                }
                else
                {
                    soldiersPositionFixDelay--;
                }
            } catch (Exception ex)
            {
                GTA.UI.Notification.Show(ex.ToString());
            }
        }
    }
}
