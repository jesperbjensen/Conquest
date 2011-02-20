/*
 *  CONQUEST
 *  A mini utility to track user achiements on your site
 *  https://github.com/deldy/conquest
 * 
 *  DEPENDENCIES: Massive (Rob Conery) https://github.com/robconery/massive
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Massive;

namespace Conquest
{
    /// <summary>
    /// The base class for defining was awards you can give to your users.
    /// Setup the Maneuvers to track, and the Madallions in your global.asax
    /// </summary>
    public class Battlefield
    {
        private readonly string _connectionStringName;
        private readonly Dictionary<string, dynamic> _maneuvers = new Dictionary<string, dynamic>();
        private readonly Dictionary<string, dynamic> _medallions = new Dictionary<string, dynamic>();
        private readonly Maneuvers _maneuversDb;
        private readonly Medallions _medallionsDb = new Medallions();
        private int[] _levels;

        public Battlefield(string connectionStringNameName)
        {
            _connectionStringName = connectionStringNameName;
            _maneuversDb = new Maneuvers(_connectionStringName);
            _medallionsDb = new Medallions(_connectionStringName);
        }


        // Static support
        private static Battlefield _currentBattleField;
        public static Battlefield Current
        {
            get { return _currentBattleField ?? (_currentBattleField = new Battlefield(null)); }
            set { _currentBattleField = value; }
        }

        /// <summary>
        /// Gets a player with a specific user name. 
        /// It is adviced to use your primary key for your user, as there is no support for renaming a player
        /// </summary>
        public Player GetPlayer(string name)
        {
            return new Player(name, this);
        }

        /// <summary>
        /// Use this to add a maneuver that a user can do.
        /// This is a action, that gives some points (EXP), and is used to calculate Medallions
        /// </summary>
        /// <param name="key">A unique key for the maneuver</param>
        /// <param name="points">The points that this maneuver should give</param>
        public void AddManeuver(string key, int points)
        {
            _maneuvers[key] = new {Points = points};
        }

        /// <summary>
        /// Use this to add a medallion, that a user can be awarded.
        /// This is like a badge, achiement or trophy. There is no points (EXP) for a badge.
        /// </summary>
        /// <param name="key">A unique key for the medallion</param>
        /// <param name="allowMultiple">Can the user be awarded this multiple times?</param>
        /// <param name="conditions">A object containing the maneuvers that a medallion gives (new { VisitedSite = 5; })</param>
        public void AddMedallion(string key, bool allowMultiple, object conditions)
        {
            if (conditions == null || conditions.ToDictionary().Count == 0)
                throw new Exception("Must have atleast one condition");

            _medallions[key] = new { AllowMultiple = allowMultiple, Conditions = conditions.ToDictionary() };
        }
        
        /// <summary>
        /// Recalculates the medallions. If a player is given, it will only recalculate that player
        /// </summary>
        /// <param name="player">The player to recalculate for, if none is supplied, it will recalculate all medallions for all users!</param>
        public void Recalculate(string player = null)
        {
            if(player == null)
                _medallionsDb.Delete();
            else
                _medallionsDb.Delete(null,"Player=@0", player);

            AwardCeremony(player);
        }

        /// <summary>
        /// Use this method to define levels. Levels is segments of points.
        /// There is always a level 1. The first element in the array, will be the points required to reach level 2, and so one.
        /// </summary>
        public void DefineLevels(int[] levels)
        {
            _levels = levels;
        }

        // Private
        private void AwardCeremony(string player = null, string key = null, bool shouldNotifyUser = true)
        {
            IEnumerable<string> players;

            if (!string.IsNullOrEmpty(player))
                players = new List<string>(new[] { player });
            else
                players = _maneuversDb.GetAllPlayers();

            foreach(var p in players)
            {
                // we need to find all the Medallions that have this Maneuver as a condition
                var medallions = string.IsNullOrEmpty(key) ? _medallions : _medallions.Where(x => ((IDictionary<string, dynamic>)x.Value.Conditions).ContainsKey(key)).ToDictionary(x=>x.Key,x=>x.Value);
                // we get all the manuevers on the player
                var maneuvers = _maneuversDb.ByPlayer(p).ToDictionary(x => x.TypeKey, x => x.Value);
                var awardedMedallions = _medallionsDb.ByPlayer(p);
                foreach (var m in medallions)
                {
                    var newMedals = HowManyMedalsToBeAwarded(m, awardedMedallions, maneuvers);
                    if (newMedals > 0)
                    {
                        _medallionsDb.Insert(new { TypeKey = m.Key, Player = player, Amount = newMedals, CreatedAt = DateTime.Now, UserNotified = !shouldNotifyUser });
                    }
                }
            }

        }

        private static int HowManyMedalsToBeAwarded(KeyValuePair<string, dynamic> medallion, IEnumerable<dynamic> awardedMedallions, Dictionary<dynamic, dynamic> maneuvers)
        {
            // We dont allow more of them
            if (medallion.Value.AllowMultiple == false && awardedMedallions.Any(x => x.TypeKey == medallion.Key))
                return 0;

            var alreadyEearned = awardedMedallions.Where(x => x.TypeKey == medallion.Key);

            // The condition for earning a new one, will be the conditions times the ones you already earned
            var medalsToBeAwarded = int.MaxValue;

            foreach(var condition in medallion.Value.Conditions)
            {
                // We will now check if we are meeting all requirements
                int requirement = condition.Value;

                medalsToBeAwarded = Math.Min(medalsToBeAwarded,maneuvers[condition.Key]/requirement);
            }

            int maxMedals = int.MaxValue;
            if(medallion.Value.AllowMultiple == false)
            {
                maxMedals = 1;
            }

            return Math.Min(maxMedals,medalsToBeAwarded-alreadyEearned.Sum(x=>x.Amount));
        }

        private IEnumerable<dynamic> GetMedallionOverview(string player)
        {
            return _medallionsDb.Overview(player);
        }

        private int GetPlayerPoints(string player)
        {
            return _maneuversDb.GetTotalPointsOnPlayer(player);
        }

        private int GetLevel(string player)
        {
            if (_levels == null)
                throw new Exception("No levels defined");

            var level = 1;
            var points = GetPlayerPoints(player);
            for (int index = 0; index < _levels.Length; index++)
            {
                var l = _levels[index];
                if (l <= points)
                {
                    level = index+2;
                }
                else
                {
                    break;
                }
            }
            return level;
        }

        private Dictionary<string,int> GetNewMedallionsOnPlayer(string player)
        {
            return _medallionsDb.NewMedallionsOnPlayer(player);
        }

        private void MarkPlayerMedallionsAsUserNotified(string name)
        {
            _medallionsDb.MarkPlayerMedallionsAsUserNotified(name);
        }
        // Inner classes
        public class Player
        {
            private readonly string _name;
            private readonly Battlefield _battlefield;

            public Player(string name, Battlefield battlefield)
            {
                _name = name;
                _battlefield = battlefield;
            }

            /// <summary>
            /// Use this is a user have just done something that you whould like to track
            /// </summary>
            /// <param name="key">The key of the maneuver to set</param>
            /// <param name="value">A optional number of how much the total should be incremented (defaults to 1)</param>
            /// <param name="date">A optional date, for when this happened</param>
            public void ExecuteManeuver(string key, int value = 1, DateTime? date = null)
            {
                if (!_battlefield._maneuvers.ContainsKey(key))
                    throw new Exception(string.Format("Unkown maneuver ('{0}')", key));

                date = date ?? DateTime.Now;

                _battlefield._maneuversDb.Insert(new { TypeKey = key, Player = _name, Value = value, CreatedAt = date, Points = _battlefield._maneuvers[key].Points * value });

                _battlefield.AwardCeremony(_name, key);
            }

            /// <summary>
            /// Recalculate the users medallions
            /// </summary>
            public void Recalculate()
            {
                _battlefield.Recalculate(_name);
            }

            /// <summary>
            /// Get the users total points
            /// </summary>
            public int GetPoints()
            {
                return _battlefield.GetPlayerPoints(_name);
            }

            /// <summary>
            /// Gets the users level
            /// </summary>
            public int GetLevel()
            {
                return _battlefield.GetLevel(_name);
            }

            /// <summary>
            /// Gets a overview of the medallions, and how many the player got of them.
            /// </summary>
            /// <returns></returns>
            public dynamic GetMedallionOverview()
            {
                return _battlefield.GetMedallionOverview(_name);
            }

            /// <summary>
            /// Gets the medallions the user was awared since last time you called this method.
            /// Notice that this method will mark the medallions as UserNofitied, so they will not be retunered next time you call the method!
            /// User this method to make a achievement popup, for some UI stuff, telling the user that they just gone a medallion
            /// </summary>
            /// <returns></returns>
            public Dictionary<string,int> GetNewMedallions()
            {
                var newMedallions = _battlefield.GetNewMedallionsOnPlayer(_name);

                _battlefield.MarkPlayerMedallionsAsUserNotified(_name);

                return newMedallions;
            }
        }
    }

    /// <summary>
    /// A little helper tool, for creating lots of level indexes
    /// </summary>
    public class LevelCreator
    {
        public static int[] CreateLinearLevels(int maxLevels, int incrementor)
        {
            var list = new List<int>();

            for(int x=1;x<maxLevels;x++)
            {
                list.Add(x*incrementor);
            }

            return list.ToArray();
        }
    }
 
    class Maneuvers : DynamicModel
    {
        public Maneuvers(string connectionStringName) : base(connectionStringName) { }

        public IList<dynamic> ByPlayer(string player)
        {
            return Query("SELECT SUM(Value) as Value, TypeKey FROM Maneuvers WHERE Player=@0 GROUP BY TypeKey", player);
        }

        public int GetTotalPointsOnPlayer(string player)
        {
            var result = Query("SELECT SUM(Points) as Points FROM Maneuvers WHERE Player=@0", player).FirstOrDefault();

            if (result == null)
                return 0;
            return result.Points;
        }

        public IEnumerable<string> GetAllPlayers()
        {
            return Query("SELECT DISTINCT Player FROM Maneuvers").Select(x => x.Player).Cast<string>();
        }
    }

    class Medallions : DynamicModel 
    {
        public Medallions(string connectionStringName) : base(connectionStringName) {}

        public IList<dynamic> ByPlayer(string player)
        {
            return Query("SELECT * FROM Medallions WHERE Player=@0", player);
        }

        public IList<dynamic> Overview(string player)
        {
            return Query("SELECT TypeKey, Sum(Amount) as Amount FROM Medallions WHERE Player=@0 GROUP BY TypeKey", player);
        }

        public Dictionary<string, int> NewMedallionsOnPlayer(string player)
        {
            var result = Query("SELECT TypeKey, Sum(Amount) as Amount FROM Medallions WHERE Player=@0 And UserNotified=0 GROUP BY TypeKey", player);
            return result.ToDictionary(x => (string)x.TypeKey, x => (int)x.Amount);
        }

        public void MarkPlayerMedallionsAsUserNotified(string player)
        {
            Query("UPDATE Medallions SET UserNotified=1 WHERE Player=@0", player);
        }
    }
}