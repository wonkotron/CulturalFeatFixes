using System;
using Helpers;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;
using System.Security.Cryptography;

namespace CulturalFeatFixes.Models {
    public class CFF_PartySpeedCalculatingModel : DefaultPartySpeedCalculatingModel {
        public override float CalculatePureSpeed(MobileParty mobileParty, StatExplainer explanation, int additionalTroopOnFootCount = 0, int additionalTroopOnHorseCount = 0) {
			if (mobileParty.Army != null && mobileParty.Army.LeaderParty.AttachedParties.Contains(mobileParty)) {
				return this.CalculatePureSpeed(mobileParty.Army.LeaderParty, explanation, 0, 0);
			}

			PartyBase party = mobileParty.Party;
			int numberOfAvailableMounts = 0;
			float totalWeightCarried = 0f;
			int herdSize = 0;
			int menCount = mobileParty.MemberRoster.TotalManCount + additionalTroopOnFootCount + additionalTroopOnHorseCount;

			AddCargoStats(mobileParty, ref numberOfAvailableMounts, ref totalWeightCarried, ref herdSize);

			float totalWeightOfItems = GetTotalWeightOfItems(mobileParty);
			int inventryCapacity = Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(mobileParty, null, additionalTroopOnFootCount, additionalTroopOnHorseCount, 0, false);
			int horsemenCount = party.NumberOfMenWithHorse + additionalTroopOnHorseCount;
			int footmenCount = party.NumberOfMenWithoutHorse + additionalTroopOnFootCount;
			int woundedCount = party.MemberRoster.TotalWounded;
			int prisonerCount = party.PrisonRoster.TotalManCount;
			float morale = mobileParty.Morale;
			if (mobileParty.AttachedParties.Count != 0) {
				foreach (MobileParty attachedParty in mobileParty.AttachedParties) {
					AddCargoStats(attachedParty, ref numberOfAvailableMounts, ref totalWeightCarried, ref herdSize);
					menCount += attachedParty.MemberRoster.TotalManCount;
					totalWeightOfItems += GetTotalWeightOfItems(attachedParty);
					inventryCapacity += Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(attachedParty, null, 0, 0, 0, false);
					horsemenCount += attachedParty.Party.NumberOfMenWithHorse;
					footmenCount += attachedParty.Party.NumberOfMenWithoutHorse;
					woundedCount += attachedParty.MemberRoster.TotalWounded;
					prisonerCount += attachedParty.PrisonRoster.TotalManCount;
				}
			}
			float baseNumber = CalculateBaseSpeedForParty(menCount);
			ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber, explanation, null);
			float cavalryRatioModifier = this.GetCavalryRatioModifier(menCount, horsemenCount);
			explainedNumber.AddFactor(cavalryRatioModifier, _textCavalry);

			int min_footmenCount_numberOfAvailableMounts = Math.Min(footmenCount, numberOfAvailableMounts);
			float mountedFootmenRatioModifier = this.GetMountedFootmenRatioModifier(menCount, min_footmenCount_numberOfAvailableMounts);
			explainedNumber.AddFactor(mountedFootmenRatioModifier, _textMountedFootmen);

			if (mobileParty.Leader != null) {
				// @CFF - Calculates off of base value instead of (cavalry + footmen on horses) * bonus
				// PerkHelper.AddFeatBonusForPerson(DefaultFeats.Cultural.KhuzaitCavalryAgility, mobileParty.Leader, ref explainedNumber);

				// @CFF - Replace call to PerkHelper.AddFeatBonusForPerson and subsequent private calls
				if (mobileParty.Leader != null && mobileParty.Leader.GetFeatValue(DefaultFeats.Cultural.KhuzaitCavalryAgility)) {
					if (DefaultFeats.Cultural.KhuzaitCavalryAgility.IncrementType == FeatObject.AdditionType.AddFactor) {
						// Add khuzait bonus based on cavalry horsemen bonus already applied instead of base
						float khuzaitBonusFactor = (cavalryRatioModifier + mountedFootmenRatioModifier) * DefaultFeats.Cultural.KhuzaitCavalryAgility.EffectBonus;
						explainedNumber.AddFactor(khuzaitBonusFactor, DefaultFeats.Cultural.KhuzaitCavalryAgility.Name);
					}
				}
			}
			
			float num12 = Math.Min(totalWeightOfItems, (float)inventryCapacity);
			if (num12 > 0f) {
				float cargoEffect = this.GetCargoEffect(num12, inventryCapacity);
				explainedNumber.AddFactor(cargoEffect, _textCargo);
			}
			if (totalWeightCarried > (float)inventryCapacity) {
				float overBurdenedEffect = this.GetOverBurdenedEffect(totalWeightCarried - (float)inventryCapacity, inventryCapacity);
				explainedNumber.AddFactor(overBurdenedEffect, _textOverburdened);
			}
			if (mobileParty.Party.NumberOfAllMembers > mobileParty.Party.PartySizeLimit) {
				float overPartySizeEffect = this.GetOverPartySizeEffect(mobileParty);
				explainedNumber.AddFactor(overPartySizeEffect, _textOverPartySize);
			}
			if (mobileParty.Party.NumberOfPrisoners > mobileParty.Party.PrisonerSizeLimit) {
				float overPrisonerSizeEffect = this.GetOverPrisonerSizeEffect(mobileParty);
				explainedNumber.AddFactor(overPrisonerSizeEffect, _textOverPrisonerSize);
			}
			herdSize += Math.Max(0, numberOfAvailableMounts - min_footmenCount_numberOfAvailableMounts);
			float herdingModifier = this.GetHerdingModifier(menCount, herdSize);
			explainedNumber.AddFactor(herdingModifier, _textHerd);
			float woundedModifier = this.GetWoundedModifier(menCount, woundedCount, mobileParty);
			explainedNumber.AddFactor(woundedModifier, _textWounded);
			float sizeModifierPrisoner = GetSizeModifierPrisoner(menCount, prisonerCount);
			explainedNumber.AddFactor(1f / sizeModifierPrisoner - 1f, _textPrisoners);
			if (morale > 70f) {
				explainedNumber.AddFactor(0.05f * ((morale - 70f) / 30f), _textHighMorale);
			}
			if (morale < 30f) {
				explainedNumber.AddFactor(-0.1f * (1f - mobileParty.Morale / 30f), _textLowMorale);
			}
			if (mobileParty == MobileParty.MainParty) {
				float playerMapMovementSpeedBonusMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerMapMovementSpeedBonusMultiplier();
				if (playerMapMovementSpeedBonusMultiplier != 0f) {
					explainedNumber.AddFactor(playerMapMovementSpeedBonusMultiplier, _difficulty);
				}
			}
			if (mobileParty.IsDisorganized) {
				explainedNumber.AddFactor(-0.3f, _textDisorganized);
			}
			explainedNumber.LimitMin(1f);
			return explainedNumber.ResultNumber;
		}

		/*
		 * Private functions pulled from decompiled binary (unmodified)
		 */
		// Token: 0x06002007 RID: 8199 RVA: 0x00082F98 File Offset: 0x00081198
		private static void AddCargoStats(MobileParty mobileParty, ref int numberOfAvailableMounts, ref float totalWeightCarried, ref int herdSize) {
			ItemRoster itemRoster = mobileParty.ItemRoster;
			int numberOfPackAnimals = itemRoster.NumberOfPackAnimals;
			int numberOfLivestockAnimals = itemRoster.NumberOfLivestockAnimals;
			herdSize += numberOfPackAnimals + numberOfLivestockAnimals;
			numberOfAvailableMounts += itemRoster.NumberOfMounts;
			totalWeightCarried += itemRoster.TotalWeight;
		}

		// Token: 0x06002008 RID: 8200 RVA: 0x00082FD8 File Offset: 0x000811D8
		private float CalculateBaseSpeedForParty(int menCount) {
			return (float)(5.0 * Math.Pow((double)(200f / (200f + (float)menCount)), 0.40000000596046448));
		}

		// Token: 0x0600200A RID: 8202 RVA: 0x000830E8 File Offset: 0x000812E8
		private static float GetTotalWeightOfItems(MobileParty mobileParty) {
			float num = 0f;
			for (int i = 0; i < mobileParty.ItemRoster.Count; i++) {
				ItemRosterElement elementCopyAtIndex = mobileParty.ItemRoster.GetElementCopyAtIndex(i);
				if (!elementCopyAtIndex.EquipmentElement.Item.IsMountable && !elementCopyAtIndex.EquipmentElement.Item.IsAnimal) {
					num += (float)elementCopyAtIndex.Amount * elementCopyAtIndex.EquipmentElement.Weight;
				}
			}
			return num;
		}

		// Token: 0x0600200B RID: 8203 RVA: 0x00083166 File Offset: 0x00081366
		private float GetCargoEffect(float weightCarried, int partyCapacity) {
			return -0.02f * weightCarried / (float)partyCapacity;
		}

		// Token: 0x0600200C RID: 8204 RVA: 0x00083173 File Offset: 0x00081373
		private float GetOverBurdenedEffect(float totalWeightCarried, int partyCapacity) {
			return -0.4f * (totalWeightCarried / (float)partyCapacity);
		}

		// Token: 0x0600200D RID: 8205 RVA: 0x00083180 File Offset: 0x00081380
		private float GetOverPartySizeEffect(MobileParty mobileParty) {
			int partySizeLimit = mobileParty.Party.PartySizeLimit;
			int numberOfAllMembers = mobileParty.Party.NumberOfAllMembers;
			return 1f / ((float)numberOfAllMembers / (float)partySizeLimit) - 1f;
		}

		// Token: 0x0600200E RID: 8206 RVA: 0x000831B8 File Offset: 0x000813B8
		private float GetOverPrisonerSizeEffect(MobileParty mobileParty) {
			int prisonerSizeLimit = mobileParty.Party.PrisonerSizeLimit;
			int numberOfPrisoners = mobileParty.Party.NumberOfPrisoners;
			return 1f / ((float)numberOfPrisoners / (float)prisonerSizeLimit) - 1f;
		}

		// Token: 0x0600200F RID: 8207 RVA: 0x000831EE File Offset: 0x000813EE
		private float GetHerdingModifier(int totalMenCount, int herdSize) {
			herdSize -= totalMenCount;
			if (herdSize <= 0) {
				return 0f;
			}
			if (totalMenCount == 0) {
				return -0.8f;
			}
			return Math.Max(-0.8f, -0.02f * (float)herdSize / (float)totalMenCount);
		}

		// Token: 0x06002010 RID: 8208 RVA: 0x00083220 File Offset: 0x00081420
		private float GetWoundedModifier(int totalMenCount, int numWounded, MobileParty party) {
			numWounded -= totalMenCount / 4;
			if (numWounded <= 0) {
				return 0f;
			}
			if (totalMenCount == 0) {
				return -0.5f;
			}
			float baseNumber = Math.Max(-0.8f, -0.05f * (float)numWounded / (float)totalMenCount);
			ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber, null);
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.MobileAid, party, ref explainedNumber);
			return explainedNumber.ResultNumber;
		}

		// Token: 0x06002011 RID: 8209 RVA: 0x0008327A File Offset: 0x0008147A
		private float GetCavalryRatioModifier(int totalMenCount, int totalCavalryCount) {
			if (totalMenCount == 0) {
				return 0f;
			}
			return 0.6f * (float)totalCavalryCount / (float)totalMenCount;
		}

		// Token: 0x06002012 RID: 8210 RVA: 0x00083290 File Offset: 0x00081490
		private float GetMountedFootmenRatioModifier(int totalMenCount, int totalCavalryCount) {
			if (totalMenCount == 0) {
				return 0f;
			}
			return 0.3f * (float)totalCavalryCount / (float)totalMenCount;
		}

		// Token: 0x06002013 RID: 8211 RVA: 0x000832A6 File Offset: 0x000814A6
		private static float GetSizeModifierWounded(int totalMenCount, int totalWoundedMenCount) {
			return (float)Math.Pow((double)((10f + (float)totalMenCount) / (10f + (float)totalMenCount - (float)totalWoundedMenCount)), 0.33000001311302185);
		}

		// Token: 0x06002014 RID: 8212 RVA: 0x000832CC File Offset: 0x000814CC
		private static float GetSizeModifierPrisoner(int totalMenCount, int totalPrisonerCount) {
			return (float)Math.Pow((double)((10f + (float)totalMenCount + (float)totalPrisonerCount) / (10f + (float)totalMenCount)), 0.33000001311302185);
		}

		/*
		 * Private variables pulled from decompiled source (unmodified)
		 */
		// Token: 0x04000CB4 RID: 3252
		private static readonly TextObject _textCargo = new TextObject("{=fSGY71wd}Cargo within capacity", null);

		// Token: 0x04000CB5 RID: 3253
		private static readonly TextObject _textOverburdened = new TextObject("{=xgO3cCgR}Overburdened", null);

		// Token: 0x04000CB6 RID: 3254
		private static readonly TextObject _textOverPartySize = new TextObject("{=bO5gL3FI}Men within party size", null);

		// Token: 0x04000CB7 RID: 3255
		private static readonly TextObject _textOverPrisonerSize = new TextObject("{=Ix8YjLPD}Men within prisoner size", null);

		// Token: 0x04000CB8 RID: 3256
		private static readonly TextObject _textCavalry = new TextObject("{=YVGtcLHF}Cavalry", null);

		// Token: 0x04000CB9 RID: 3257
		private static readonly TextObject _textKhuzaitCavalryBonus = new TextObject("{=yi07dBks}Khuzait Cavalry Bonus", null);

		// Token: 0x04000CBA RID: 3258
		private static readonly TextObject _textMountedFootmen = new TextObject("{=5bSWSaPl}Footmen on horses", null);

		// Token: 0x04000CBB RID: 3259
		private static readonly TextObject _textWounded = new TextObject("{=aLsVKIRy}Wounded Members", null);

		// Token: 0x04000CBC RID: 3260
		private static readonly TextObject _textPrisoners = new TextObject("{=N6QTvjMf}Prisoners", null);

		// Token: 0x04000CBD RID: 3261
		private static readonly TextObject _textHerd = new TextObject("{=NhAMSaWU}Herd", null);

		// Token: 0x04000CBE RID: 3262
		private static readonly TextObject _difficulty = new TextObject("{=uG2Alcat}Game Difficulty", null);

		// Token: 0x04000CBF RID: 3263
		private static readonly TextObject _textHighMorale = new TextObject("{=aDQcIGfH}High Morale", null);

		// Token: 0x04000CC0 RID: 3264
		private static readonly TextObject _textLowMorale = new TextObject("{=ydspCDIy}Low Morale", null);

		// Token: 0x04000CC1 RID: 3265
		private static readonly TextObject _textDisorganized = new TextObject("{=JuwBb2Yg}Disorganized", null);

		// Token: 0x04000CC2 RID: 3266
		private static readonly TextObject _movingInForest = new TextObject("{=rTFaZCdY}Forest", null);

		// Token: 0x04000CC3 RID: 3267
		private static readonly TextObject _fordEffect = new TextObject("{=NT5fwUuJ}Fording", null);

		// Token: 0x04000CC4 RID: 3268
		private static readonly TextObject _night = new TextObject("{=fAxjyMt5}Night", null);

		// Token: 0x04000CC5 RID: 3269
		private static readonly TextObject _snow = new TextObject("{=vLjgcdgB}Snow", null);

		// Token: 0x04000CC6 RID: 3270
		private static readonly TextObject _desert = new TextObject("{=ecUwABe2}Desert", null);

		// Token: 0x04000CC7 RID: 3271
		private static readonly TextObject _sturgiaSnowBonus = new TextObject("{=0VfEGekD}Sturgia Snow Bonus", null);

		// Token: 0x04000CC8 RID: 3272
		private const float BaseSpeed = 5f;

		// Token: 0x04000CC9 RID: 3273
		private const float MininumSpeed = 1f;

		// Token: 0x04000CCA RID: 3274
		private const float MovingAtForestEffect = -0.3f;

		// Token: 0x04000CCB RID: 3275
		private const float MovingAtWaterEffect = -0.3f;

		// Token: 0x04000CCC RID: 3276
		private const float MovingAtNightEffect = -0.25f;

		// Token: 0x04000CCD RID: 3277
		private const float MovingOnSnowEffect = -0.1f;

		// Token: 0x04000CCE RID: 3278
		private const float MovingInDesertEffect = -0.15f;

		// Token: 0x04000CCF RID: 3279
		private const float CavalryEffect = 0.6f;

		// Token: 0x04000CD0 RID: 3280
		private const float MountedFootMenEffect = 0.3f;

		// Token: 0x04000CD1 RID: 3281
		private const float HerdEffect = -0.02f;

		// Token: 0x04000CD2 RID: 3282
		private const float WoundedEffect = -0.05f;

		// Token: 0x04000CD3 RID: 3283
		private const float CargoEffect = -0.02f;

		// Token: 0x04000CD4 RID: 3284
		private const float OverburdenedEffect = -0.4f;

		// Token: 0x04000CD5 RID: 3285
		private const float HighMoraleThresold = 70f;

		// Token: 0x04000CD6 RID: 3286
		private const float LowMoraleThresold = 30f;

		// Token: 0x04000CD7 RID: 3287
		private const float HighMoraleEffect = 0.05f;

		// Token: 0x04000CD8 RID: 3288
		private const float LowMoraleEffect = -0.1f;

		// Token: 0x04000CD9 RID: 3289
		private const float DisorganizedEffect = -0.3f;
	}
}

