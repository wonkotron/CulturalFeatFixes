using CulturalFeatFixes.Models;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
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

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject) {
            base.OnGameStart(game, gameStarterObject);

            AddModels(gameStarterObject as CampaignGameStarter);
        }

        private void AddModels(CampaignGameStarter gameStarter) {
            if (gameStarter != null) {
                gameStarter.AddModel(new CFF_PartySpeedCalculatingModel());
            }
        }
    }
}
