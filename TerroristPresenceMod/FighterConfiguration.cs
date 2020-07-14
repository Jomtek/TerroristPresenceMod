using GTA;

namespace TerroristPresenceMod
{
    class FighterConfiguration
    {
        public PedHash pedHash;
        public WeaponHash weapon;

        public FighterConfiguration(PedHash pedHash, WeaponHash weapon = WeaponHash.Unarmed)
        {
            this.pedHash = pedHash;
            this.weapon = weapon;
        }
    }
}
