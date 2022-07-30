using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Content.Shared.Sound;
using Content.Shared.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules
{
    public sealed class SocietiesRuleSystem : GameRuleSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        public override string Prototype => "Societies"; 

        public int configSocietyCount = 2; //TODO config, must be <= societies.Count

        public List<SecretSociety> allSocieties = new List<SecretSociety>();
        public List<SecretSociety> thisRoundsSocieties = new List<SecretSociety>();

        public override void Initialize()
        {
            base.Initialize();
            Logger.Info$("societies", "Initalizing Societies!");

            //TODO: move this stuff to prototypes and yml. Here for testing convenience.
            allSocieties = new List<SecretSociety>() {
                new SecretSociety() {
                    Name = "Mortality Discussion Kollective",
                    Description = "",
                    Abbreviation = "MDK",
                    AllObjectives = new List<SecretSocietyObjective>() {
                        new SecretSocietyObjective() {
                            Name = "Kill The Lights",
                            Description = "Kill the Lights",

                        }
                    }
                },
                new SecretSociety(){
                    Name = "Electochemistry Enthusiasts",
                    Description = "",
                    Abbreviation = "EE",
                }
            };

            SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
            SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
            SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);

            Logger.Info$("societies", "Initalized!");
        }

        private void OnStartAttempt(RoundStartAttemptEvent ev)
        {

        }

        private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
        {
            if (!RuleAdded)
                return;

            PopulateSocieties(ev);

            foreach (var s in thisRoundsSocieties)
            {
                GreetSocietyMembers(s);
                foreach (var m in s.Members)
                {
                    var playerMind = m.Data.ContentData()?.Mind;
                    if (playerMind == null)
                        continue;

                    //TODO LOC
                    playerMind.Briefing = "The codewords for " + m.Name + " are " + String.Join(",", s.Codewords);
                }
            }

        }

        private void OnRoundEndText(RoundEndTextAppendEvent ev)
        {
            if (!RuleAdded)
                return;

        }

        public override void Started()
        {
            //throw new NotImplementedException();
            foreach (var s in allSocieties) //redo all these every round, in case admins add the others.
                s.Codewords = MakeCodewords();

            //pick societies
            thisRoundsSocieties = allSocieties.OrderBy(s => _random.Next()).Take(configSocietyCount).ToList();


            //pick objectives.
        }

        public override void Ended()
        {
            //throw new NotImplementedException();
        }


        //Borrowing codewords for now.
        private string[] MakeCodewords()
        {
            var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);
            var adjectives = _prototypeManager.Index<DatasetPrototype>("adjectives").Values;
            var verbs = _prototypeManager.Index<DatasetPrototype>("verbs").Values;
            var codewordPool = adjectives.Concat(verbs).ToList();
            var finalCodewordCount = Math.Min(codewordCount, codewordPool.Count);
            var Codewords = new string[finalCodewordCount];
            for (var i = 0; i < finalCodewordCount; i++)
            {
                Codewords[i] = _random.PickAndTake(codewordPool);
            }
            return Codewords;
        }

        public void GreetSocietyMembers(SecretSociety s)
        {
            var chatMgr = IoCManager.Resolve<IChatManager>();
            foreach (var m in s.Members)
            {
                //TODO localize
                chatMgr.DispatchServerMessage(m, "You are a member of the " + s.Name + ". Work towards your objectives!"); //Loc.GetString("stringname")
                chatMgr.DispatchServerMessage(m, "You can identify other members of your secret society with these keywords: " + string.Join(", ", s.Codewords));
            }
        }

        public void PopulateSocieties(RulePlayerJobsAssignedEvent ev)
        {
            //sort out players. TODO
            var percentEnrolled = 1.0f; // .75; //TODO config.
            var pool = ev.Players.OrderBy(p => _random.Next()).Take((int) (ev.Players.Count() * percentEnrolled));

            //Take player pool, split evenly across selected societies. 

            int groupSize = pool.Count() / thisRoundsSocieties.Count();
            int spare = pool.Count() - (groupSize * thisRoundsSocieties.Count());


            //foreach (var s in thisRoundsSocieties)
            for (int s = 0; s < thisRoundsSocieties.Count(); s++)
            {
                var members = pool.Skip(s * groupSize).Take(groupSize).ToList();
                thisRoundsSocieties[s].Members = members;
            }
        }
    }
}
