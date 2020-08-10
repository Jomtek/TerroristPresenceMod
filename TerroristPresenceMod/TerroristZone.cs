using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using TerroristPresenceMod.Utils;

namespace TerroristPresenceMod
{
    class TerroristZone
    {
        public Vector3 ZonePos { get; }
        public string GroupName { get; }

        public List<Ped> Terrorists = new List<Ped>();
        public int TerroristsAmount;
        public FighterConfiguration FighterCfg;
        private List<Ped> DeadTerrorists = new List<Ped>();
        private int SpawnRadius;
        private bool SpawnOnStreet;
        private float BlipScale;

        private Blip ZoneBlip;
        public bool Spawned = false;
        public bool Inactive = false;
        public bool Dangerous = false;
        public bool Capture = false;

        public bool IsReclaimable = true;
        public FighterConfiguration ReclaimersCfg;

        private int ZoneFarLimit;
        private int ZoneNearLimit;

        private int TicksSinceZoneReclaim = 0;
        private bool RegisterTicks = false;

        public TerroristZone(
            Vector3 zonePos, int terroristsAmount,
            string groupName, FighterConfiguration fighterCfg,
            int spawnRadius, bool spawnOnStreet = true, bool isReclaimable = true,
            FighterConfiguration reclaimersCfg = null, float blipScale = 2.5f,
            int zoneFarLimit = 500, int zoneNearLimit = 350)
        {
            ZonePos = zonePos;
            TerroristsAmount = terroristsAmount;
            GroupName = groupName;
            FighterCfg = fighterCfg;
            SpawnRadius = spawnRadius;
            SpawnOnStreet = spawnOnStreet;
            BlipScale = blipScale;
            ZoneFarLimit = zoneFarLimit;
            ZoneNearLimit = zoneNearLimit;

            IsReclaimable = isReclaimable;

            if (reclaimersCfg == null)
                ReclaimersCfg = fighterCfg;
            else
                ReclaimersCfg = reclaimersCfg;
        }

        public void InitBlip(bool declare = true)
        {
            if (declare) ZoneBlip = World.CreateBlip(ZonePos);
            ZoneBlip.Name = "Terrorist Zone - " + GroupName;
            ZoneBlip.Color = BlipColor.RedDark2;
            ZoneBlip.Scale = BlipScale;
            ZoneBlip.IsShortRange = false;
        }

        public void MarkBlipAsDangerous()
        {
            ZoneBlip.Name = "Dangerous Zone - " + GroupName;
            ZoneBlip.Color = BlipColor.Orange; 
            ZoneBlip.Scale = BlipScale * 1.2f;
            ZoneBlip.IsFlashing = true;
        }

        public bool IsPlayerNearZone() =>
            Game.Player.Character.Position.DistanceTo(ZonePos) < ZoneNearLimit;
        public bool IsPlayerFarFromZone() =>
            Game.Player.Character.Position.DistanceTo(ZonePos) > ZoneFarLimit;

        public void DeleteTerrorists()
        {
            foreach (Ped terrorist in Terrorists)
            {
                terrorist.AttachedBlip.Delete();
                terrorist.Delete();
            }

            Terrorists.Clear();
            Spawned = false;
        }

        public void ManageDeadTerrorists()
        {
            var removedTerrorists = new List<Ped>();

            foreach (Ped terrorist in Terrorists)
                if (!terrorist.IsAlive)
                {
                    if (terrorist.AttachedBlip != null) terrorist.AttachedBlip.Delete();
                    removedTerrorists.Add(terrorist);
                }

            foreach (Ped terrorist in removedTerrorists)
            {
                Terrorists.Remove(terrorist);
                DeadTerrorists.Add(terrorist);
            }

            if (Terrorists.Count == 0)
            {
                Screen.ShowSubtitle("Congratulations - " + GroupName + " (" + TerroristsAmount + " soldiers) defeated", 15000);

                ZoneBlip.Color = BlipColor.GreenDark;
                ZoneBlip.IsFlashing = false;
                ZoneBlip.ShowRoute = false;
                ZoneBlip.Scale = 2.5f;
                ZoneBlip.Name = "Safe Zone - " + GroupName;
                Dangerous = false;
                Inactive = true;
                Capture = false;
            }
        }

        public int ClearDeadEntities()
        {
            int deletedEntities = DeadTerrorists.Count;
            foreach (Ped terrorist in DeadTerrorists) terrorist.Delete();
            DeadTerrorists.Clear();

            return deletedEntities;
        }

        public void SpawnTerrorist()
        {
            Vector3 spawnPos;
            if (SpawnOnStreet)
                spawnPos = World.GetNextPositionOnStreet(ZonePos.Around(SpawnRadius));
            else
                spawnPos = World.GetNextPositionOnSidewalk(ZonePos.Around(SpawnRadius));

            Ped ped = World.CreatePed(
                FighterCfg.PedHash,
                spawnPos
            );

            WeaponHash weapon;
            if (FighterCfg.Weapon == WeaponHash.Unarmed)
                weapon = GlobalInfo.weaponsList[GlobalInfo.generalRandomInstance.Next(0, GlobalInfo.weaponsList.Count)];
            else
                weapon = FighterCfg.Weapon;

            ped.Weapons.Give(weapon, -1, true, true);
            ped.Accuracy = 35;
            ped.MaxHealth = 200;
            ped.Health = 200;
            ped.Armor = 100;
            ped.Task.WanderAround();
            ped.RelationshipGroup = GlobalInfo.RELATIONSHIP_TERRORIST;

            Function.Call(Hash.SET_PED_HEARING_RANGE, ped, 3000f);
            Function.Call(Hash.SET_PED_SEEING_RANGE, ped, 3000f);

            ped.AddBlip();
            if (ped.AttachedBlip != null)
                ped.AttachedBlip.Color = BlipColor.GreyDark;

            Terrorists.Add(ped);
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
                    SpawnTerrorist();
            } catch (Exception ex)
            {   
                Notification.Show(ex.StackTrace);
            }
        }

        public void DeleteTerrorist(Ped terrorist)
        {
            terrorist.AttachedBlip.Delete();
            terrorist.Delete();
            Terrorists.Remove(terrorist);
        }

        public void ZoneReclaimedTick()
        {
            if (!Dangerous && Inactive && GlobalInfo.generalRandomInstance.Next(0, 50) == 0)
            {
                MarkBlipAsDangerous();
                Spawned = false;
                Inactive = false;
                Dangerous = true;

                if (Convert.ToInt32(TerroristsAmount * 0.8) > 5)
                    TerroristsAmount = Convert.ToInt32(TerroristsAmount * 0.8);
                else
                    TerroristsAmount = 5;

                FighterCfg = ReclaimersCfg;
                Screen.ShowSubtitle(GroupName + " zone is being reclaimed !", 8000);

                RegisterTicks = true;
            }
        }   

        public bool ZoneLostTick()
        {
            if (RegisterTicks)
                TicksSinceZoneReclaim++;

            if (Dangerous && !Capture && TicksSinceZoneReclaim == 2)
            {
                InitBlip(false);
                Inactive = false;
                Spawned = false;
                Dangerous = false;

                if (Convert.ToInt32(TerroristsAmount * 1.4) <= 100)
                    TerroristsAmount = Convert.ToInt32(TerroristsAmount * 1.4);
                else
                    TerroristsAmount = 100;

                TicksSinceZoneReclaim = 0;
                RegisterTicks = false;

                Screen.ShowSubtitle(GroupName + " zone was lost - terrorists are now stronger !", 10000);
                return true;
            }

            return false;
        }
    }   
}
