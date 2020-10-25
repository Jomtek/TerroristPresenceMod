using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Security.Principal;
using TerroristPresenceMod.Utils;

namespace TerroristPresenceMod
{
    class TerroristZone
    {
        public Vector3 ZonePos { get; }
        public string GroupName { get; }

        public bool IsReclaimable = true;

        // Troop info
        public List<Terrorist> Terrorists = new List<Terrorist>();
        public List<Vehicle> Vehicles = new List<Vehicle>();
        private List<Terrorist> _DeadTerrorists = new List<Terrorist>();

        // Spawn info
        public FighterConfiguration FighterCfg;
        public FighterConfiguration ReclaimerCfg;
        private int _SpawnRadius;
        private bool _SpawnOnStreet;

        // Zone state info
        public int TerroristsAmount;
        public bool Spawned = false;
        public bool Inactive = false;
        public bool Dangerous = false;
        public bool Capture = false;
        public bool ReinforcementsSent = false;

        // Blip info
        public Blip ZoneBlip;
        public BlipColor CurrentSoldierBlipColor = BlipColor.GreyDark;

        // Ticks
        private int _TicksSinceZoneReclaim = 0;
        private bool _RegisterTicks = false;

        // / / / //

        public TerroristZone(
            Vector3 zonePos, int terroristsAmount,
            string groupName, FighterConfiguration fighterCfg,
            int spawnRadius, bool spawnOnStreet = true, bool isReclaimable = true,
            FighterConfiguration reclaimersCfg = null)
        {
            ZonePos = zonePos;
            TerroristsAmount = terroristsAmount;
            GroupName = groupName;
            FighterCfg = fighterCfg;
            _SpawnRadius = spawnRadius;
            _SpawnOnStreet = spawnOnStreet;

            IsReclaimable = isReclaimable;

            if (reclaimersCfg == null)
                ReclaimerCfg = fighterCfg;
            else
                ReclaimerCfg = reclaimersCfg;
        }

        public bool IsPlayerNearZone() =>
            Game.Player.Character.Position.DistanceTo(ZonePos) < 330;
        public bool IsPlayerFarFromZone() =>
            Game.Player.Character.Position.DistanceTo(ZonePos) > 675;

        #region ZoneBlip
        public void InitBlip(bool init = true)
        {
            if (init) ZoneBlip = World.CreateBlip(ZonePos);
            ZoneBlip.Name = "Terrorist Zone - " + GroupName;
            ZoneBlip.Color = BlipColor.RedDark2;
            ZoneBlip.Scale = 2.5f;
            ZoneBlip.IsShortRange = false;
        }

        public void MarkBlipAsDangerous()
        {
            ZoneBlip.Name = "Dangerous Zone - " + GroupName;
            ZoneBlip.Color = BlipColor.Orange; 
            ZoneBlip.Scale = 3.5f;
            ZoneBlip.IsFlashing = true;
        }
        #endregion

        #region Spawning

        public void SpawnTerrorist()
        {
            Vector3 spawnPos;
            if (_SpawnOnStreet)
                spawnPos = World.GetNextPositionOnStreet(ZonePos.Around(_SpawnRadius));
            else
                spawnPos = World.GetNextPositionOnSidewalk(ZonePos.Around(_SpawnRadius));

            var terrorist = new Terrorist(FighterCfg, CurrentSoldierBlipColor);
            terrorist.Spawn(spawnPos);
            terrorist.Configure();
            Terrorists.Add(terrorist);
        }

        public void SpawnTerrorists()
        {
            if (!Spawned)
                Spawned = true;
            else
                throw new Exception("Group already spawned");

            try
            {
                for (int i = 1; i < TerroristsAmount; i++)
                {
                    SpawnTerrorist();            
                }
            } catch (Exception ex)
            {   
                Notification.Show(ex.StackTrace);
            }
        }

        private void SpawnReinforcements()
        {
            WipeGTAMemory();
            for (int i = 0; i < 1; i++)
            {
                var fighters = new List<Terrorist>();

                foreach (VehicleHash hash in new VehicleHash[2] {
                        VehicleHash.Technical,
                        VehicleHash.Technical,
                        //VehicleHash.Defiler
                    })
                {
                    // Configure vehicule
                    Vector3 vehiclePos = World.GetNextPositionOnStreet(ZonePos.Around(550));

                    while (World.CalculateTravelDistance(ZonePos, vehiclePos) > 650)
                    {
                        vehiclePos = World.GetNextPositionOnStreet(ZonePos.Around(550));
                    }

                    Vehicle vehicle =
                        World.CreateVehicle(hash, vehiclePos);

                    Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, vehicle, 4);
                    vehicle.IsPersistent = true;
                    vehicle.Heading = (Game.Player.Character.Position - vehicle.Position).ToHeading();
                    Vehicles.Add(vehicle);

                    // Spawn driver
                    var driver = new Terrorist(FighterCfg, CurrentSoldierBlipColor);
                    driver.Ped = vehicle.CreatePedOnSeat(VehicleSeat.Driver, FighterCfg.PedHash);
                    driver.Configure(false);

                    // Spawn passengers
                    if (hash != VehicleHash.Defiler)
                    {
                        foreach (VehicleSeat seat in new VehicleSeat[2] {
                            VehicleSeat.RightFront,
                            VehicleSeat.LeftRear
                        })
                        {
                            var passenger = new Terrorist(FighterCfg, CurrentSoldierBlipColor);
                            passenger.Ped = vehicle.CreatePedOnSeat(seat, FighterCfg.PedHash);
                            passenger.Configure();

                            Terrorists.Add(passenger);
                            fighters.Add(passenger);
                        }
                    }

                    //if (Game.Player.Character.IsInVehicle() ||
                    //    Game.Player.Character.Position.DistanceTo(vehicle.Position) > 50)
                    //{
                        driver.Ped.Task.DriveTo(vehicle, ZonePos, 5, 10, DrivingStyle.Rushed);
                        foreach (Terrorist fighter in fighters)
                            fighter.Ped.Task.VehicleShootAtPed(Game.Player.Character);
                    //}
                    /*else // TODO
                    {
                        driver.Ped.Task.FightAgainst(Game.Player.Character);
                        foreach (Terrorist fighter in fighters)
                            fighter.Ped.Task.FightAgainst(Game.Player.Character);
                    }*/

                    Terrorists.Add(driver);
                }
            }
            ReinforcementsSent = true;
        }
        #endregion

        #region Deletion
        public void DeleteTerrorist(Terrorist terrorist)
        {
            Terrorists.Remove(terrorist);
            terrorist.Delete();
        }

        public void DeleteTerrorists()
        {
            //foreach (var terrorist in Terrorists)
            //    terrorist.Delete();
            //Terrorists.Clear();

            foreach (var terrorist in Terrorists)
            {
                terrorist.Ped.AttachedBlip.Delete();
                terrorist.Ped.Delete();
            }

            Terrorists.Clear();

            Spawned = false;
        }

        public int WipeGTAMemory()
        {
            int releasedEntities =
                _DeadTerrorists.Count + Vehicles.Count;

            foreach (Vehicle vehicle in Vehicles)
                vehicle.MarkAsNoLongerNeeded();

            // Terrorist peds are automatically wiped

            _DeadTerrorists.Clear();
            Vehicles.Clear();

            foreach (var entity in World.GetNearbyEntities(Game.Player.Character.Position, 250))
            {
                if (entity is Ped)
                {
                    var ped = (Ped)entity;
                    if (!ped.IsAlive)
                    {
                        ped.MarkAsNoLongerNeeded();
                        releasedEntities ++;
                    }
                }
            }

            return releasedEntities;
        }
        #endregion

        public void SetCaptured(bool fromConfigFile = false)
        {
            ZoneBlip.Color = BlipColor.GreenDark;
            ZoneBlip.IsFlashing = false;
            ZoneBlip.Scale = 2.25f;
            ZoneBlip.Name = "Safe Zone - " + GroupName;

            Dangerous = false;
            Inactive = true;
            Capture = false;
        
            if (!fromConfigFile)
            {
                GlobalInfo.CapturedZonesNames.Add(GroupName);
                GlobalInfo.SaveCapturedZones();
            }
        }

        public void ManageDeadTerrorists()
        {
            var removedTerrorists = new List<Terrorist>();

            foreach (var terrorist in Terrorists)
                if (!terrorist.Ped.IsAlive)
                {
                    if (terrorist.Ped.AttachedBlip != null) terrorist.Ped.AttachedBlip.Delete();
                    removedTerrorists.Add(terrorist);
                }

            foreach (var terrorist in removedTerrorists)
            {
                Terrorists.Remove(terrorist);
                _DeadTerrorists.Add(terrorist);
                terrorist.Ped.MarkAsNoLongerNeeded();
            }

            /*if (Terrorists.Count < 5 && TerroristsAmount >= 5 && !ReinforcementsSent)
            {
                CurrentSoldierBlipColor = BlipColor.Orange;
                ZoneBlip.IsFlashing = true;
                Screen.ShowSubtitle("Reinforcements incoming !", 5000);
                SpawnReinforcements();
                return;
            }*/

            if (Terrorists.Count == 0)
            {
                if (ReinforcementsSent)
                    CurrentSoldierBlipColor = BlipColor.GreyDark;

                Screen.ShowSubtitle("Congratulations - " + GroupName + " (" + TerroristsAmount + " soldiers) defeated", 15000);
                SetCaptured();
            }
        }

        #region Ticks
        public void ZoneReclaimedTick()
        {
            if (!Dangerous && Inactive && GlobalInfo.GeneralRandomInstance.Next(0, 50) == 0)
            {
                MarkBlipAsDangerous();
                Spawned = false;
                Inactive = false;
                Dangerous = true;

                if (Convert.ToInt32(TerroristsAmount * 0.8) > 5)
                    TerroristsAmount = Convert.ToInt32(TerroristsAmount * 0.8);
                else
                    TerroristsAmount = 5;

                FighterCfg = ReclaimerCfg;
                _RegisterTicks = true;

                GlobalInfo.CapturedZonesNames.Remove(GroupName);
                GlobalInfo.SaveCapturedZones();

                Screen.ShowSubtitle(GroupName + " zone is being reclaimed !", 8000);
            }
        }   

        public bool ZoneLostTick()
        {
            if (_RegisterTicks)
                _TicksSinceZoneReclaim++;

            if (Dangerous && !Capture && _TicksSinceZoneReclaim == 2)
            {
                InitBlip(false);
                Inactive = false;
                Spawned = false;
                Dangerous = false;

                if (Convert.ToInt32(TerroristsAmount * 1.5) <= 100)
                    TerroristsAmount = Convert.ToInt32(TerroristsAmount * 1.5);
                else
                    TerroristsAmount = 100;

                _TicksSinceZoneReclaim = 0;
                _RegisterTicks = false;
                GlobalInfo.CapturedZonesNames.Remove(GroupName);
                GlobalInfo.SaveCapturedZones();

                Screen.ShowSubtitle(GroupName + " zone was lost - terrorists are now stronger !", 10000);
                return true;
            }

            return false;
        }
        #endregion
    }   
}