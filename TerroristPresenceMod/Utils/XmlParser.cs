using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Xml;
using Hash = GTA.Native.Hash;

namespace TerroristPresenceMod.Utils
{
    class XmlParsingException : Exception
    {
        public override string Message { get; }

        public XmlParsingException(string message)
        {
            Message = message;
        }
    }

    class XmlParser
    {
        public static TerroristZone parseZoneConfiguration(
            XmlNodeList zoneConfig,
            ref List<string> encounteredNames,
            ref List<Vector3> encounteredPositions
        ) {
            string zoneName = "";
            int soldiersCount = 0, spawnRadius = 0;
            int posX = 0, posY = 0, posZ = 0;
            bool spawnOnStreet = false, isZoneReclaimable = true;
            PedHash pedOutfit = PedHash.Abigail, reclaimersOutfit = PedHash.Marine03SMY;
            WeaponHash weapon = WeaponHash.APPistol, reclaimersWeapon = WeaponHash.Unarmed;

            List<string> nodesList = new List<string>()
            {
                "Name", "SoldiersCount", "SpawnRadius", "SpawnOnStreet",
                "OutfitId", "WeaponId", "IsReclaimable", "ReclaimersOutfit",
                "ReclaimersWeapon", "X", "Y", "Z"
            };

            List<string> requiredNodes = new List<string>() {
                "Name", "SoldiersCount", "SpawnRadius", "OutfitId", "WeaponId", "X", "Y", "Z"
            };

            foreach (XmlNode node in zoneConfig)
            {
                if (node.Name == "Name")
                    zoneName = node.InnerText;

                else if (node.Name == "SoldiersCount")
                    soldiersCount = Convert.ToInt32(node.InnerText);

                else if (node.Name == "SpawnRadius")
                    spawnRadius = Convert.ToInt32(node.InnerText);

                else if (node.Name == "SpawnOnStreet")
                    spawnOnStreet = node.InnerText == "true";

                else if (node.Name == "OutfitId")
                    pedOutfit = (PedHash)Function.Call<Hash>(Hash.GET_HASH_KEY, node.InnerText);

                else if (node.Name == "WeaponId")
                {
                    if (node.InnerText.Trim().Length == 0)
                        weapon = WeaponHash.Unarmed;
                    else
                        weapon = (WeaponHash)Function.Call<Hash>(Hash.GET_HASH_KEY, node.InnerText);
                }

                else if (node.Name == "IsReclaimable")
                    isZoneReclaimable = node.InnerText == "true";

                else if (node.Name == "ReclaimersOutfit")
                {
                    reclaimersOutfit = (PedHash)Function.Call<Hash>(Hash.GET_HASH_KEY, node.InnerText);
                }
                else if (node.Name == "ReclaimersWeapon")
                    if (node.InnerText.Trim().Length == 0)
                        reclaimersWeapon = WeaponHash.Unarmed;
                    else
                        reclaimersWeapon = (WeaponHash)Function.Call<Hash>(Hash.GET_HASH_KEY, node.InnerText);

                else if (node.Name == "X")
                    posX = Convert.ToInt32(node.InnerText);

                else if (node.Name == "Y")
                    posY = Convert.ToInt32(node.InnerText);

                else if (node.Name == "Z")
                    posZ = Convert.ToInt32(node.InnerText);

                if (!nodesList.Contains(node.Name))
                    throw new XmlParsingException("Invalid XML file (unknown node '" + node.Name + "')");

                if (requiredNodes.Contains(node.Name))
                    requiredNodes.Remove(node.Name);
            }

            var currentPos = new Vector3(posX, posY, posZ);

            foreach (Vector3 pos in encounteredPositions)
                if (currentPos.DistanceTo(pos) < 100)
                    throw new XmlParsingException("Zone '" + zoneName + "' is too close to another zone on the map.");

            encounteredPositions.Add(currentPos);

            if (requiredNodes.Count > 0)
                throw new XmlParsingException("Missing node '" + requiredNodes[0] + "'");

            if (encounteredNames.Contains(zoneName))
                throw new XmlParsingException("Two zones cannot have the same name : '" + zoneName + "'");
            else
                encounteredNames.Add(zoneName);

            return new TerroristZone(
                currentPos, soldiersCount, zoneName, new FighterConfiguration(pedOutfit, weapon),
                spawnRadius, spawnOnStreet, isZoneReclaimable
            );
        }
    }
}
