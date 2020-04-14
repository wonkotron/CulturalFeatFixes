using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CulturalFeatFixes {
    public class SubModule : MBSubModuleBase {
        public static readonly string ModuleName = "CulturalFeatFixes";

        protected override void OnSubModuleLoad() {
            base.OnSubModuleLoad();

            var harmony = new Harmony("mod.bannerlord.wonkotron");
            harmony.PatchAll();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot() {
            InformationManager.DisplayMessage(new InformationMessage("Loaded " + ModuleName, Color.White));
        }
    }
}
