using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Conquest
{
    public partial class Test : System.Web.UI.Page
    {
        public dynamic Overview;
        public int Points;
        public int Level;
        public Dictionary<string, int> YouJustGot;

        protected void Page_Load(object sender, EventArgs e)
        {
            var b = Battlefield.Current;
            b.AddMedallion("PageLoader", false, new { LoadedPage = 20 });
            b.AddMedallion("SiteLoader", true, new { LoadedPage = 5 });
            b.DefineLevels(LevelCreator.CreateLinearLevels(1000,10000));
            b.AddManeuver("LoadedPage",1000);

            var c = b.GetPlayer("deldy");

            if(Request.QueryString["reset"] == "true")
            {
                b.Recalculate();
            }
            else
            {
                c.ExecuteManeuver("LoadedPage");
            }


            Overview = c.GetMedallionOverview();
            Points = c.GetPoints();
            Level = c.GetLevel();

            YouJustGot = c.GetNewMedallions();
        }
    }
}