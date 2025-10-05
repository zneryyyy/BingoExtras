using BingoMode.BingoRandomizer;
using BingoMode.BingoSteamworks;
using Expedition;
using Menu.Remix;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BingoMode.BingoChallenges
{
    using static ChallengeHooks;

    public class BingoMouthSpearRandomizer : ChallengeRandomizer
    {
        public Randomizer<int> amount;
        public Randomizer<string> region;
        public Randomizer<bool> oneCycle;

        public override Challenge Random()
        {
            BingoMouthSpearChallenge challenge = new();
            challenge.amount.Value = amount.Random();
            challenge.region.Value = region.Random();
            challenge.oneCycle.Value = oneCycle.Random();
            return challenge;
        }

        public override StringBuilder Serialize(string indent)
        {
            string surindent = indent + INDENT_INCREMENT;
            StringBuilder serializedContent = new();
            serializedContent.AppendLine($"{surindent}amount-{amount.Serialize(surindent)}");
            serializedContent.AppendLine($"{surindent}region-{region.Serialize(surindent)}");
            serializedContent.AppendLine($"{surindent}oneCycle-{oneCycle.Serialize(surindent)}");
            return base.Serialize(indent).Replace("__Type__", "MouthSpear").Replace("__Content__", serializedContent.ToString());
        }

        public override void Deserialize(string serialized)
        {
            Dictionary<string, string> dict = ToDict(serialized);
            amount = Randomizer<int>.InitDeserialize(dict["amount"]);
            region = Randomizer<string>.InitDeserialize(dict["region"]);
            oneCycle = Randomizer<bool>.InitDeserialize(dict["oneCycle"]);
        }
    }

    public class BingoMouthSpearChallenge : BingoChallenge
    {
        public const string NAME = nameof(BingoMouthSpearChallenge);
        public SettingBox<int> amount;
        public SettingBox<string> region;
        public SettingBox<bool> oneCycle;
        public int current;

        public BingoMouthSpearChallenge()
        {
            amount = new(0, "Amount", 0);
            oneCycle = new(false, "In one Cycle", 3);
            region = new("", "Region", 5, listName: "regions");
        }

        public override void UpdateDescription()
        {
            if (ChallengeTools.creatureNames == null)
            {
                ChallengeTools.CreatureName(ref ChallengeTools.creatureNames);
            }
            string location = region.Value != "Any Region" ? Region.GetRegionFullName(region.Value, ExpeditionData.slugcatPlayer) : "";
            description = ChallengeTools.IGT.Translate("Spear [<current>/<amount>] lizards in the mouth <location><onecycle>")
                .Replace("<current>", ValueConverter.ConvertToString(current))
                .Replace("<amount>", ValueConverter.ConvertToString(amount.Value))
                .Replace("<location>", location != "" ? " in " + location : "")
                .Replace("<onecycle>", oneCycle.Value ? " in one cycle" : "")
                ;
            base.UpdateDescription();
        }

        public override Phrase ConstructPhrase()
        {
            Phrase phrase = new([[new Icon("lizlick"), Icon.FromEntityName("Spear")]]);
            int lastLine = 1;
            if (region.Value != "Any Region")
            {
                phrase.InsertWord(new Verse(region.Value), 1);
                lastLine = 2;
            }

            phrase.InsertWord(new Counter(current, amount.Value), lastLine);
            if (oneCycle.Value) phrase.InsertWord(new Icon("cycle_limit"), lastLine);
            return phrase;
        }

        public override string ChallengeName()
        {
            return ChallengeTools.IGT.Translate("Spearing lizards in the mouth");
        }

        public override void Reset()
        {
            current = 0;
            base.Reset();
        }

        public override bool Duplicable(Challenge challenge)
        {
            return challenge is not BingoMouthSpearChallenge;
        }

        public override Challenge Generate()
        {
            bool oneCycle = UnityEngine.Random.value < 0.2f;
            int amount = UnityEngine.Random.Range(1, oneCycle ? 2 : 5);
            return new BingoMouthSpearChallenge()
            {
                amount = new(amount, "Amount", 0),
                oneCycle = new(oneCycle, "In one Cycle", 3),
                region = new("Any Region", "Region", 5, listName: "regions"),
            };
        }

        public override void Update()
        {
            base.Update();
            if (!completed && oneCycle.Value && game != null && game.cameras.Length > 0 && game.cameras[0].room != null && game.cameras[0].room.shelterDoor != null && game.cameras[0].room.shelterDoor.IsClosing)
            {
                if (current != 0)
                {
                    current = 0;
                    UpdateDescription();
                }
                return;
            }
        }

        public void Speared(string spearRegion)
        {
            if (completed || TeamsCompleted[SteamTest.team] || hidden || revealed || (region.Value != "Any Region" && spearRegion != region.Value)) return;
            current++;
            UpdateDescription();
            if (current >= amount.Value) CompleteChallenge();
            else ChangeValue();
        }

        public override bool CombatRequired()
        {
            return true;
        }

        public override int Points()
        {
            return 20;
        }

        public override string ToString()
        {
            return string.Concat(new string[]
            {
                NAME,
                "~",
                amount.ToString(),
                "><",
                current.ToString(),
                "><",
                region.ToString(),
                "><",
                oneCycle.ToString(),
                "><",
                completed ? "1" : "0",
                "><",
                revealed ? "1" : "0",
            });
        }

        public override void FromString(string args)
        {
            try
            {
                string[] array = Regex.Split(args, "><");
                amount = SettingBoxFromString(array[0]) as SettingBox<int>;
                current = int.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                region = SettingBoxFromString(array[2]) as SettingBox<string>;
                oneCycle = SettingBoxFromString(array[3]) as SettingBox<bool>;
                completed = (array[4] == "1");
                revealed = (array[5] == "1");
                UpdateDescription();
            }
            catch (Exception ex)
            {
                ExpLog.Log("ERROR: " + NAME + " FromString() encountered an error: " + ex.Message);
                throw ex;
            }
        }

        public override bool ValidForThisSlugcat(SlugcatStats.Name slugcat)
        {
            return true;
        }

        public override void AddHooks()
        {
            On.Lizard.SpearStick += Lizard_SpearStick;
        }

        public override void RemoveHooks()
        {
            On.Lizard.SpearStick -= Lizard_SpearStick;
        }


        public override List<object> Settings() => [amount, region, oneCycle];
 
        public bool Lizard_SpearStick(On.Lizard.orig_SpearStick orig, Lizard self, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos onAppendagePos, Vector2 direction)
        {
            bool stick = orig.Invoke(self, source, dmg, chunk, onAppendagePos, direction);
            if (stick && chunk.index == 0 && self.HitInMouth(direction)) 
            {
                for (int j = 0; j < ExpeditionData.challengeList.Count; j++)
                {
                    if (ExpeditionData.challengeList[j] is BingoMouthSpearChallenge c)
                    {
                        c.Speared(self.room.world.region.name);
                    }
                }
            }
            return stick;
        }

    }
}
