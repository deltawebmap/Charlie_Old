using ArkImportTools.OutputEntities;
using System;
using System.Collections.Generic;
using System.Text;
using UassetToolkit;
using LibDeltaSystem.Entities.ArkEntries.Dinosaur;

namespace ArkImportTools
{
    public static class ArkStatsRipper
    {
        public static void DoRipStats(PropertyReader reader, DinosaurEntry entry)
        {
            //Create arrays
            entry.baseLevel = new float[12];
            entry.increasePerWildLevel = new float[12];
            entry.increasePerTamedLevel = new float[12];
            entry.additiveTamingBonus = new float[12];
            entry.multiplicativeTamingBonus = new float[12];

            //Loop through ARK indexes
            for (int i = 0; i<=11; i++)
            {
                //Calculate multipliers
                bool can_level = true;// (i == 2) || (reader.GetPropertyByte("CanLevelUpValue", CANLEVELUP_VALUES[i], i) == 1);
                int add_one = IS_PERCENT_STAT[i];
                float zero_mult = can_level ? 1 : 0;
                float ETHM = reader.GetPropertyFloat("ExtraTamedHealthMultiplier", EXTRA_MULTS_VALUES[i], i);

                //Add stat data
                entry.baseLevel[i] = MathF.Round(reader.GetPropertyFloat("MaxStatusValues", BASE_VALUES[i], i) + add_one, ROUND_PERCISION);
                entry.increasePerWildLevel[i] = MathF.Round(reader.GetPropertyFloat("AmountMaxGainedPerLevelUpValue", IW_VALUES[i], i) * zero_mult, ROUND_PERCISION);
                entry.increasePerTamedLevel[i] = MathF.Round(reader.GetPropertyFloat("AmountMaxGainedPerLevelUpValueTamed", 0, i) * ETHM * zero_mult, ROUND_PERCISION);
                entry.additiveTamingBonus[i] = MathF.Round(reader.GetPropertyFloat("TamingMaxStatAdditions", 0, i), ROUND_PERCISION);
                entry.multiplicativeTamingBonus[i] = MathF.Round(reader.GetPropertyFloat("TamingMaxStatMultipliers", 0, i), ROUND_PERCISION);
            }
        }

        public const int ROUND_PERCISION = 6;

        /* New defaults */
        //https://github.com/arkutils/Purlovia/blob/f25dd80a06930f0d34beacd03dafc5f9cecb054e/ark/defaults.py
        public const float FEMALE_MINTIMEBETWEENMATING_DEFAULT = 64800.0f;
        public const float FEMALE_MAXTIMEBETWEENMATING_DEFAULT = 172800.0f;

        public const float BABYGESTATIONSPEED_DEFAULT = 0.000035f;

        public static readonly float[] BASE_VALUES = new float[] { 100, 100, 100, 100, 100, 100, 0, 0, 0, 0, 0, 0 };
        public static readonly float[] IW_VALUES = new float[] {0, 0, 0.06f, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        public static readonly float[] IMPRINT_VALUES = new float[] {0.2f, 0, 0.2f, 0, 0.2f, 0.2f, 0, 0.2f, 0.2f, 0.2f, 0, 0};
        public static readonly float[] EXTRA_MULTS_VALUES = new float[] {1.35f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
        public static readonly float[] DONTUSESTAT_VALUES = new float[] {0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 1};
        public static readonly byte[] CANLEVELUP_VALUES = new byte[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        public static readonly int[] IS_PERCENT_STAT = new int[] {0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1};
    }
}
