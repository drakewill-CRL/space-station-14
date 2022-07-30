using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules
{
    public sealed class SecretSociety
    {
        public string ID = default!;
        public string Name = "";
        public string Description = "";
        public string Abbreviation = "";
        public string[] Codewords = new string[0];
        public List<IPlayerSession> Members = new List<IPlayerSession>();
        public List<SecretSocietyObjective> AllObjectives = new List<SecretSocietyObjective>(); //Probably not inheriting from existing objectives, which need a mind. Unless I make minds share the group objectives?
        public List<SecretSocietyObjective> CurrentObjectives = new List<SecretSocietyObjective>(); //Probably not inheriting from existing objectives, which need a mind. Unless I make minds share the group objectives?
    }

    //TODO: is this an interface/base class the objectives implement, and each of those do all their own code?
    //Maybe i can implement the ObjectivePrototype, and add a couple things to it for this mode?
    public sealed class SecretSocietyObjective
    {
        public string ID = default!;
        public string Name = "";
        public string Description = "";
        public string Progress = ""; //says how far along the team is?
        public string Initialize = ""; // function to set this up for a round?
        //Event listener to react to goes here. 
        //end of round scan function goes here
        //
    }



    //Future work: make it a prototype. Currently errors out with this minimum.
    //public sealed class SecretSocietyPrototype : IPrototype
    //{
    //    [ViewVariables]
    //    [IdDataFieldAttribute]
    //    public string ID { get; } = default!;
    //}
}
