using GTA;

namespace TerroristPresenceMod
{
    class FighterConfiguration
    {
        public PedHash PedHash;
        public WeaponHash Weapon;

        public FighterConfiguration(PedHash pedHash, WeaponHash weapon = WeaponHash.Unarmed)
        {
            PedHash = pedHash;
            Weapon = weapon;
        }
    }
}
