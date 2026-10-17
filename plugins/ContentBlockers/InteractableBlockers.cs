namespace ExpansionManager.ContentBlockers;

public static class InteractableBlockers
{
    [SystemInitializer]
    private static void Init()
    {
        Stage.onServerStageBegin += Stage_onServerStageBegin;
        On.RoR2.DLC4ProgressionController.OnEnable += DLC4ProgressionController_OnEnable;
    }

    private static void Stage_onServerStageBegin(Stage instance)
    {
        SceneDef scene = instance.sceneDef;
        if (scene && Run.instance.AreExpansionInteractablesDisabled(DLC3Content.DLC3) && scene.cachedName is "computationalexchange")
        {
            GameObject obj = GameObject.Find("/HOLDER: Interactables/GROUP: Drone Zone/DroneCombinerStation");
            if (obj)
            {
                obj.SetActive(false);
            }
        }
    }

    private static void DLC4ProgressionController_OnEnable(On.RoR2.DLC4ProgressionController.orig_OnEnable orig, DLC4ProgressionController self)
    {
        orig(self);
        if (Run.instance.AreExpansionInteractablesDisabled(DLC4Content.DLC4))
        {
            SceneDirector.onPrePopulateSceneServer -= self.OnPrePopulateSceneServer;
            TeleporterInteraction.onTeleporterChargedGlobal -= self.HandleTeleporterCompleted;
        }
    }
}
