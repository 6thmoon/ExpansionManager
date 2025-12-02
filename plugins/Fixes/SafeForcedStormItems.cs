using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace ExpansionManager.Fixes;

// Pickups with guaranteed Seekers of the Storm items do not handle the case where no Seekers of the Storm items are available
// This is mostly handled by drop table fallbacks for the Seekers of the Storm drop tables but this fix is included for extra safety
public static class SafeForcedStormItems
{
    [SystemInitializer]
    private static void Init()
    {
        IL.RoR2.PickupPickerController.GenerateOptionsFromDropTablePlusForcedStorm += PickupPickerController_GenerateOptionsFromDropTablePlusForcedStorm;
    }

    private static void PickupPickerController_GenerateOptionsFromDropTablePlusForcedStorm(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locStormDropsListIndex = -1;
        int locElementIndex = -1;
        ILLabel breakLabel = null;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(2),
            x => x.MatchLdloc(out locStormDropsListIndex),
            x => x.MatchLdarg(0),
            x => x.MatchLdarg(3),
            x => x.MatchLdcI4(1),
            x => x.MatchCallOrCallvirt<PickupDropTable>(nameof(PickupDropTable.GenerateDistinctPickups)))
            && c.TryGotoNext(MoveType.Before,
            x => x.MatchLdloc(locStormDropsListIndex),
            x => x.MatchLdloc(out locElementIndex),
            x => x.MatchCallvirt(out _),    // good enough, should be list indexer i.e. `List<UniquePickup>.get_Item`
            x => x.MatchStfld<PickupPickerController.Option>(nameof(PickupPickerController.Option.pickup)))
            && c.TryGotoPrev(MoveType.After,
            x => x.MatchBgt(out breakLabel))
            )
        {
            c.Emit(OpCodes.Ldloc, locStormDropsListIndex);
            c.Emit(OpCodes.Ldloc, locElementIndex);
            c.EmitDelegate<Func<List<UniquePickup>, int, bool>>((stormDropsList, i) =>
            {
                return ListUtils.IsInBounds(stormDropsList, i);
            });
            c.Emit(OpCodes.Brfalse, breakLabel);
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(SafeForcedStormItems)}: {nameof(PickupPickerController_GenerateOptionsFromDropTablePlusForcedStorm)} IL match failed");
    }
}
