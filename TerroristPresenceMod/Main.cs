using System;
using System.Collections.Generic;
using GTA;
using GTA.UI;
using GTA.Math;
using System.Windows.Forms;
using System.Linq.Expressions;
using System.ComponentModel;
using TerroristPresenceMod.Utils;
using GTA.Native;
using System.Xml;
using System.IO;

namespace TerroristPresenceMod
{
    public class Main : Script
    {
        private List<TerroristZone> terroristZones = new List<TerroristZone>();

        public Main()
        {
            Notification.Show("Terrorist Presence Mod (v1.20) (by Jomtek)");

            foreach (Blip blip in World.GetAllBlips())
                if (blip.Color == BlipColor.RedDark2 ||
                    blip.Color == BlipColor.GreenDark ||
                    blip.Color == BlipColor.Orange   ||
                    blip.Color == BlipColor.GreyDark)
                {
                    blip.Delete();
                }

            GlobalInfo.RELATIONSHIP_TERRORIST = World.AddRelationshipGroup("TERRORIST");
            RelationshipSetter.SetRelationships();
            
            XmlDocument doc = new XmlDocument();
            doc.Load("scripts/TerroristPresenceMod.xml");

            var encounteredNames = new List<string>();
            var encounteredPositions = new List<Vector3>();

            try
            {
                foreach (XmlNode zoneNode in doc.DocumentElement.ChildNodes)
                    terroristZones.Add(XmlParser.parseZoneConfiguration(zoneNode.ChildNodes, ref encounteredNames, ref encounteredPositions));
            } catch (XmlParserException ex)
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
                    if (zone.spawned)
                        clearedEntities += zone.ClearDeadEntities();

                if (clearedEntities > 0)
                    GTA.UI.Notification.Show(clearedEntities + " dead entities cleared");

                foreach (TerroristZone zone in terroristZones)
                    if (zone.spawned)
                        for (int i = 0; i < zone.terrorists.Count; i++)
                        {
                            Ped terrorist = zone.terrorists[i];

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
            ScreenEffects.Tick();

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
                            zone.capture = true;

                            Notification.Show("! We've been informed that terrorists are near your position !", true);
                            break;
                        }
                    }
                    else if (zone.IsPlayerFarFromZone())
                    {
                        zone.DeleteTerrorists();
                        zone.ClearDeadEntities();
                        zone.capture = false;
                        
                        GTA.UI.Screen.ShowSubtitle("Radar message - Leaving " + zone.groupName + " zone");
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
                    if (zone.isReclaimable)
                        zone.ZoneReclaimedTick();

                zonesReclaimedDelay = 375;
            } else
            {
                zonesReclaimedDelay--;
            }

            if (zonesLostDelay == 0)
            {
                foreach (TerroristZone zone in terroristZones)
                    if (zone.isReclaimable)
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
