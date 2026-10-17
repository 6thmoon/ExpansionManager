using MonoMod.Cil;
using Random = UnityEngine.Random;

namespace ExpansionManager.ContentBlockers;

public static class EliteBlockers
{
    [SystemInitializer]
    private static void Init()
    {
        On.RoR2.EliteDef.IsAvailable += EliteDef_IsAvailable;
        On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState.OnEnter += OverchargingState_OnEnter;
        On.RoR2.DLC4LesserCatastropheController.BeginCatastrophesServer += DLC4LesserCatastropheController_BeginCatastrophesServer;
    }

    private static bool EliteDef_IsAvailable(On.RoR2.EliteDef.orig_IsAvailable orig, EliteDef self)
    {
        return orig(self) && !(self.eliteEquipmentDef && self.eliteEquipmentDef.requiredExpansion && Run.instance && Run.instance.AreExpansionElitesDisabled(self.eliteEquipmentDef.requiredExpansion));
    }

    private static void OverchargingState_OnEnter(On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState.orig_OnEnter orig, EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState self)
    {
        orig(self);
        CombatDirector combatDirector = self.extraBossDirectorServer;
        if (combatDirector)
        {
            combatDirector.onSpawnedServer.AddListener((GameObject obj) =>
            {
                if (obj.TryGetComponent(out Inventory inventory))
                {
                    EquipmentDef equipmentDef = inventory.GetActiveEquipment().equipmentDef;
                    if (Run.instance.AreExpansionElitesDisabled(equipmentDef.requiredExpansion))
                    {
                        equipmentDef = Random.value < 0.5 ? RoR2Content.Equipment.AffixPoison : RoR2Content.Equipment.AffixHaunted;
                        inventory.SetEquipmentIndex(equipmentDef.equipmentIndex, inventory.activeEquipmentLocation);
                    }
                }
            });
        }
    }

    private static void DLC4LesserCatastropheController_BeginCatastrophesServer(On.RoR2.DLC4LesserCatastropheController.orig_BeginCatastrophesServer orig, DLC4LesserCatastropheController self)
    {
        if (Run.instance.AreExpansionElitesDisabled(DLC4Content.DLC4) || Run.instance.AreExpansionMonstersDisabled(DLC4Content.DLC4))
        {
            self.CatastrophesToSpawn = 0;
        }
        orig(self);
    }
}
