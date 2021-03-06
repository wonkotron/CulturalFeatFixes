﻿using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using Helpers;  // From Taleworlds.CampaignSystem.dll
using System;

namespace CulturalFeatFixes.Patches {
    [HarmonyPatch(typeof(PerkHelper), "AddFeatBonusForPerson")]
    public class PerkHelperPatch {
        // @TODO - Refactor
        // Fixes issue where effect bonuses are incorrectly converted from percentage to fractional value
        // Effect bonuses are already stored in fractional values
        // See TaleWorlds.CampaignSystem.DefaultFeats.InitializeAll() [e1.1.0 beta as of 2020-04-14]

        public static void Apply() {
            var TargetMethodInfo = typeof(PerkHelper)
                .GetMethod(nameof(PerkHelper.AddFeatBonusForPerson), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            var PatchMethodInfo = typeof(PerkHelperPatch)
                .GetMethod(nameof(Prefix), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            SubModule.harmony.Patch(
                TargetMethodInfo,
                prefix: new HarmonyMethod(PatchMethodInfo));
        }

        static bool Prefix(FeatObject feat, CharacterObject character, ref ExplainedNumber bonuses) {
            if (character != null && character.GetFeatValue(feat)) {
                Patched_AddToStat(ref bonuses, feat.IncrementType, feat.EffectBonus, feat.Name);
            }

            return false;  // Skip real AddFeatBonusForPerson()
        }

        private static void Patched_AddToStat(ref ExplainedNumber stat, SkillEffect.EffectIncrementType effectIncrementType, float number, TextObject text) {
            if (effectIncrementType == SkillEffect.EffectIncrementType.Add) {
                stat.Add(number, text);
                return;
            }
            if (effectIncrementType == SkillEffect.EffectIncrementType.AddFactor) {
                stat.AddFactor(number, text);
            }
        }

        private static void Patched_AddToStat(ref ExplainedNumber stat, FeatObject.AdditionType additionType, float number, TextObject text) {
            if (additionType == FeatObject.AdditionType.Add) {
                stat.Add(number, text);
                return;
            }
            if (additionType == FeatObject.AdditionType.AddFactor) {
                stat.AddFactor(number, text);
            }
        }
    }
}