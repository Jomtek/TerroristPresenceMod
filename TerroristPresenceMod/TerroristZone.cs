using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;

namespace TerroristPresenceMod
{
    class TerroristZone
    {
        public Vector3 zonePos { get; }
        public string groupName { get; }

        public List<Ped> terrorists = new List<Ped>();
        private int terroristsAmount;
        private FighterConfiguration fighterCfg;
        private int spawnRadius;
        private bool spawnOnStreet;
        private float blipScale;

        private Blip zoneBlip;
        public bool spawned = false;
        public bool inactive = false;

        public TerroristZone(
            Vector3 zonePos, int terroristsAmount,
            string groupName, FighterConfiguration fighterCfg,
            int spawnRadius, bool spawnOnStreet = true, float blipScale = 2.5f)
        {
            this.zonePos = zonePos;
            this.terroristsAmount = terroristsAmount;
            this.groupName = groupName;
            this.fighterCfg = fighterCfg;
            this.spawnRadius = spawnRadius;
            this.spawnOnStreet = spawnOnStreet;
            this.blipScale = blipScale;
        }

        public void InitBlip()
        {
            this.zoneBlip = World.CreateBlip(this.zonePos);
            this.zoneBlip.Name = "Terrorist Zone - " + this.groupName;
            this.zoneBlip.Color = BlipColor.RedDark2;
            this.zoneBlip.Scale = this.blipScale;
            this.zoneBlip.IsShortRange = false;
        }

        public bool IsPlayerNearZone() =>
            Game.Player.Character.Position.DistanceTo(this.zonePos) < 350;
        public bool IsPlayerFarFromZone() =>
            Game.Player.Character.Position.DistanceTo(this.zonePos) > 500;

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

        public void RemoveDeadTerrorists()
        {
            var removedTerrorists = new List<Ped>();

            foreach (Ped terrorist in this.terrorists)
                if (!terrorist.IsAlive)
                {
                    if (terrorist.AttachedBlip != null) terrorist.AttachedBlip.Delete();
                    removedTerrorists.Add(terrorist);
                }

            foreach (Ped terrorist in removedTerrorists)
                this.terrorists.Remove(terrorist);

            if (this.terrorists.Count == 0)
            {
                Screen.ShowSubtitle("Congratulations - " + this.groupName + " (" + this.terroristsAmount + " soldiers) defeated", 15000);
                Game.Player.Money += this.terroristsAmount * 650;
                this.zoneBlip.Color = BlipColor.GreenDark;
                this.zoneBlip.Name = "Safe Zone - " + this.groupName;
                this.inactive = true;
            }
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
                {
                    var ped = World.CreatePed(
                        this.fighterCfg.pedHash,
                        spawnOnStreet ? World.GetNextPositionOnStreet(this.zonePos.Around(this.spawnRadius)) : World.GetNextPositionOnSidewalk(this.zonePos.Around(this.spawnRadius))
                   );

                    WeaponHash weapon;

                    if (this.fighterCfg.weapon == WeaponHash.Unarmed)
                        weapon = GlobalInfo.weaponsList[GlobalInfo.generalRandomInstance.Next(0, GlobalInfo.weaponsList.Count)];
                    else
                        weapon = this.fighterCfg.weapon;

                    ped.Weapons.Give(weapon, -1, true, true);
                    ped.Accuracy = 1;
                    ped.Task.WanderAround();
                    ped.RelationshipGroup = GlobalInfo.RELATIONSHIP_TERRORIST;

                    Function.Call(Hash.SET_PED_HEARING_RANGE, ped, 1000.0);
                    Function.Call(Hash.SET_PED_SEEING_RANGE, ped, 1000.0);

                    ped.AddBlip();
                    if (ped.AttachedBlip != null)
                        ped.AttachedBlip.Color = BlipColor.GreyDark;

                    terrorists.Add(ped);
                }
            } catch (Exception ex)
            {
                Notification.Show(ex.StackTrace);
            }

            // Notification.Show("Spawned " + this.groupName + " terrorists");
        }
    }   
}
