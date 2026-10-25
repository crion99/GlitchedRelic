# Original relic coverage (STS2 0.111.0)

This inventory is generated from the installed game's relic classes. A hook name identifies an interaction point, **not** its effect. `Fragment` means its condition has a mapped event alias and its result/value is in the Dataminer pool; it does not mean the original relic's full implementation was copied. `Pending` means its unique effect has not been individually ported; generic events may still observe it. Current progress: **180 fragments, 119 pending**.

| # | Relic class | Declared hooks | Unique effect |
|---:|---|---|---|
| 1 | `Akabeko` | `AfterSideTurnStart` | Fragment |
| 2 | `AlchemicalCoffer` | `AfterObtained` | Fragment |
| 3 | `AmethystAubergine` | `AfterModifyingRewards` | Pending |
| 4 | `Anchor` | `BeforeCombatStart` | Fragment |
| 5 | `ArcaneScroll` | `AfterObtained` | Fragment |
| 6 | `ArchaicTooth` | `AfterCloned`, `AfterObtained` | Pending |
| 7 | `ArtOfWar` | `AfterCardPlayed`, `AfterCombatEnd`, `AfterEnergyReset`, `AfterSideTurnEnd` | Fragment |
| 8 | `Astrolabe` | `AfterObtained` | Fragment |
| 9 | `BagOfMarbles` | `BeforeSideTurnStart` | Fragment |
| 10 | `BagOfPreparation` | `ModifyHandDraw` | Fragment |
| 11 | `BeatingRemnant` | `AfterDamageReceived`, `AfterModifyingHpLostAfterOsty`, `BeforeSideTurnStart`, `ModifyHpLostAfterOsty` | Pending |
| 12 | `BeautifulBracelet` | `AfterObtained` | Fragment |
| 13 | `Bellows` | `AfterPlayerTurnStart` | Fragment |
| 14 | `BeltBuckle` | `AfterCombatEnd`, `AfterCombatVictory`, `AfterObtained`, `AfterPotionDiscarded`, `AfterPotionProcured`, `AfterPotionUsed`, `BeforeCombatStart` | Fragment |
| 15 | `BigHat` | `AfterSideTurnStart` | Fragment |
| 16 | `BigMushroom` | `AfterObtained`, `AfterRoomEntered`, `ModifyHandDraw` | Fragment |
| 17 | `BiiigHug` | `AfterObtained`, `AfterShuffle` | Fragment |
| 18 | `BingBong` | `AfterCardChangedPiles` | Pending |
| 19 | `BlackBlood` | `AfterCombatVictory` | Fragment |
| 20 | `BlackStar` | — | Pending |
| 21 | `BlessedAntler` | `BeforeHandDraw`, `ModifyMaxEnergy` | Fragment |
| 22 | `BloodSoakedRose` | `AfterObtained`, `ModifyMaxEnergy` | Fragment |
| 23 | `BloodVial` | `AfterPlayerTurnStartLate` | Fragment |
| 24 | `BoneFlute` | `AfterAttack` | Fragment |
| 25 | `BoneTea` | `AfterPlayerTurnStart` | Fragment |
| 26 | `Bookmark` | `AfterFlush` | Pending |
| 27 | `BookOfFiveRings` | `AfterCardChangedPiles` | Pending |
| 28 | `BookRepairKnife` | `AfterDiedToDoom` | Pending |
| 29 | `BoomingConch` | `AfterSideTurnStart`, `ModifyHandDraw` | Fragment |
| 30 | `BoundPhylactery` | `AfterEnergyResetLate`, `BeforeCombatStart` | Pending |
| 31 | `BowlerHat` | `AfterModifyingGoldGained`, `ModifyGoldGained` | Pending |
| 32 | `Bread` | `AfterSideTurnStart`, `ModifyMaxEnergy` | Fragment |
| 33 | `BrilliantScarf` | `AfterCardPlayed`, `AfterCombatEnd`, `BeforeSideTurnStart` | Pending |
| 34 | `Brimstone` | `AfterSideTurnStart` | Fragment |
| 35 | `BronzeScales` | `AfterRoomEntered` | Fragment |
| 36 | `BurningBlood` | `AfterCombatVictory` | Fragment |
| 37 | `BurningSticks` | `AfterCardExhausted`, `AfterCombatEnd`, `AfterRoomEntered` | Pending |
| 38 | `Byrdpip` | `AfterObtained`, `BeforeCombatStart` | Pending |
| 39 | `CallingBell` | `AfterObtained` | Fragment |
| 40 | `Candelabra` | `AfterSideTurnStart` | Fragment |
| 41 | `CaptainsWheel` | `AfterBlockCleared` | Fragment |
| 42 | `Cauldron` | `AfterObtained` | Fragment |
| 43 | `CentennialPuzzle` | `AfterCombatEnd`, `AfterDamageReceived` | Fragment |
| 44 | `Chandelier` | `AfterSideTurnStart` | Fragment |
| 45 | `CharonsAshes` | `AfterCardExhausted` | Fragment |
| 46 | `ChemicalX` | `BeforeCardPlayed`, `ModifyXValue` | Pending |
| 47 | `ChoicesParadox` | `AfterPlayerTurnStart` | Pending |
| 48 | `ChosenCheese` | `AfterCombatEnd` | Fragment |
| 49 | `Circlet` | — | Pending |
| 50 | `Claws` | `AfterObtained` | Fragment |
| 51 | `CloakClasp` | `BeforeSideTurnEnd` | Fragment |
| 52 | `CrackedCore` | `BeforeSideTurnStart` | Fragment |
| 53 | `Crossbow` | `AfterSideTurnStart` | Fragment |
| 54 | `CursedPearl` | `AfterObtained` | Fragment |
| 55 | `DarkstonePeriapt` | `AfterCardChangedPiles` | Fragment |
| 56 | `DataDisk` | `AfterRoomEntered` | Fragment |
| 57 | `DaughterOfTheWind` | `AfterCardPlayed` | Fragment |
| 58 | `DelicateFrond` | `BeforeCombatStart` | Pending |
| 59 | `DemonTongue` | `AfterDamageReceived`, `BeforeSideTurnStart` | Pending |
| 60 | `DeprecatedRelic` | — | Pending |
| 61 | `DiamondDiadem` | `AfterSideTurnStart` | Fragment |
| 62 | `DingyRug` | `ModifyCardRewardCreationOptions` | Pending |
| 63 | `DistinguishedCape` | `AfterObtained` | Fragment |
| 64 | `DivineDestiny` | `AfterSideTurnStart` | Fragment |
| 65 | `DivineRight` | `AfterRoomEntered` | Fragment |
| 66 | `DollysMirror` | `AfterObtained` | Pending |
| 67 | `DowsingRod` | `AfterObtained` | Fragment |
| 68 | `DragonFruit` | `AfterGoldGained` | Fragment |
| 69 | `DreamCatcher` | `ModifyExtraRestSiteHealText` | Pending |
| 70 | `Driftwood` | — | Pending |
| 71 | `DustyTome` | `AfterObtained` | Pending |
| 72 | `Ectoplasm` | `AfterModifyingGoldGained`, `ModifyGoldGained`, `ModifyMaxEnergy` | Pending |
| 73 | `ElectricShrymp` | `AfterObtained` | Fragment |
| 74 | `EmberTea` | `AfterRoomEntered` | Fragment |
| 75 | `EmotionChip` | `AfterCombatEnd`, `AfterDamageReceived`, `AfterPlayerTurnStart` | Pending |
| 76 | `EmptyCage` | `AfterObtained` | Fragment |
| 77 | `EternalFeather` | `AfterRoomEntered` | Fragment |
| 78 | `FakeAnchor` | `BeforeCombatStart` | Fragment |
| 79 | `FakeBloodVial` | `AfterPlayerTurnStartLate` | Fragment |
| 80 | `FakeHappyFlower` | `AfterCombatEnd`, `AfterSideTurnStart` | Fragment |
| 81 | `FakeLeesWaffle` | `AfterObtained` | Fragment |
| 82 | `FakeMango` | `AfterObtained` | Fragment |
| 83 | `FakeMerchantsRug` | — | Pending |
| 84 | `FakeOrichalcum` | `BeforeSideTurnEnd`, `BeforeSideTurnEndVeryEarly`, `BeforeSideTurnStart` | Fragment |
| 85 | `FakeSneckoEye` | `AfterObtained`, `BeforeCombatStart` | Pending |
| 86 | `FakeStrikeDummy` | `ModifyDamageAdditive` | Pending |
| 87 | `FakeVenerableTeaSet` | `AfterEnergyReset`, `AfterRoomEntered` | Fragment |
| 88 | `FencingManual` | `AfterSideTurnStart` | Pending |
| 89 | `FestivePopper` | `AfterPlayerTurnStart` | Fragment |
| 90 | `Fiddle` | `AfterPreventingDraw`, `ModifyHandDraw` | Pending |
| 91 | `FishingRod` | `AfterCombatEnd` | Pending |
| 92 | `ForgottenSoul` | `AfterCardExhausted` | Fragment |
| 93 | `FragrantMushroom` | `AfterObtained` | Fragment |
| 94 | `FresnelLens` | `ModifyMerchantCardCreationResults` | Pending |
| 95 | `FrozenEgg` | `ModifyMerchantCardCreationResults` | Pending |
| 96 | `FuneraryMask` | `BeforeHandDraw` | Fragment |
| 97 | `FurCoat` | `AfterCreatureAddedToCombat`, `AfterObtained`, `BeforeCombatStart`, `ModifyGeneratedMapLate` | Pending |
| 98 | `GalacticDust` | `AfterStarsSpent` | Pending |
| 99 | `GamblingChip` | `AfterPlayerTurnStart` | Fragment |
| 100 | `GamePiece` | `AfterCardPlayed` | Fragment |
| 101 | `GhostSeed` | `AfterCardEnteredCombat`, `AfterRoomEntered` | Pending |
| 102 | `Girya` | `AfterRoomEntered` | Fragment |
| 103 | `GlassEye` | `AfterObtained` | Pending |
| 104 | `Glitter` | — | Pending |
| 105 | `GnarledHammer` | `AfterObtained` | Fragment |
| 106 | `GoldenCompass` | `AfterObtained`, `ModifyGeneratedMap`, `ModifyUnknownMapPointRoomTypes` | Pending |
| 107 | `GoldenPearl` | `AfterObtained` | Fragment |
| 108 | `GoldPlatedCables` | `AfterModifyingOrbPassiveTriggerCount`, `ModifyOrbPassiveTriggerCounts` | Pending |
| 109 | `Gorget` | `AfterRoomEntered` | Fragment |
| 110 | `GremlinHorn` | `AfterDeath` | Fragment |
| 111 | `HandDrill` | `AfterBlockBroken` | Fragment |
| 112 | `HappyFlower` | `AfterCombatEnd`, `AfterSideTurnStart` | Fragment |
| 113 | `HeftyTablet` | `AfterObtained` | Fragment |
| 114 | `HelicalDart` | `AfterCardPlayed` | Fragment |
| 115 | `HistoryCourse` | `AfterAutoPrePlayPhaseEntered` | Pending |
| 116 | `HornCleat` | `AfterBlockCleared` | Fragment |
| 117 | `IceCream` | — | Pending |
| 118 | `InfusedCore` | `AfterSideTurnStart`, `ModifyOrbValue` | Pending |
| 119 | `IntimidatingHelmet` | `BeforeCardPlayed` | Fragment |
| 120 | `IronClub` | `AfterCardPlayed` | Fragment |
| 121 | `IvoryTile` | `AfterCardPlayed` | Fragment |
| 122 | `JeweledMask` | `BeforeHandDraw` | Fragment |
| 123 | `JewelryBox` | `AfterObtained` | Fragment |
| 124 | `JossPaper` | `AfterCardExhausted`, `AfterCombatEnd`, `AfterSideTurnEnd` | Fragment |
| 125 | `JuzuBracelet` | `ModifyUnknownMapPointRoomTypes` | Pending |
| 126 | `Kaleidoscope` | `AfterObtained` | Fragment |
| 127 | `Kifuda` | `AfterObtained` | Fragment |
| 128 | `Kunai` | `AfterCardPlayed`, `AfterCombatEnd`, `BeforeSideTurnStart` | Fragment |
| 129 | `Kusarigama` | `AfterCardPlayed`, `AfterCombatEnd`, `AfterSideTurnEnd`, `BeforeCombatStart` | Pending |
| 130 | `Lantern` | `AfterSideTurnStart` | Fragment |
| 131 | `LargeCapsule` | `AfterObtained` | Pending |
| 132 | `LastingCandy` | `BeforeCombatRewardOffered` | Pending |
| 133 | `LavaLamp` | `AfterDamageReceived`, `AfterRoomEntered` | Pending |
| 134 | `LavaRock` | — | Pending |
| 135 | `LeadPaperweight` | `AfterObtained` | Fragment |
| 136 | `LeafyPoultice` | `AfterObtained` | Fragment |
| 137 | `LeesWaffle` | `AfterObtained` | Fragment |
| 138 | `LetterOpener` | `AfterCardPlayed`, `AfterCombatEnd`, `AfterSideTurnStart`, `BeforeCombatStart` | Fragment |
| 139 | `LizardTail` | `AfterPreventingDeath` | Pending |
| 140 | `LoomingFruit` | `AfterObtained` | Fragment |
| 141 | `LordsParasol` | `AfterRoomEntered` | Pending |
| 142 | `LostCoffer` | `AfterObtained` | Fragment |
| 143 | `LostWisp` | `AfterCardPlayed` | Pending |
| 144 | `LuckyFysh` | `AfterCardChangedPiles` | Fragment |
| 145 | `LunarPastry` | `AfterSideTurnEnd` | Fragment |
| 146 | `Mango` | `AfterObtained` | Fragment |
| 147 | `MassiveScroll` | `AfterObtained` | Pending |
| 148 | `MawBank` | `AfterItemPurchased`, `AfterRoomEntered` | Fragment |
| 149 | `MealTicket` | `AfterRoomEntered` | Fragment |
| 150 | `MeatCleaver` | — | Pending |
| 151 | `MeatOnTheBone` | `AfterCombatVictoryEarly`, `AfterCurrentHpChanged`, `BeforeCombatStart` | Fragment |
| 152 | `MembershipCard` | `ModifyMerchantPrice` | Pending |
| 153 | `MercuryHourglass` | `AfterPlayerTurnStart` | Fragment |
| 154 | `Metronome` | `AfterCombatEnd`, `AfterOrbChanneled`, `AfterRoomEntered` | Pending |
| 155 | `MiniatureCannon` | `ModifyDamageAdditive` | Pending |
| 156 | `MiniatureTent` | — | Pending |
| 157 | `MiniRegent` | `AfterCombatEnd`, `AfterStarsSpent`, `BeforeSideTurnStart` | Fragment |
| 158 | `MoltenEgg` | `ModifyMerchantCardCreationResults` | Pending |
| 159 | `MrStruggles` | `AfterPlayerTurnStart` | Fragment |
| 160 | `MummifiedHand` | `AfterCardPlayed` | Fragment |
| 161 | `MusicBox` | `AfterCardPlayed`, `AfterCombatEnd`, `BeforeCardPlayed`, `BeforeSideTurnStart` | Pending |
| 162 | `MysticLighter` | `ModifyDamageAdditive` | Pending |
| 163 | `NeowsBones` | `AfterObtained` | Fragment |
| 164 | `NeowsSacrifice` | `AfterObtained` | Fragment |
| 165 | `NeowsTalisman` | `AfterObtained` | Fragment |
| 166 | `NeowsTorment` | `AfterObtained` | Fragment |
| 167 | `NewLeaf` | `AfterObtained` | Fragment |
| 168 | `NinjaScroll` | `BeforeHandDraw` | Fragment |
| 169 | `Nunchaku` | `AfterCardPlayed` | Fragment |
| 170 | `NutritiousOyster` | `AfterObtained` | Fragment |
| 171 | `NutritiousSoup` | `AfterObtained` | Fragment |
| 172 | `OddlySmoothStone` | `AfterRoomEntered` | Fragment |
| 173 | `OldCoin` | `AfterObtained` | Fragment |
| 174 | `OrangeDough` | `AfterSideTurnStart` | Fragment |
| 175 | `Orichalcum` | `BeforeSideTurnEnd`, `BeforeSideTurnEndVeryEarly`, `BeforeSideTurnStart` | Fragment |
| 176 | `OrnamentalFan` | `AfterCardPlayed`, `AfterCombatEnd`, `BeforeSideTurnStart` | Fragment |
| 177 | `Orrery` | `AfterObtained` | Fragment |
| 178 | `PaelsBlood` | `ModifyHandDraw` | Pending |
| 179 | `PaelsClaw` | `AfterObtained` | Fragment |
| 180 | `PaelsEye` | `AfterCombatEnd`, `AfterObtained`, `AfterSideTurnStart`, `AfterTakingExtraTurn`, `BeforeCardPlayed`, `BeforeSideTurnEndEarly` | Pending |
| 181 | `PaelsFlesh` | `AfterCombatEnd`, `AfterSideTurnStart`, `BeforeCombatStart`, `BeforeSideTurnStart`, `ModifyMaxEnergy` | Pending |
| 182 | `PaelsGrowth` | `AfterObtained` | Fragment |
| 183 | `PaelsHorn` | `AfterObtained` | Fragment |
| 184 | `PaelsLegion` | `AfterCardPlayed`, `AfterCombatEnd`, `AfterModifyingBlockAmount`, `AfterObtained`, `AfterSideTurnStart`, `BeforeCombatStart`, `ModifyBlockMultiplicative` | Pending |
| 185 | `PaelsTears` | `AfterCombatEnd`, `AfterSideTurnStart`, `BeforeSideTurnEnd` | Pending |
| 186 | `PaelsTooth` | `AfterCloned`, `AfterCombatEnd`, `AfterObtained` | Fragment |
| 187 | `PaelsWing` | — | Pending |
| 188 | `PandorasBox` | `AfterObtained` | Pending |
| 189 | `Pantograph` | `AfterRoomEntered`, `BeforeCombatStart` | Fragment |
| 190 | `PaperKrane` | — | Pending |
| 191 | `PaperPhrog` | — | Pending |
| 192 | `ParryingShield` | `AfterSideTurnEnd` | Fragment |
| 193 | `Pear` | `AfterObtained` | Fragment |
| 194 | `Pendulum` | `AfterCombatEnd`, `BeforeHandDraw`, `ModifyHandDraw` | Pending |
| 195 | `PenNib` | `AfterCardPlayed`, `BeforeCardPlayed`, `ModifyDamageMultiplicative` | Pending |
| 196 | `Permafrost` | `AfterCardPlayed`, `AfterRoomEntered` | Fragment |
| 197 | `PetrifiedToad` | `BeforeCombatStartLate` | Pending |
| 198 | `PhialHolster` | `AfterObtained` | Fragment |
| 199 | `PhilosophersStone` | `AfterCreatureAddedToCombat`, `AfterRoomEntered`, `ModifyMaxEnergy` | Pending |
| 200 | `PhylacteryUnbound` | `AfterSideTurnStart`, `BeforeCombatStart` | Pending |
| 201 | `Planisphere` | `AfterRoomEntered` | Pending |
| 202 | `Pocketwatch` | `AfterCardPlayed`, `AfterCombatEnd`, `AfterModifyingHandDraw`, `AfterSideTurnStart`, `BeforeSideTurnStart`, `ModifyHandDraw` | Fragment |
| 203 | `PollinousCore` | `AfterCombatEnd`, `AfterModifyingHandDraw`, `BeforeHandDraw`, `ModifyHandDraw` | Pending |
| 204 | `Pomander` | `AfterObtained` | Fragment |
| 205 | `PotionBelt` | `AfterObtained` | Fragment |
| 206 | `PowerCell` | `BeforeSideTurnStart` | Fragment |
| 207 | `PrayerWheel` | — | Pending |
| 208 | `PrecariousShears` | `AfterObtained` | Pending |
| 209 | `PreciseScissors` | `AfterObtained` | Fragment |
| 210 | `PreservedFog` | `AfterObtained` | Fragment |
| 211 | `PrismaticGem` | `ModifyCardRewardCreationOptions`, `ModifyMaxEnergy` | Pending |
| 212 | `PumpkinCandle` | `AfterCombatEnd`, `AfterObtained`, `ModifyMaxEnergy` | Pending |
| 213 | `PunchDagger` | `AfterObtained` | Fragment |
| 214 | `RadiantPearl` | `BeforeHandDraw` | Fragment |
| 215 | `RainbowRing` | `AfterCardPlayed`, `AfterCombatEnd`, `BeforeSideTurnStart` | Fragment |
| 216 | `RazorTooth` | `AfterCardPlayed` | Pending |
| 217 | `RedMask` | `BeforeSideTurnStart` | Fragment |
| 218 | `RedSkull` | `AfterCombatEnd`, `AfterCurrentHpChanged`, `AfterRoomEntered` | Pending |
| 219 | `Regalite` | `AfterCardGeneratedForCombat`, `AfterCombatEnd`, `BeforeSideTurnStart` | Fragment |
| 220 | `RegalPillow` | `AfterRestSiteHeal`, `AfterRoomEntered`, `ModifyExtraRestSiteHealText`, `ModifyRestSiteHealAmount` | Fragment |
| 221 | `ReptileTrinket` | `AfterPotionUsed` | Fragment |
| 222 | `RingingTriangle` | — | Pending |
| 223 | `RingOfTheDrake` | `ModifyHandDraw` | Fragment |
| 224 | `RingOfTheSnake` | `ModifyHandDraw` | Fragment |
| 225 | `RippleBasin` | `AfterCardPlayed`, `AfterCombatEnd`, `BeforeSideTurnEnd`, `BeforeSideTurnStart` | Pending |
| 226 | `RoyalPoison` | `AfterPlayerTurnStart` | Fragment |
| 227 | `RoyalStamp` | `AfterObtained` | Fragment |
| 228 | `RuinedHelmet` | `AfterCombatEnd`, `AfterModifyingPowerAmountReceived` | Pending |
| 229 | `RunicCapacitor` | `AfterSideTurnStart` | Fragment |
| 230 | `RunicPyramid` | — | Pending |
| 231 | `Sai` | `AfterSideTurnStart` | Fragment |
| 232 | `SandCastle` | `AfterObtained` | Fragment |
| 233 | `ScreamingFlagon` | `BeforeSideTurnEnd` | Fragment |
| 234 | `ScrollBoxes` | `AfterObtained` | Fragment |
| 235 | `SeaGlass` | `AfterObtained` | Fragment |
| 236 | `SealOfGold` | `AfterSideTurnStart` | Fragment |
| 237 | `SelfFormingClay` | `AfterDamageReceived` | Fragment |
| 238 | `SereTalon` | `AfterObtained` | Fragment |
| 239 | `Shovel` | — | Pending |
| 240 | `Shuriken` | `AfterCardPlayed`, `AfterCombatEnd`, `BeforeSideTurnStart` | Fragment |
| 241 | `SignetRing` | `AfterObtained` | Fragment |
| 242 | `SilkenTress` | `AfterModifyingCardRewardOptions`, `AfterObtained` | Pending |
| 243 | `SilverCrucible` | `AfterModifyingCardRewardOptions`, `AfterRoomEntered` | Pending |
| 244 | `SlingOfCourage` | `AfterRoomEntered` | Fragment |
| 245 | `SmallCapsule` | `AfterObtained` | Pending |
| 246 | `SneckoEye` | `AfterObtained`, `BeforeCombatStart`, `ModifyHandDraw` | Pending |
| 247 | `SneckoSkull` | `AfterModifyingPowerAmountGiven`, `ModifyPowerAmountGivenAdditive` | Fragment |
| 248 | `Sozu` | `ModifyMaxEnergy` | Pending |
| 249 | `SparklingRouge` | `AfterBlockCleared` | Fragment |
| 250 | `SpikedGauntlets` | `ModifyMaxEnergy` | Pending |
| 251 | `StoneCalendar` | `AfterCombatEnd`, `AfterRoomEntered`, `AfterSideTurnStart`, `BeforeSideTurnEnd` | Fragment |
| 252 | `StoneCracker` | `AfterRoomEntered` | Fragment |
| 253 | `StoneHumidifier` | `AfterRestSiteHeal`, `ModifyExtraRestSiteHealText` | Fragment |
| 254 | `Storybook` | `AfterObtained` | Fragment |
| 255 | `Strawberry` | `AfterObtained` | Fragment |
| 256 | `StrikeDummy` | `ModifyDamageAdditive` | Pending |
| 257 | `SturdyClamp` | `AfterPreventingBlockClear` | Pending |
| 258 | `SwordOfJade` | `AfterRoomEntered` | Fragment |
| 259 | `SwordOfStone` | `AfterCombatVictory` | Pending |
| 260 | `SymbioticVirus` | `AfterSideTurnStart` | Fragment |
| 261 | `TanxsWhistle` | `AfterObtained` | Fragment |
| 262 | `TeaOfDiscourtesy` | `BeforeCombatStart` | Fragment |
| 263 | `TheAbacus` | `AfterShuffle` | Fragment |
| 264 | `TheBoot` | `AfterModifyingHpLostAfterOsty`, `ModifyHpLostAfterOstyLate` | Pending |
| 265 | `TheCourier` | `ModifyMerchantPrice` | Pending |
| 266 | `ThrowingAxe` | `AfterCombatEnd`, `AfterModifyingCardPlayCount`, `AfterRoomEntered`, `ModifyCardPlayCount` | Pending |
| 267 | `Tingsha` | `AfterCardDiscarded` | Fragment |
| 268 | `TinyMailbox` | `ModifyExtraRestSiteHealText` | Pending |
| 269 | `ToastyMittens` | `AfterPlayerTurnStart` | Fragment |
| 270 | `Toolbox` | `BeforeHandDraw` | Pending |
| 271 | `TouchOfOrobas` | `AfterCloned`, `AfterObtained` | Pending |
| 272 | `ToughBandages` | `AfterCardDiscarded` | Fragment |
| 273 | `ToxicEgg` | `ModifyMerchantCardCreationResults` | Pending |
| 274 | `ToyBox` | `AfterCombatEnd`, `AfterObtained` | Pending |
| 275 | `TriBoomerang` | `AfterObtained` | Fragment |
| 276 | `TungstenRod` | `AfterModifyingHpLostAfterOsty`, `ModifyHpLostAfterOsty` | Pending |
| 277 | `TuningFork` | `AfterCardPlayed` | Fragment |
| 278 | `TwistedFunnel` | `BeforeSideTurnStart` | Fragment |
| 279 | `UnceasingTop` | `AfterHandEmptied` | Fragment |
| 280 | `UndyingSigil` | `ModifyDamageMultiplicative` | Pending |
| 281 | `UnsettlingLamp` | `AfterCardPlayed`, `AfterCombatEnd`, `BeforeCombatStart`, `BeforePowerAmountChanged`, `ModifyPowerAmountGivenMultiplicative` | Pending |
| 282 | `Vajra` | `AfterRoomEntered` | Fragment |
| 283 | `Vambrace` | `AfterCardPlayed`, `AfterCombatEnd`, `AfterModifyingBlockAmount`, `BeforeCombatStart`, `ModifyBlockMultiplicative` | Pending |
| 284 | `VelvetChoker` | `AfterCardPlayed`, `AfterCombatEnd`, `AfterRoomEntered`, `BeforeSideTurnStart`, `ModifyMaxEnergy` | Pending |
| 285 | `VenerableTeaSet` | `AfterEnergyReset`, `AfterRoomEntered` | Fragment |
| 286 | `VeryHotCocoa` | `AfterSideTurnStart` | Fragment |
| 287 | `VexingPuzzlebox` | `AfterPlayerTurnStart` | Fragment |
| 288 | `VitruvianMinion` | `ModifyBlockMultiplicative`, `ModifyDamageMultiplicative` | Pending |
| 289 | `WarHammer` | `AfterCombatVictory` | Fragment |
| 290 | `WarPaint` | `AfterObtained` | Fragment |
| 291 | `Whetstone` | `AfterObtained` | Fragment |
| 292 | `WhisperingEarring` | `AfterAutoPrePlayPhaseEnteredLate`, `ModifyMaxEnergy` | Pending |
| 293 | `WhiteBeastStatue` | — | Pending |
| 294 | `WhiteStar` | — | Pending |
| 295 | `WingCharm` | — | Pending |
| 296 | `WingedBoots` | `AfterRoomEntered` | Pending |
| 297 | `WongoCustomerAppreciationBadge` | — | Pending |
| 298 | `WongosMysteryTicket` | `AfterCombatEnd`, `AfterModifyingRewards` | Pending |
| 299 | `YummyCookie` | `AfterCloned`, `AfterObtained` | Fragment |
