namespace ExpansionManager.Fixes;

// If a drop table has no available drops, we pull from a secondary drop table
// to ensure that the player always gets something
public static class DropTableFallbacks
{
    public static readonly Dictionary<string, string> fallbacksDictionary = new()
    {
        { "dtVoidChest", "dtChest1" },
        { "dtShrineHalcyoniteTier1", "dtShrineHalcyoniteTier3" }, // Tiers 1 and 2 are only halcyonite items, Tier 3 is no halcyonite items
        { "dtShrineHalcyoniteTier2", "dtShrineHalcyoniteTier3" },
        { "dtCursedItemTable", "dtDuplicatorWild" },
        { "bdtHallowedCauldron_HighGrade", "dtTier3Item" },
    };

    public static bool DropTableFallback(PickupDropTable dropTable, out PickupDropTable fallbackDropTable)
    {
        if (dropTable.GetPickupCount() == 0 && fallbacksDictionary.TryGetValue(dropTable.name, out string fallbackName))
        {
            return fallbackDropTable = PickupDropTable.instancesList.Find(x => x.name == fallbackName);
        }
        fallbackDropTable = null;
        return false;
    }

    [SystemInitializer]
    private static void Init()
    {
        On.RoR2.PickupDropTable.GeneratePickup += PickupDropTable_GeneratePickup;
        On.RoR2.PickupDropTable.GenerateDistinctPickups += PickupDropTable_GenerateDistinctPickups;
        On.RoR2.PickupDropTable.GenerateDrop += PickupDropTable_GenerateDrop;
        On.RoR2.PickupDropTable.GenerateUniqueDrops += PickupDropTable_GenerateUniqueDrops;
        On.EntityStates.HallowedCauldron.DrainingItems.GetDropTable += DrainingItems_GetDropTable;
        On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState.OnEnter += OverchargingState_OnEnter;
    }

    private static void PickupDropTable_GenerateDistinctPickups(On.RoR2.PickupDropTable.orig_GenerateDistinctPickups orig, PickupDropTable self, List<UniquePickup> dest, int desiredCount, Xoroshiro128Plus rng, bool allowLoop = true)
    {
        if (DropTableFallback(self, out PickupDropTable fallbackDropTable))
        {
            fallbackDropTable.GenerateDistinctPickups(dest, desiredCount, rng, allowLoop);
            return;
        }
        orig(self, dest, desiredCount, rng, allowLoop);
    }

    private static UniquePickup PickupDropTable_GeneratePickup(On.RoR2.PickupDropTable.orig_GeneratePickup orig, PickupDropTable self, Xoroshiro128Plus rng)
    {
        if (DropTableFallback(self, out PickupDropTable fallbackDropTable))
        {
            return fallbackDropTable.GeneratePickup(rng);
        }
        return orig(self, rng);
    }

    private static PickupIndex[] PickupDropTable_GenerateUniqueDrops(On.RoR2.PickupDropTable.orig_GenerateUniqueDrops orig, PickupDropTable self, int maxDrops, Xoroshiro128Plus rng)
    {
        if (DropTableFallback(self, out PickupDropTable fallbackDropTable))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return fallbackDropTable.GenerateUniqueDrops(maxDrops, rng);
#pragma warning restore CS0618 // Type or member is obsolete
        }
        return orig(self, maxDrops, rng);
    }

    private static PickupIndex PickupDropTable_GenerateDrop(On.RoR2.PickupDropTable.orig_GenerateDrop orig, PickupDropTable self, Xoroshiro128Plus rng)
    {
        if (DropTableFallback(self, out PickupDropTable fallbackDropTable))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return fallbackDropTable.GenerateDrop(rng);
#pragma warning restore CS0618 // Type or member is obsolete
        }
        return orig(self, rng);
    }

    private static DrainItemsAndDropMultiChoicePotentialController.DropTier DrainingItems_GetDropTable(On.EntityStates.HallowedCauldron.DrainingItems.orig_GetDropTable orig, EntityStates.HallowedCauldron.DrainingItems self, int tier)
    {
        var dropTier = orig(self, tier);
        if (Run.instance.AreExpansionItemsDisabled(DLC4Content.DLC4))
        {
            if (fallbacksDictionary.TryGetValue(dropTier.dropTable.name, out string fallbackName))
            {
                dropTier = new DrainItemsAndDropMultiChoicePotentialController.DropTier
                {
                    dropTable = PickupDropTable.instancesList.Find(x => x.name == fallbackName),
                    optionsModifier = dropTier.optionsModifier,
                    displayColorIndex = dropTier.displayColorIndex
                };
            }
        }
        return dropTier;
    }

    private static void OverchargingState_OnEnter(On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState.orig_OnEnter orig, EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState self)
    {
        orig(self);
        if (Run.instance.AreExpansionItemsDisabled(DLC4Content.DLC4))
        {
            BossGroup bossGroup = self.extraBossGroupServer;
            if (bossGroup)
            {
                bossGroup.bossDropChance = 1;
            }
        }
    }
}
