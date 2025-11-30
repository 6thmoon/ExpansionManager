using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.ExpansionManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ExpansionManager.Fixes;

// Most DLC stages do not have a base monster pool
// and the monster pool on some stages is left especially barren if the expansion's monsters are disabled
// We add a few new monsters to fill in the gaps in this situation
public static class DeadDccsAdditions
{
    const string
        CHAMPIONS = "Champions",
        MINIBOSSES = "Minibosses",
        BASIC_MONSTERS = "Basic Monsters",
        SPECIAL = "Special";

    public static readonly Dictionary<string, Action<DirectorCardCategorySelection>> monsterSelectionAdditions = new()
    {
        { "dccsAncientLoftMonstersDLC1", AncientLoftMonstersDLC1 },
        { "dccsSulfurPoolsMonstersDLC1", SulfurPoolsMonstersDLC1 },
        { "dccsHelminthRoostMonstersDLC2Only", HelminthRoostMonstersDLC2Only },
    };

    public static AsyncOperationHandle<SpawnCard>
        cscBell = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Bell/cscBell.asset"),
        cscLesserWisp = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Wisp/cscLesserWisp.asset"),
        cscMagmaWorm = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/MagmaWorm/cscMagmaWorm.asset"),
        cscScorchling = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC2/Scorchling/cscScorchling.asset"),
        cscHermitCrab = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/HermitCrab/cscHermitCrab.asset"),
        cscLemurian = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Lemurian/cscLemurian.asset"),
        cscImp = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Imp/cscImp.asset");

    public static void AncientLoftMonstersDLC1(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(MINIBOSSES, new DirectorCard
        {
            spawnCard = cscBell.WaitForCompletion(),
            selectionWeight = 1,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscLesserWisp.WaitForCompletion(),
            selectionWeight = 1,
            preventOverhead = true,
        });
    }

    public static void SulfurPoolsMonstersDLC1(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(CHAMPIONS, new DirectorCard
        {
            spawnCard = cscMagmaWorm.WaitForCompletion(),
            selectionWeight = 1,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscScorchling.WaitForCompletion(),
            selectionWeight = 1,
            spawnDistance = DirectorCore.MonsterSpawnDistance.Far,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscHermitCrab.WaitForCompletion(),
            selectionWeight = 1,
            spawnDistance = DirectorCore.MonsterSpawnDistance.Far,
            preventOverhead = true,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscLemurian.WaitForCompletion(),
            selectionWeight = 2,
        });
    }

    public static void HelminthRoostMonstersDLC2Only(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscImp.WaitForCompletion(),
            selectionWeight = 2,
        });
    }

    public static bool AttemptAddCard(this DirectorCardCategorySelection dccs, string categoryName, DirectorCard card)
    {
        int categoryIndex = dccs.FindCategoryIndexByName(categoryName);
        if (ArrayUtils.IsInBounds(dccs.categories, categoryIndex))
        {
            dccs.AddCard(categoryIndex, card);
            return true;
        }
        return false;
    }

    [SystemInitializer]
    private static void Init()
    {
        On.RoR2.DCCSBlender.MergeCategories += DCCSBlender_MergeCategories;
    }

    private static void DCCSBlender_MergeCategories(On.RoR2.DCCSBlender.orig_MergeCategories orig, ref DirectorCardCategorySelection blendedDCCS, List<WeightedSelection<DirectorCardCategorySelection>.ChoiceInfo> selectedDCCSList)
    {
        orig(ref blendedDCCS, selectedDCCSList);
        foreach (WeightedSelection<DirectorCardCategorySelection>.ChoiceInfo choiceInfo in selectedDCCSList)
        {
            string name = choiceInfo.value.name;
            if (name.EndsWith("(Clone)"))
            {
                name = name.Remove(name.Length - 7);
            }
            if (monsterSelectionAdditions.TryGetValue(name, out var addCards))
            {
                SceneDef sceneDef = SceneInfo.instance ? SceneInfo.instance.sceneDef : null;
                ExpansionDef requiredExpansion = sceneDef ? sceneDef.requiredExpansion : null;
                if (requiredExpansion && Run.instance.AreExpansionMonstersDisabled(requiredExpansion))
                {
                    ExpansionManagerPlugin.Logger.LogInfo($"{nameof(DeadDccsAdditions)}: {requiredExpansion.name} has monsters disabled, Adding monster cards to {name}");
                    addCards(blendedDCCS);
                }
            }
        }
    }
}
