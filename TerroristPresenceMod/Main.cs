using System;
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
            doc.Load("scripts/TerroristPresenceMod.xml");

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
                zone.InitBlip();

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
                        clearedEntities += zone.ClearDeadEntities();

                if (clearedEntities > 0)
                    Notification.Show(clearedEntities + " dead entities cleared");

                foreach (TerroristZone zone in terroristZones)
                    if (zone.Spawned)
                        for (int i = 0; i < zone.Terrorists.Count; i++)
                        {
                            Ped terrorist = zone.Terrorists[i];

                            if ((!terrorist.IsWalking && !terrorist.IsInCombat && !terrorist.IsInCover) || !terrorist.IsVisible)
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
        
        private int zonesReclaimedDelay = 4500;
        private int zonesLostDelay = 10000;

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
                                z.ClearDeadEntities();

                            GTA.UI.Screen.ShowSubtitle("Radar message - Entering a zone controlled by " + zone.GroupName + " terrorists");
                            zone.SpawnTerrorists();
                            zone.Capture = true;

                            Notification.Show("! We've been informed that terrorists are near your position !", true);
                            break;
                        }
                    }
                    else if (zone.IsPlayerFarFromZone())
                    {
                        zone.DeleteTerrorists();
                        zone.ClearDeadEntities();
                        zone.Capture = false;
                        
                        GTA.UI.Screen.ShowSubtitle("Radar message - Leaving " + zone.GroupName + " zone");
                        break;
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
                        if (zone.Spawned && !zone.Inactive)
                            for (int k = 0; k < zone.Terrorists.Count; k++)
                            {
                                Ped terrorist = zone.Terrorists[k];
                                if (terrorist.Position.X == 0)
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

                zonesReclaimedDelay = 375;
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

                zonesLostDelay = 8000;
            } else
            {
                zonesLostDelay--;
            }
        }
    }
}