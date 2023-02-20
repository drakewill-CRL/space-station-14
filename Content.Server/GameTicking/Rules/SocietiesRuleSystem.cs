using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Destructible;
using Content.Shared.Roles;
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
        [Dependency] private readonly LightBulbSystem _lightBulbSystem = default!;
        //[Dependency] private readonly EntitySystemManager esm = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        public override string Prototype => "Societies"; 

        public int configSocietyCount = 2; //TODO config, must be <= societies.Count

        public List<SecretSociety> allSocieties = new List<SecretSociety>();
        public List<SecretSociety> thisRoundsSocieties = new List<SecretSociety>();

        public int MDKLightsBroken = 0;

        public override void Initialize()
        {
            Logger.InfoS("societies", "Initalizing Societies!");
            base.Initialize();

            //esm.TryGetEntitySystem<LightBulbSystem>(out var lbs);
            

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

            //Nope, this throws an error about a duplicate. Might need to make my own component and add it to everything i want to track?
            //SubscribeLocalEvent<LightBulbComponent, BreakageEventArgs>(OnMDKBreak);

            Logger.InfoS("societies", "Initalized!");
        }

        private void OnStartAttempt(RoundStartAttemptEvent ev)
        {
            Logger.InfoS("societies", "Societies Start Attempt!");
            MDKLightsBroken = 0;
        }

        private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
        {
            Logger.InfoS("societies", "players spawned");
            if (!RuleAdded)
                return;

            PopulateSocieties(ev);

            foreach (var s in thisRoundsSocieties)
            {
                s.CurrentObjectives = s.AllObjectives.OrderBy(o => _random.Next()).Take(2).ToList();

                GreetSocietyMembers(s);
                foreach (var m in s.Members)
                { 
                    var playerMind = m.Data.ContentData()?.Mind;
                    if (playerMind == null)
                        continue;

                    var antagPrototype = _prototypeManager.Index<AntagPrototype>("SocietyMember");
                    var societyRole = new SocietyMemberRole(playerMind, antagPrototype);

                    //societyRole.Greet();

                    //TODO LOC
                    playerMind.Briefing = "The codewords for " + m.Name + " are " + String.Join(",", s.Codewords);
                }
            }

        }

        private void OnRoundEndText(RoundEndTextAppendEvent ev)
        {
            if (!RuleAdded)
                return;
            Logger.InfoS("societies", "Round ended");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Societies:");
            sb.AppendLine("MDK Broke " + MDKLightsBroken + " lights");

            ev.AddLine(sb.ToString());
        }

        public override void Started()
        {
            Logger.InfoS("societies", "Societies Started!");
            foreach (var s in allSocieties) //redo all these every round, in case admins add the others.
                s.Codewords = MakeCodewords();

            //pick societies
            thisRoundsSocieties = allSocieties.OrderBy(s => _random.Next()).Take(configSocietyCount).ToList();
            thisRoundsSocieties = allSocieties.Take(1).ToList(); //TESTING - force players to MDK while figuring out logic.


            //pick objectives.
        }

        public override void Ended()
        {
            Logger.InfoS("societies", "Societies Ended");
        }

        private void OnMDKBreak(EntityUid uid, LightBulbComponent component, BreakageEventArgs args)
        {
            Logger.InfoS("societies", "Light bulb broken, credit for MDK!");
            MDKLightsBroken++;
        }



        //Borrowing codewords for now.
        private string[] MakeCodewords()
        {
            Logger.InfoS("societies", "making Codewords for societies!");
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
            Logger.InfoS("societies", "Greeting society members");
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
            Logger.InfoS("societies", "Populating Societies.");
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

            var remaining = pool.Skip(pool.Count() - spare).Take(spare).ToList();

            while (spare > 0)
            {
                thisRoundsSocieties[spare - 1].Members.Add(remaining[spare - 1]);
                spare--;
            }

        }
    }
}
