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
        public Vector3 zonePos { get; }
        public string groupName { get; }

        public List<Ped> terrorists = new List<Ped>();
        public int terroristsAmount;
        public FighterConfiguration fighterCfg;
        private List<Ped> deadTerrorists = new List<Ped>();
        private int spawnRadius;
        private bool spawnOnStreet;
        private float blipScale;

        private Blip zoneBlip;
        public bool spawned = false;
        public bool inactive = false;
        public bool dangerous = false;
        public bool capture = false;

        public bool isReclaimable = true;
        public PedHash reclaimersOutfit;
        public WeaponHash reclaimersWeapon;

        private int zoneFarLimit;
        private int zoneNearLimit;

        private int ticksSinceZoneReclaim = 0;
        private bool registerTicks = false;

        public TerroristZone(
            Vector3 zonePos, int terroristsAmount,
            string groupName, FighterConfiguration fighterCfg,
            int spawnRadius, bool spawnOnStreet = true, bool isReclaimable = true,
            PedHash reclaimersOutfit = PedHash.Marine03SMY, WeaponHash reclaimersWeapon = WeaponHash.Unarmed,
            float blipScale = 2.5f, int zoneFarLimit = 500, int zoneNearLimit = 350)
        {
            this.zonePos = zonePos;
            this.terroristsAmount = terroristsAmount;
            this.groupName = groupName;
            this.fighterCfg = fighterCfg;
            this.spawnRadius = spawnRadius;
            this.spawnOnStreet = spawnOnStreet;
            this.blipScale = blipScale;
            this.zoneFarLimit = zoneFarLimit;
            this.zoneNearLimit = zoneNearLimit;

            this.isReclaimable = isReclaimable;
            this.reclaimersOutfit = reclaimersOutfit;
            this.reclaimersWeapon = reclaimersWeapon;
        }

        public void InitBlip(bool declare = true)
        {
            if (declare) this.zoneBlip = World.CreateBlip(this.zonePos);
            this.zoneBlip.Name = "Terrorist Zone - " + this.groupName;
            this.zoneBlip.Color = BlipColor.RedDark2;
            this.zoneBlip.Scale = this.blipScale;
            this.zoneBlip.IsShortRange = false;
        }

        public void MarkBlipAsDangerous()
        {
            this.zoneBlip.Name = "Dangerous Zone - " + this.groupName;
            this.zoneBlip.Color = BlipColor.Orange; 
            this.zoneBlip.Scale = this.blipScale * 1.2f;
            this.zoneBlip.IsFlashing = true;
        }

        public bool IsPlayerNearZone() =>
            Game.Player.Character.Position.DistanceTo(this.zonePos) < this.zoneNearLimit;
        public bool IsPlayerFarFromZone() =>
            Game.Player.Character.Position.DistanceTo(this.zonePos) > this.zoneFarLimit;

        public void DeleteTerrorists()
        {
            foreach (Ped terrorist in terrorists)
            {
                terrorist.AttachedBlip.Delete();
                terrorist.Delete();
            }

            terrorists.Clear();
            spawned = false;
        }

        public void ManageDeadTerrorists()
        {
            var removedTerrorists = new List<Ped>();

            foreach (Ped terrorist in this.terrorists)
                if (!terrorist.IsAlive)
                {
                    if (terrorist.AttachedBlip != null) terrorist.AttachedBlip.Delete();
                    removedTerrorists.Add(terrorist);
                }

            foreach (Ped terrorist in removedTerrorists)
            {
                this.terrorists.Remove(terrorist);
                this.deadTerrorists.Add(terrorist);
            }

            if (this.terrorists.Count == 0)
            {
                ScreenEffects.ZoneCaptured();
                Screen.ShowSubtitle("Congratulations - " + this.groupName + " (" + this.terroristsAmount + " soldiers) defeated", 15000);

                this.zoneBlip.Color = BlipColor.GreenDark;
                this.zoneBlip.IsFlashing = false;
                this.zoneBlip.ShowRoute = false;
                this.zoneBlip.Scale = 2.5f;
                this.zoneBlip.Name = "Safe Zone - " + this.groupName;
                this.dangerous = false;
                this.inactive = true;
                this.capture = false;
            }
        }

        public int ClearDeadEntities()
        {
            int deletedEntities = deadTerrorists.Count;
            foreach (Ped terrorist in deadTerrorists) terrorist.Delete();
            deadTerrorists.Clear();

            return deletedEntities;
        }

        public void SpawnTerrorist()
        {
            Ped ped = World.CreatePed(
                this.fighterCfg.pedHash,
                spawnOnStreet ? World.GetNextPositionOnStreet(this.zonePos.Around(this.spawnRadius)) : World.GetNextPositionOnSidewalk(this.zonePos.Around(this.spawnRadius))
            );

            WeaponHash weapon = WeaponHash.Unarmed;

            if (this.fighterCfg.weapon == WeaponHash.Unarmed)
                weapon = GlobalInfo.weaponsList[GlobalInfo.generalRandomInstance.Next(0, GlobalInfo.weaponsList.Count)];
            else
                weapon = this.fighterCfg.weapon;

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

            terrorists.Add(ped);
        }

        public void SpawnTerrorists()
        {
            if (!spawned)
                spawned = true;
            else
                throw new Exception("Group already spawned");

            try
            {
                for (int i = 1; i < this.terroristsAmount; i++)
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
            this.terrorists.Remove(terrorist);
        }

        public void ZoneReclaimedTick()
        {
            if (!dangerous && inactive && GlobalInfo.generalRandomInstance.Next(0, 50) == 0)
            {
                ScreenEffects.ZoneReclaimed();

                MarkBlipAsDangerous();
                spawned = false;
                inactive = false;
                dangerous = true;

                if (Convert.ToInt32(terroristsAmount * 0.8) > 5)
                    terroristsAmount = Convert.ToInt32(terroristsAmount * 0.8);
                else
                    terroristsAmount = 5;

                fighterCfg = new FighterConfiguration(reclaimersOutfit, reclaimersWeapon);
                GTA.UI.Screen.ShowSubtitle(groupName + " zone is being reclaimed !", 8000);

                registerTicks = true;
            }
        }   

        public bool ZoneLostTick()
        {
            if (registerTicks)
                ticksSinceZoneReclaim++;

            if (dangerous && !capture && ticksSinceZoneReclaim == 2)
            {
                ScreenEffects.ZoneLost();

                InitBlip(false);
                inactive = false;
                spawned = false;
                dangerous = false;

                if (Convert.ToInt32(terroristsAmount * 1.4) <= 100)
                    terroristsAmount = Convert.ToInt32(terroristsAmount * 1.4);
                else
                    terroristsAmount = 100;

                ticksSinceZoneReclaim = 0;
                registerTicks = false;

                GTA.UI.Screen.ShowSubtitle(groupName + " zone was lost - terrorists are now stronger !", 10000);
                return true;
            }

            return false;
        }
    }   
}
