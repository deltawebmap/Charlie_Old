using System;
using UassetToolkit;

namespace DeltaDataTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //UAssetFileBlueprint bp = UAssetFileBlueprint.OpenFile(@"C:\Program Files (x86)\Steam\steamapps\common\ARK\ShooterGame\Content\ScorchedEarth/WeaponPlantSpeciesY/PrimalItemConsumable_Seed_PlantSpeciesY.uasset", true, "PrimalItemConsumable_Seed_PlantSpeciesY", @"C:\Program Files (x86)\Steam\steamapps\common\ARK\ShooterGame\Content\");
            UAssetFileBlueprint bp = UAssetFileBlueprint.OpenFile(@"C:\Program Files (x86)\Steam\steamapps\common\ARK\ShooterGame\Content\PrimalEarth\Dinos\Yutyrannus\Yutyrannus_Character_BP.uasset", true, "Yutyrannus_Character_BP", @"C:\Program Files (x86)\Steam\steamapps\common\ARK\ShooterGame\Content\");
            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}
