using RoR2.ExpansionManagement;
using System.Collections;
using UnityEngine.AddressableAssets;

namespace ExpansionManager.Fixes;

// Originally, only Shrine of Shaping was affected; currently applies to all DLC3 interactables
public static class MissingExpansionRequirement
{
    [SystemInitializer]
    private static IEnumerator Init()
    {
        var expansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC2/Common/DLC2.asset");
        var prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/ShrineColossusAccess.prefab");
        while (!expansion.IsDone)
        {
            yield return null;
        }
        yield return add();
        IEnumerator add()
        {
            while (!prefab.IsDone)
            {
                yield return null;
            }
            GameObject result = prefab.Result;
            if (!result || result.GetComponent<ExpansionRequirementComponent>())
            {
                yield break;
            }
            result.AddComponent<ExpansionRequirementComponent>().requiredExpansion = expansion.Result;
        }
        expansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC3/DLC3.asset");
        var requests = from key in new string[]
        {
            "RoR2/DLC3/DroneCombinerStation/DroneCombinerStation.prefab",
            "RoR2/DLC3/DroneScrapper/DroneScrapper.prefab",
            "RoR2/DLC3/TemporaryItemsDistributor/TemporaryItemsShopTerminal.prefab",
            "RoR2/DLC3/TripleDroneShop/TripleDroneShop.prefab"
        } select Addressables.LoadAssetAsync<GameObject>(key);
        while (!expansion.IsDone)
        {
            yield return null;
        }
        foreach (var handle in requests)
        {
            prefab = handle;
            yield return add();
        }
        expansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC4/DLC4.asset");
        while (!expansion.IsDone)
        {
            yield return null;
        }
        prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC4/HollowChest/HollowChestFree.prefab");
        yield return add();
        prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC4/ShrineCompulsion/ShrineCompulsion.prefab");
        yield return add();
    }
}
