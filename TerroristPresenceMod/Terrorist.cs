using GTA;
using GTA.Math;
using GTA.Native;
using System;
using TerroristPresenceMod.Utils;

namespace TerroristPresenceMod
{
    class Terrorist
    {
        public Ped Ped;
        public FighterConfiguration FighterCfg;
        public bool Exists = false;

        private BlipColor _BlipColor;

        public Terrorist(FighterConfiguration cfg, BlipColor bColor)
        {
            FighterCfg = cfg;
            _BlipColor = bColor;
        }

        public void Spawn(Vector3 spawnPos)
        {
            Ped = World.CreatePed(
                FighterCfg.PedHash,
                spawnPos
            );
        }

        public void Configure(bool wander = true)
        {
            if (Ped == null)
            {
                throw new ArgumentException();
            }

            WeaponHash weapon;
            if (FighterCfg.Weapon == WeaponHash.Unarmed)
                weapon = GlobalInfo.WeaponsList[
                    GlobalInfo.GeneralRandomInstance.Next(0, GlobalInfo.WeaponsList.Count - 1)
                ];
            else
                weapon = FighterCfg.Weapon;

            Ped.Weapons.Give(weapon, -1, true, true);
            Ped.Accuracy = 15;
            Ped.MaxHealth = 200;
            Ped.Health = 200;
            Ped.Armor = 100;
            Ped.IsPersistent = true; // TODO
            if (wander) Ped.Task.WanderAround();
            Ped.RelationshipGroup = GlobalInfo.RELATIONSHIP_TERRORIST;

            Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, Ped, 2); // Offensive combat movement
            Function.Call(Hash.SET_PED_HEARING_RANGE, Ped, 10000f);
            Function.Call(Hash.SET_PED_SEEING_RANGE, Ped, 10000f);

            Ped.AddBlip();
            if (Ped.AttachedBlip != null)
                Ped.AttachedBlip.Color = _BlipColor;

            Exists = true;
        }

        public void Delete()
        {
            Ped.AttachedBlip.Delete();  
            Ped.Delete();
            Exists = false;
        }
    }
}
