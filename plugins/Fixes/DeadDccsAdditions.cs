using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.ExpansionManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
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
        { "dccsIronalluviumMonsters", IronAlluviumMonsters },
        { "dccsIronalluvium2MonstersDLC2", Ironalluvium2MonstersDLC2 },
        { "dccsRepurposedcraterMonstersDLC1", RepurposedCraterMonstersDLC1 },
        { "dccsConduitcanyonMonsters", ConduitCanyonMonsters },
        { "dccsBallFamily_ItemThemed", BallFamily_ItemThemed },
    };

    public static AsyncOperationHandle<SpawnCard>
        cscBell = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Bell/cscBell.asset"),
        cscLesserWisp = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Wisp/cscLesserWisp.asset"),
        cscMagmaWorm = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/MagmaWorm/cscMagmaWorm.asset"),
        cscHermitCrab = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/HermitCrab/cscHermitCrab.asset"),
        cscLemurian = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Lemurian/cscLemurian.asset"),
        cscVulture = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Vulture/cscVulture.asset"),
        cscRoboBallMini = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/RoboBallBoss/cscRoboBallMini.asset"),
        cscChild = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC2/Child/cscChild.asset"),
        cscMinorConstruct = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/MajorAndMinorConstruct/cscMinorConstruct.asset"),
        cscMiniMushroom = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/MiniMushroom/cscMiniMushroom.asset"),
        cscImp = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Imp/cscImp.asset"),
        cscRoboBallBoss = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset");

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
            spawnCard = cscHermitCrab.WaitForCompletion(),
            selectionWeight = 1,
            spawnDistance = DirectorCore.MonsterSpawnDistance.Far,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscLemurian.WaitForCompletion(),
            selectionWeight = 1,
        });
        dccs.SetCategoryWeight(BASIC_MONSTERS, 2);
    }

    public static void IronAlluviumMonsters(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscRoboBallMini.WaitForCompletion(),
            selectionWeight = 1,
            spawnDistance = DirectorCore.MonsterSpawnDistance.Far,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscVulture.WaitForCompletion(),
            selectionWeight = 1,
        });
        dccs.SetCategoryWeight(BASIC_MONSTERS, 2);
    }

    public static void Ironalluvium2MonstersDLC2(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscChild.WaitForCompletion(),
            selectionWeight = 1,
        });
        dccs.SetCategoryWeight(BASIC_MONSTERS, 2);
    }

    public static void RepurposedCraterMonstersDLC1(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscMinorConstruct.WaitForCompletion(),
            selectionWeight = 4,
            preventOverhead = true,
        });
    }

    public static void ConduitCanyonMonsters(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscMiniMushroom.WaitForCompletion(),
            selectionWeight = 10,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscImp.WaitForCompletion(),
            selectionWeight = 10,
        });
    }

    public static void BallFamily_ItemThemed(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(CHAMPIONS, new DirectorCard
        {
            spawnCard = cscRoboBallBoss.WaitForCompletion(),
            selectionWeight = 1,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = cscRoboBallMini.WaitForCompletion(),
            selectionWeight = 1,
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

    public static void SetCategoryWeight(this DirectorCardCategorySelection dccs, string categoryName, int categoryWeight)
    {
        int categoryIndex = dccs.FindCategoryIndexByName(categoryName);
        if (ArrayUtils.IsInBounds(dccs.categories, categoryIndex))
        {
            dccs.categories[categoryIndex].selectionWeight = categoryWeight;
        }
    }

    [SystemInitializer]
    private static void Init()
    {
        On.RoR2.DCCSBlender.MergeCategories += DCCSBlender_MergeCategories;
        On.RoR2.CombatDirector.Awake += CombatDirector_Awake;
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

    private static void CombatDirector_Awake(On.RoR2.CombatDirector.orig_Awake orig, CombatDirector self)
    {
        orig(self);
        if (NetworkServer.active && self.monsterCards)
        {
            var dccs = new DirectorCardCategorySelection();
            dccs.CopyFrom(self.monsterCards);
            List<WeightedSelection<DirectorCardCategorySelection>.ChoiceInfo> source =
            [
                new WeightedSelection<DirectorCardCategorySelection>.ChoiceInfo
                {
                    value = self.monsterCards,
                    weight = 1
                }
            ];
            DCCSBlender_MergeCategories(none, ref dccs, source);
            self.monsterCardsSelection = dccs.GenerateDirectorCardWeightedSelection();
        }
        static void none(ref DirectorCardCategorySelection blendedDCCS, List<WeightedSelection<DirectorCardCategorySelection>.ChoiceInfo> selectedDCCSList)
        {
        }
    }
}
