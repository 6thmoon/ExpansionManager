using ExpansionManager.Util;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.ExpansionManagement;

namespace ExpansionManager.ContentBlockers;

public static class ItemBlockers
{
    [SystemInitializer]
    private static void Init()
    {
        CharacterBody.onBodyAwakeGlobal += CharacterBody_onBodyAwakeGlobal;
        On.RoR2.ExplicitPickupDropTable.GenerateWeightedSelection += ExplicitPickupDropTable_GenerateWeightedSelection;
        IL.RoR2.PreGameController.RecalculateModifierAvailability += PreGameController_RecalculateModifierAvailability;
        On.RoR2.CraftableCatalog.FilterByEntitlement += CraftableCatalog_FilterByEntitlement;
    }

    private static void CharacterBody_onBodyAwakeGlobal(CharacterBody body)
    {
        if (body.TryGetComponent(out DeathRewards deathRewards))
        {
            if (deathRewards.bossDropTable && deathRewards.bossDropTable.GetPickupCount() == 0)
            {
                deathRewards.bossDropTable = null;
            }
            PickupDef pickupDef = PickupCatalog.GetPickupDef((PickupIndex)deathRewards.bossPickup);
            if (pickupDef != null && (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None))
            {
                ExpansionDef requiredExpansion = pickupDef.GetRequiredExpansion();
                if (requiredExpansion && Run.instance.AreExpansionItemsDisabled(requiredExpansion))
                {
                    deathRewards.bossPickup = default;
                }
            }
        }
    }

    private static void ExplicitPickupDropTable_GenerateWeightedSelection(On.RoR2.ExplicitPickupDropTable.orig_GenerateWeightedSelection orig, ExplicitPickupDropTable self)
    {
        orig(self);
        if (!Run.instance)
        {
            return;
        }
        for (int i = self.weightedSelection.Count; --i >= 0;)
        {
            PickupIndex pickupIndex = self.weightedSelection.GetChoice(i).value.pickupIndex;
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef != null)
            {
                ItemIndex itemIndex = pickupDef.itemIndex;
                if (itemIndex is not ItemIndex.None)
                {
                    if (ItemCatalog.GetItemDef(itemIndex).ContainsTag(ItemTag.WorldUnique))
                    {
                        continue;
                    }
                }
                else if (pickupDef.equipmentIndex is EquipmentIndex.None)
                {
                    continue;
                }
                ExpansionDef requiredExpansion = pickupDef.GetRequiredExpansion();
                if (requiredExpansion && Run.instance.AreExpansionItemsDisabled(requiredExpansion))
                {
                    self.weightedSelection.RemoveChoice(i);
                }
            }
        }
    }

    private static void PreGameController_RecalculateModifierAvailability(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locChoiceDefIndex = -1;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out locChoiceDefIndex),
            x => x.MatchLdfld<RuleChoiceDef>(nameof(RuleChoiceDef.requiredExpansionDef)),
            x => x.MatchLdfld<ExpansionDef>(nameof(ExpansionDef.enabledChoice)),
            x => x.MatchCallOrCallvirt<RuleBook>(nameof(RuleBook.IsChoiceActive)))
            )
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, locChoiceDefIndex);
            c.EmitDelegate((PreGameController controller, RuleChoiceDef choice) =>
            {
                if (ExpansionRulesCatalog.IsExpansionContentDisabled(controller.readOnlyRuleBook, choice.requiredExpansionDef, ExpansionRulesCatalog.disableExpansionItemsChoices))
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(choice.itemIndex);
                    if (itemDef)
                    {
                        return itemDef.ContainsTag(ItemTag.WorldUnique) && itemDef.tier is not ItemTier.FoodTier;
                    }
                    else
                    {
                        EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(choice.equipmentIndex);
                        if (equipmentDef)
                        {
                            return equipmentDef.IsElite();
                        }
                    }
                }
                return true;
            });
            c.Emit(OpCodes.And);
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(ItemBlockers)}: {nameof(PreGameController_RecalculateModifierAvailability)} IL match failed");
    }

    private static void CraftableCatalog_FilterByEntitlement(On.RoR2.CraftableCatalog.orig_FilterByEntitlement orig, ref CraftableCatalog.RecipeEntry[] source)
    {
        orig(ref source);
        var list = new List<CraftableCatalog.RecipeEntry>();
        foreach (CraftableCatalog.RecipeEntry entry in source)
        {
            PickupDef pickup = PickupCatalog.GetPickupDef(entry.result);
            if (!Run.instance.IsPickupAvailable(entry.result))
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(pickup.itemIndex);
                EquipmentIndex equipmentIndex = pickup.equipmentIndex;
                if (itemDef)
                {
                    if (itemDef.tier is ItemTier.FoodTier || itemDef.DoesNotContainTag(ItemTag.WorldUnique))
                        continue;
                }
                else if (equipmentIndex is not EquipmentIndex.None)
                {
                    if (EquipmentCatalog.GetEquipmentDef(equipmentIndex).canDrop)
                        continue;
                }
            }
            list.Add(entry);
        }
        source = list.ToArray();
    }
}
