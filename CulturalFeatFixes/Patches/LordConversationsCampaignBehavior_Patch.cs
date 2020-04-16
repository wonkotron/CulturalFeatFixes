using System;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace CulturalFeatFixes.Patches {
    [HarmonyPatch]
    public class LordConversationsCampaignBehavior_Patch {
        // Thanks Community Patch devs!
        // https://github.com/Tyler-IN/MnB2-Bannerlord-CommunityPatch/blob/master/src/CommunityPatch/Patches/LordConversationsCampaignBehaviorPatch.cs

        private static readonly Type TargetType = Type.GetType("SandBox.LordConversationsCampaignBehavior, SandBox, Version=1.0.0.0, Culture=neutral");

        public static readonly MethodInfo TargetMethodInfo
            = TargetType.GetMethod(
                "conversation_magistrate_form_a_caravan_accept_on_consequence",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        public static readonly MethodInfo PatchMethodInfo_Prefix
            = typeof(LordConversationsCampaignBehavior_Patch)
            .GetMethod(nameof(Prefix), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

        public static readonly MethodInfo PatchMethodInfo_Postfix
            = typeof(LordConversationsCampaignBehavior_Patch)
            .GetMethod(nameof(Postfix), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

        public static void Apply() {
            CulturalFeatFixes.SubModule.harmony.Patch(
                TargetMethodInfo,
                prefix: new HarmonyMethod(PatchMethodInfo_Prefix),
                postfix: new HarmonyMethod(PatchMethodInfo_Postfix));
        }

        private static int mainHeroGold_beforeTransaction = 0;
        static void Prefix() {
            mainHeroGold_beforeTransaction = Hero.MainHero.Gold;
        }

        static void Postfix() {
            if (Hero.MainHero.Gold < mainHeroGold_beforeTransaction) {
                // presume transaction occurred
                if (Hero.MainHero.IsHumanPlayerCharacter &&
                    Hero.MainHero.CharacterObject.GetFeatValue(DefaultFeats.Cultural.AseraiCheapCaravans)) {

                    int creditAmount = mainHeroGold_beforeTransaction - Hero.MainHero.Gold;  // will modify later
                    if (DefaultFeats.Cultural.AseraiCheapCaravans.EffectBonus == 0f) {
                        // Assume 30%
                        creditAmount = (int)(creditAmount * 0.30f);
                    }
                    else {
                        creditAmount = (int)(creditAmount * (1.0f - DefaultFeats.Cultural.AseraiCheapCaravans.EffectBonus));
                    }
                    
                    // Flavor text
                    TextObject textObject = new TextObject("An Aserai clerk nods to you and slips a coinpurse into your hands.");
                    // StringHelpers.SetCharacterProperties("HERO", Hero.MainHero.CharacterObject, null, textObject);
                    InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));

                    // Apply credit
                    GiveGoldAction.ApplyForSettlementToCharacter(Settlement.CurrentSettlement, Hero.MainHero, creditAmount, false);
                }
            }
        }
    }
}
