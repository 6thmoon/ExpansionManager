using BepInEx.Logging;
using RoR2.ExpansionManagement;
using System.Security;
using System.Security.Permissions;
using Path = System.IO.Path;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace ExpansionManager;

[BepInPlugin(GUID, NAME, VERSION)]
public class ExpansionManagerPlugin : BaseUnityPlugin
{
    public const string
            GUID = "groovesalad." + NAME,
            NAME = "ExpansionManager",
            VERSION = "1.1.3";

    public static new ManualLogSource Logger { get; private set; }

    protected void Awake()
    {
        Logger = base.Logger;

        string directoryName = Path.GetDirectoryName(Info.Location);

        ExpansionManagerAssets.assetsPath = Path.Combine(directoryName, "AssetBundles", "expansionmanagerassets.bundle");
        ExpansionManagerAssets.ModInit();

        Language.collectLanguageRootFolders += list => list.Add(Path.Combine(directoryName, "Language"));
        On.RoR2.RuleDef.FromExpansion += ConfigureHiddenExpansions;

        ExpansionRulesCatalog.ModInit();
        ExpansionManagerUI.ModInit();
    }

    private RuleDef ConfigureHiddenExpansions(On.RoR2.RuleDef.orig_FromExpansion orig, ExpansionDef expansion)
    {
        RuleDef rule = orig(expansion);
        var key = from letter in Language.GetString(expansion.nameToken) where letter is not (
                '=' or '\n' or '\t' or '\\' or '"' or '\'' or '[' or ']'
            ) select letter;

        rule.hideLobbyDisplay = Config.Bind(
                "Hide Expansion", string.Join("", key), rule.hideLobbyDisplay
            ).Value;
        return rule;
    }
}
