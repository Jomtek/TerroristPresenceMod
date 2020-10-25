using System;
using System.IO;
using System.Collections.Generic;
using GTA;
using GTA.UI;
using GTA.Math;
using System.Windows.Forms;
using TerroristPresenceMod.Utils;
using System.Xml;

namespace TerroristPresenceMod
{
    public class Main : Script
    {
        private List<TerroristZone> terroristZones = new List<TerroristZone>();

        public Main()
        {
            Notification.Show("Terrorist Presence Mod (v1.30) (by Jomtek)");

            foreach (Blip blip in World.GetAllBlips())
                if (blip.Color == BlipColor.RedDark2 ||
                    blip.Color == BlipColor.GreenDark ||
                    blip.Color == BlipColor.Orange   ||
                    blip.Color == BlipColor.GreyDark)
                {
                    blip.Delete();
                }


            GlobalInfo.RELATIONSHIP_TERRORIST = World.AddRelationshipGroup("TERRORIST");
            GlobalInfo.RELATIONSHIP_ZOMBIE = World.AddRelationshipGroup("Zombie");
            GlobalInfo.RELATIONSHIP_HOSTILE = World.AddRelationshipGroup("Hostile");
            RelationshipSetter.SetRelationships();

            XmlDocument doc = new XmlDocument();
            doc.Load("scripts/TPM/TerroristZones.xml");

            if (!File.Exists("scripts/TPM/CapturedZones.txt"))
            {
                GTA.UI.Screen.ShowSubtitle("[TerroristPresence] Missing file CapturedZones.txt");
                return;
            }
            else
            {
                string[] capturedZonesNames = File.ReadAllLines("scripts/TPM/CapturedZones.txt");
                GlobalInfo.CapturedZonesNames = new List<string>(capturedZonesNames);
            }

            var encounteredNames = new List<string>();
            var encounteredPositions = new List<Vector3>();

            try
            {
                foreach (XmlNode zoneNode in doc.DocumentElement.ChildNodes)
                    terroristZones.Add(XmlParser.ParseZoneConfiguration(zoneNode.ChildNodes, ref encounteredNames, ref encounteredPositions));
            } catch (XmlParsingException ex)
            {
                GTA.UI.Screen.ShowSubtitle("[TerroristPresence] Invalid XML file : " + ex.Message, 10000);
                return;
            }

            foreach (TerroristZone zone in terroristZones)
            {
                zone.InitBlip();
                if (GlobalInfo.CapturedZonesNames.Contains(zone.GroupName))
                {
                    zone.SetCaptured(true);
                }
            }

            Tick += OnTick;
            KeyDown += OnKeyDown;
        }   

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.H)
            {
                int clearedEntities = 0;
                foreach (TerroristZone zone in terroristZones)
                    if (zone.Spawned)
                        clearedEntities += zone.WipeGTAMemory();

                if (clearedEntities > 0)
                    Notification.Show(clearedEntities + " entities removed from memory");

                foreach (TerroristZone zone in terroristZones)
                    if (zone.Spawned)
                        for (int i = 0; i < zone.Terrorists.Count; i++)
                        {
                            var terrorist = zone.Terrorists[i];

                            if ((!terrorist.Ped.IsWalking &&
                                !terrorist.Ped.IsInVehicle() &&
                                !terrorist.Ped.IsInCombat &&
                                !terrorist.Ped.IsInCover) || !terrorist.Ped.IsVisible)
                            {
                                zone.DeleteTerrorist(terrorist);
                                zone.SpawnTerrorist();
                            }
                        }
            } else if (e.KeyCode == Keys.K)
            {
                Notification.Show("X: " + ((int)Game.Player.Character.Position.X).ToString() +
                    " | Y: " + ((int)Game.Player.Character.Position.Y).ToString() +
                    " | Z: " + ((int)Game.Player.Character.Position.Z).ToString());
            }
        }

        private int spawnZonesDelay = 1000;
        private int deadSoldiersManageDelay = 50;
        private int soldiersPositionFixDelay = 250;
        
        private int zonesReclaimedDelay = 14000;
        private int zonesLostDelay = 15000;

        private void OnTick(object sender, EventArgs e)
        {
            if (spawnZonesDelay == 0)
            {
                foreach (TerroristZone zone in terroristZones)
                {
                    if (zone.Inactive) continue;

                    if (!zone.Spawned)
                    {
                        if (zone.IsPlayerNearZone())
                        {
                            foreach (TerroristZone z in terroristZones)
                                z.WipeGTAMemory();

                            RadarAlert.EnteringZone(zone.GroupName);
                            zone.SpawnTerrorists();
                            zone.Capture = true;

                            //break;
                        }
                    } else if (zone.IsPlayerFarFromZone())
                    {
                        zone.DeleteTerrorists();
                        zone.WipeGTAMemory();
                        zone.Capture = false;

                        RadarAlert.LeavingZone(zone.GroupName);
                        //break;
                    } else
                    {
                        foreach (var vehicle in zone.Vehicles)
                        {
                            if (Game.Player.Character.Position.DistanceTo(vehicle.Position) > 100)
                            {
                                vehicle.Driver.Task.DriveTo(
                                    vehicle,
                                    zone.ZonePos,
                                    5,
                                    10,
                                    DrivingStyle.Rushed
                                );
                            }
                        }
                    }
                }   

                spawnZonesDelay = 250;
            }
            else
            {
                spawnZonesDelay--;
            }

            if (deadSoldiersManageDelay == 0)
            {
                foreach (TerroristZone zone in terroristZones)
                    if (zone.Spawned && !zone.Inactive)
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
                        if (zone.Spawned && !zone.Inactive && !zone.ReinforcementsSent)
                            for (int k = 0; k < zone.Terrorists.Count; k++)
                            {
                                var terrorist = zone.Terrorists[k];
                                if (terrorist.Ped.Position.X == 0)
                                {
                                    zone.DeleteTerrorists();
                                    break;  
                                }
                            }
                    }

                    soldiersPositionFixDelay = 250;
                }
                else
                {
                    soldiersPositionFixDelay--;
                }
            } catch (Exception ex)
            {
                GTA.UI.Notification.Show(ex.ToString());
            }

            if (zonesReclaimedDelay == 0)
            {
                foreach (TerroristZone zone in terroristZones)
                    if (zone.IsReclaimable)
                        zone.ZoneReclaimedTick();

                zonesReclaimedDelay = 14000;
            } else
            {
                zonesReclaimedDelay--;
            }

            if (zonesLostDelay == 0)
            {
                foreach (TerroristZone zone in terroristZones)
                    if (zone.IsReclaimable)
                        if (zone.ZoneLostTick())
                            break;

                zonesLostDelay = 15000;
            } else
            {
                zonesLostDelay--;
            }
        }
    }
}