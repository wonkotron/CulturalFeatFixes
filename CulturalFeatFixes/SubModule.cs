using System;
using System.IO;
using CulturalFeatFixes.Models;
using CulturalFeatFixes.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CulturalFeatFixes {
    public class SubModule : MBSubModuleBase {
        public static readonly Harmony harmony = new Harmony("mod.bannerlord.wonkotron");
        public static readonly string ModuleName = "CulturalFeatFixes";

        // @TODO:  Refactor mod loading method for better maintainability
        protected override void OnSubModuleLoad() {
            base.OnSubModuleLoad();

            PerkHelperPatch.Apply();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject) {
            base.OnGameStart(game, gameStarterObject);

            AddModels(gameStarterObject as CampaignGameStarter);
        }

        public override void OnGameInitializationFinished(Game game) {            
            LordConversationsCampaignBehavior_Patch.Apply();

            base.OnGameInitializationFinished(game);
        }

        private void AddModels(CampaignGameStarter gameStarter) {
            if (gameStarter != null) {
                gameStarter.AddModel(new CFF_PartySpeedCalculatingModel());
            }
        }
    }
}
