using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VstsMergeTool
{
    class MergeToolEntry
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            if (Enum.TryParse<Authentication>(args[0], out var authenticationWay))
            {
                var initializer = new Initializer(authenticationWay);
                var result = initializer.MergeTool.CreatePullRequest().Result;
            }
            else
            {
                logger.Info("Please choose from PersonalToken or UserNameAndPassword");
            }
        }
    }
}
