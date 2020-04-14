# CulturalFeatFixes
Fixes and improves implementation of Player Culture Feats in Mount and Blade II:  Bannerlord

This mod is currently a bug fix patch that affects player culture perks.  The goal is for this project is to find and fix issues with perks not being applied, then implement balancing improvements.  It uses the Harmony library (https://harmony.pardeike.net/).

For source code and to report issues, please see:
https://github.com/wonkotron/CulturalFeatFixes

**Game Version Baseline:**  e0.1.0 beta

**Mod Status**
1. Battanian and Khuzait movement bonuses are now applied (values seem high)
1. Sturgian movement bonus under test (snow terrain not registering?)
1. Vlandian XP bonus under test (need raw XP data)

**Mod Details**
1. Fixed a bug in the Helpers.PerkHelpers.AddToStat functions (TaleWorlds.CampaignSystem.dll) which made cultural feat bonus values negligible to the point of being rounded away.
1. Cutural feat bonuses in Party Speed menu now listed under feat.Name instead of generic "Feats"

**Installation Instructions:**
1. Vortex

**Compatibility Notes:**
1. Replaces Helpers.PerkHelper.AddFeatBonusForPerson()
