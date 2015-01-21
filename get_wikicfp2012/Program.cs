using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using get_wikicfp2012.Crawler;
using get_wikicfp2012.Stats;
using get_wikicfp2012.Opi;
using get_wikicfp2012.Export;
using get_wikicfp2012.DBLP;
using get_wikicfp2012.Probability;
using get_wikicfp2012.Score;
using get_wikicfp2012.ProbabilityGroups;

namespace get_wikicfp2012
{
    class Program
    {
        public const string CONNECTION_STRING = @"Data Source=OSTRICH\SQLEXPRESS;Database=dbScience2;Integrated Security=SSPI;Connect Timeout=0";
        public const string CACHE_ROOT = @"D:\work\phd\!\";
        public const int THREAD_COUNT = 5;
        public const bool LOAD_SQL = false;

        static void Main(string[] args)
        {
            new Parser()
                //.ParseConf(@"D:\work\phd\dblp\dblp_bht.xml")
                //.ParseCite(@"D:\work\phd\dblp_arnetminer\acm_output.txt")
                //.UpdateLinks()
                ;

            new OpiCrawler()
                //.Connect()
                //.ScanOPI("opi\\ludzieNauki")
                //.MatchNames()
                //.Close()
                ;
            //CL
            new CareerLine()
                //.CrawlerFirst()
                //.CountLengths()
                //.LimitSets(0)
                //.LimitSets(1)
                //.LimitSets(2)
                ;
            //

            //Tri

            new Triangles()
                //.ScanLinks()
                //.EventToPerson()
                //.CountTypes(-1)
                //.CountTypes(0)
                //.CountTypes(1)
                //.GetStats(-1)
                //.GetStats(0)
                //.GetStats(1)

                //.Betweenness(1,0)
                //.BetweennessStat(1,0)
                //.Betweenness(1,1)
                //.BetweennessStat(1,1)
                //.Betweenness(1,2)
                //.BetweennessStat(1,2)

                //.FakeEventToPerson()
                //.Betweenness(-1, 10)
                //.BetweennessStat(-1, 10)
            ;

            /*
            // download test
            WebInput web = new WebInput();
            string url="http://www.zsi.pwr.wroc.pl/MISSI2010/organization.php2";
            web.GetPage(ref url, WebInputOptions.ForceDownload);
            */
          

            /*
            new PagesCrawler()
                .ParseFile("cfp2\\list.csv", "cfp2\\output.csv", false)
                .Action(PagesCrawlerOptions.Threaded);
            */

            new ExportCSV()
                //.Open()
                //.Store("csv", 0)
                //.CountCommitteeSizes()
                //.Close()
                ;
            
            /*
            new ConditionalProbability()
                .Prepare()
                .Prepare2()
                .CalculateAll()
                ;
             */
            /*
            new ConditionalGroups()
                .Open()
                //.ReadEvents()
                //.IdentifyReasons()                
                .Collect()
                .Close()
                ;
            
             */

            /* OLD ???
            new ScoreObjects()
                .Prepare()
                .CalculateAll()
                ;
            */
            
            new ScorePeople()
                .Prepare()
                //.CalculateTranfers()
                //.CalculateAll()
                //.SaveAll()
                //.BulkImport()
                .CalculateHIndex()
                .SaveHIndex()
                .Close()
                ;            
           
            new ScoreCareerLine()
                //.CrawlerFirst()
                //.LimitSets(5, 20, 5, 0.99)
                //.LimitSets()                
                ;
           
            new ScoreEvents()
                //.Prepare()                
                //.CalculateAll()
                //.SaveAll()                
                //.CalculateAllNow(2012)
                //.SaveAllNow()                
                //.Close()
                ;
              
            Console.WriteLine("END");
            Console.ReadKey();           
        }
    }
}
