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
            if (pickupDef != null)
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
            if (IsValid(pickupIndex))
            {
                continue;
            }
            self.weightedSelection.RemoveChoice(i);
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
            c.EmitDelegate((bool isChoiceActive, PreGameController controller, RuleChoiceDef choice) =>
            {
                ExpansionDef requiredExpansion = choice.requiredExpansionDef;
                if (requiredExpansion)
                {
                    requiredExpansion = GetRequiredExpansion(choice.itemIndex, choice.equipmentIndex);
                    if (ExpansionRulesCatalog.IsExpansionContentDisabled(controller.readOnlyRuleBook, requiredExpansion, ExpansionRulesCatalog.disableExpansionItemsChoices))
                    {
                        return false;
                    }
                }
                return isChoiceActive;
            });
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(ItemBlockers)}: {nameof(PreGameController_RecalculateModifierAvailability)} IL match failed");
    }

    private static void CraftableCatalog_FilterByEntitlement(On.RoR2.CraftableCatalog.orig_FilterByEntitlement orig, ref CraftableCatalog.RecipeEntry[] source)
    {
        orig(ref source);
        source = source.Where((CraftableCatalog.RecipeEntry entry) => IsValid(entry.result)).ToArray();
    }

    public static bool IsValid(PickupIndex pickupIndex)
    {
        if (pickupIndex != PickupIndex.none)
        {
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef is not null)
            {
                ExpansionDef requiredExpansion = GetRequiredExpansion(pickupDef.itemIndex, pickupDef.equipmentIndex);
                if (Run.instance.AreExpansionItemsDisabled(requiredExpansion))
                    return false;
            }
        }
        return true;
    }

    public static ExpansionDef GetRequiredExpansion(ItemIndex itemIndex, EquipmentIndex equipmentIndex)
    {
        ExpansionDef requiredExpansion = null;
        if (itemIndex != ItemIndex.None)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            if (itemDef)
            {
                switch (itemDef.tier)
                {
                    case ItemTier.Hex:
                    case ItemTier.Page:
                        break;

                    default:
                        if (itemDef.ContainsTag(ItemTag.WorldUnique))
                            break;
                        goto case ItemTier.FoodTier;

                    case ItemTier.FoodTier:
                        requiredExpansion = itemDef.requiredExpansion;
                        break;
                }
            }
        }
        else if (equipmentIndex != EquipmentIndex.None)
        {
            EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
            if (equipmentDef && equipmentDef.canDrop)
            {
                requiredExpansion = equipmentDef.requiredExpansion;
            }
        }

        return requiredExpansion;
    }
}
