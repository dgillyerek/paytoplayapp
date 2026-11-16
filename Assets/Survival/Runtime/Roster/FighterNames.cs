namespace Survival.Domain.Roster
{
    /// <summary>
    /// Roster names of every locked-battle fighter. Code that branches on a fighter uses these, never a string literal,
    /// so a typo is a compile error instead of a silent miss.
    /// </summary>
    public static class FighterNames
    {
        public const string SirAldric = "Sir Aldric";
        public const string Lyra = "Lyra";
        public const string Rowan = "Rowan";
        public const string Oakenshield = "Oakenshield";
        public const string Stormcrest = "Stormcrest";
        public const string Emberfang = "Emberfang";
        public const string Vespera = "Vespera";
        public const string Nightfang = "Nightfang";
        public const string Ironhowl = "Ironhowl";
        public const string Ashwyrm = "Ashwyrm";
        public const string Blightroot = "Blightroot";
        public const string Bonequill = "Bonequill";
        public const string Vorgath = "Vorgath";
    }
}
