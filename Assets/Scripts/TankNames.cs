using System;
using System.Linq;
using System.Collections.Generic;

#nullable enable

namespace Orbits
{
    public static class TankNames
    {
        #region Fields
        private static List<string> UnusedNames = new();
        public static readonly IReadOnlyList<string> AllNames = new string[] {
            "Tarntanya",
            "Ararat",
            "Armidale",
            "Arunta",
            "Ballarat",
            "Bathurst",
            "Benalla",
            "Meanjin",
            "Broome",
            "Canberra",
            "Childers",
            "Choules",
            "Collins",
            "Dechaineux",
            "Diamantina",
            "Farncomb",
            "Gascoyne",
            "Glenelg",
            "Hobart",
            "Larrakia",
            "Launceston",
            "Leeuwin",
            "Maitland",
            "Maryborough",
            "Melville",
            "Naarm",
            "Parramatta",
            "Boorloo",
            "Garramilla",
            "Nipaluna",
            "Rankin",
            "Sheean",
            "Shepparton",
            "Stalwart",
            "Stuart",
            "Supply",
            "Bondi",
            "Toowoomba",
            "Waller",
            "Warramunga",
            "Wollongong",
            "Yarra",
            "Relentless",
            "Cyclone",
            "Havoc",
            "Renegade",
            "Vigilant",
            "Triton",
            "Titan",
            "Eclipse",
            "Pendragon",
            "Myrmidon",
            "Victoria",
            "Tsunami",
            "Voltaire",
            "Python",
            "Vasily",
            "Zeppelin",
            "Baramundi",
       };
        #endregion

        #region Methods
        public static string GetRandomName()
        {
            if (UnusedNames.Count == 0)
            {
                UnusedNames = AllNames.ToList();
            }

            var index = UnityEngine.Random.Range(0, UnusedNames.Count);
            var result = UnusedNames[index];
            UnusedNames.RemoveAt(index);
            return "SST " + result;
        }
        #endregion
    }
}