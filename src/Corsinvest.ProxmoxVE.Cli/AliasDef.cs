using System.Linq;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Cli
{
    /// <summary>
    /// Alias command.
    /// </summary>
    public class AliasDef
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="command"></param>
        /// <param name="system"></param>
        public AliasDef(string name, string description, string command, bool system)
        {
            Name = name;
            Description = description;
            Command = command;
            System = system;
        }

        /// <summary>
        /// Name
        /// </summary>
        /// <value></value>
        public string Name { get; }

        /// <summary>
        /// Description
        /// </summary>
        /// <value></value>
        public string Description { get; }

        /// <summary>
        /// Command
        /// </summary>
        /// <value></value>
        public string Command { get; }

        /// <summary>
        /// System
        /// </summary>
        /// <value></value>
        public bool System { get; }

        /// <summary>
        /// Get argument into command start "{" end "}"
        /// </summary>
        /// <returns></returns>
        public string[] GetArguments()
            => new Regex(@"{\s*(.+?)\s*}").Matches(Command)
                                          .OfType<Match>()
                                          .Where(a => a.Success)
                                          .Select(a => a.Groups[1].Value)
                                          .ToArray();

        /// <summary>
        /// Check exists name or alias
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Exists(string name)
        {
            foreach (var name1 in name.Split(','))
            {
                if (Names.Contains(name1)) { return true; }
            }

            return false;
        }

        /// <summary>
        /// Name alias
        /// </summary>
        /// <returns></returns>
        public string[] Names => Name.Split(',');

        /// <summary>
        /// Check name is valid
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsValid(string name) => new Regex("^[a-zA-Z0-9,_-]*$").IsMatch(name);
    }
}