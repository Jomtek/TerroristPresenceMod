using GTA;

namespace TerroristPresenceMod.Utils
{
    public class RelationshipSetter
    {
        public static void SetRelationships()
        {
            GlobalInfo.RELATIONSHIP_TERRORIST.SetRelationshipBetweenGroups(GlobalInfo.RELATIONSHIP_COP, Relationship.Hate, true);
            GlobalInfo.RELATIONSHIP_TERRORIST.SetRelationshipBetweenGroups(GlobalInfo.RELATIONSHIP_ARMY, Relationship.Hate, true);
            GlobalInfo.RELATIONSHIP_TERRORIST.SetRelationshipBetweenGroups(GlobalInfo.RELATIONSHIP_SECURITY_GUARD, Relationship.Hate, true);
            GlobalInfo.RELATIONSHIP_TERRORIST.SetRelationshipBetweenGroups(GlobalInfo.RELATIONSHIP_PLAYER, Relationship.Hate, false);

            GlobalInfo.RELATIONSHIP_TERRORIST.SetRelationshipBetweenGroups(GlobalInfo.RELATIONSHIP_CIVMALE, Relationship.Hate, false);
            GlobalInfo.RELATIONSHIP_TERRORIST.SetRelationshipBetweenGroups(GlobalInfo.RELATIONSHIP_CIVFEMALE, Relationship.Hate, false);
        }
    }
}
