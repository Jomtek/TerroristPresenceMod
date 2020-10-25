using GTA;
using System;
using System.Collections.Generic;

namespace TerroristPresenceMod.Utils
{
    public static class GlobalInfo
    {
        // Relationship Groups
        public static RelationshipGroup RELATIONSHIP_TERRORIST;
        public static RelationshipGroup RELATIONSHIP_ARMY = new RelationshipGroup(0xE3D976F3);
        public static RelationshipGroup RELATIONSHIP_COP = new RelationshipGroup(0xA49E591C);
        public static RelationshipGroup RELATIONSHIP_SECURITY_GUARD = new RelationshipGroup(0xF50B51B7);
        public static RelationshipGroup RELATIONSHIP_PLAYER = new RelationshipGroup(0x6F0783F5);

        public static RelationshipGroup RELATIONSHIP_CIVMALE = new RelationshipGroup(0x02B8FA80);
        public static RelationshipGroup RELATIONSHIP_CIVFEMALE = new RelationshipGroup(0x47033600);

        // At the demand of "PatrickNsyd"
        public static RelationshipGroup RELATIONSHIP_ZOMBIE;
        public static RelationshipGroup RELATIONSHIP_HOSTILE;

        // Zones capture state
        public static List<string> CapturedZonesNames;

        // Other
        public static List<int> SoldiersBlipsHandles = new List<int>();
        public static Random GeneralRandomInstance = new Random();

        public static List<WeaponHash> WeaponsList = new List<WeaponHash> {
            WeaponHash.HeavySniper,
            WeaponHash.RPG,
            WeaponHash.AssaultRifle,
            WeaponHash.DoubleBarrelShotgun,
            WeaponHash.Minigun
        };


        // // FUNCTIONS // //

        public static void SaveCapturedZones()
        {
            System.IO.File.WriteAllLines("scripts/TPM/CapturedZones.txt", CapturedZonesNames);
        }
    }
}