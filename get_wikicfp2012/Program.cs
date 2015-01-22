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
        // connection string to MSSQL database
        // script in database folder 
        public const string CONNECTION_STRING = @"Data Source=OSTRICH\SQLEXPRESS;Database=dbScience2;Integrated Security=SSPI;Connect Timeout=0";

        //root for local web-cache, temporary files and result files
        public const string CACHE_ROOT = @"D:\work\phd\!\";

        // thread count for page crawler
        public const int THREAD_COUNT = 5;

        // preload SQL wile crawling
        public const bool LOAD_SQL = false;

        static void Main(string[] args)
        {
            // steps can be switched off by sommenting out function lines (starting with dot '.')
            // constructor lines must be left in place (staring with new)

            new Parser()
                // parse DBLP
                //.ParseConf(@"D:\work\phd\dblp\dblp_bht.xml")
                // parse ArNetMiner citation
                //.ParseCite(@"D:\work\phd\dblp_arnetminer\acm_output.txt")
                // update conference links
                //.UpdateLinks()
                ;

            new CFPCrawler().CrawlList("http://www.wikicfp.com","/cfp/allcat");
            new CFPFilePaser().ScanNames(Program.CACHE_ROOT + "rfp");            


            new PagesCrawler()
                // parse found URLs files
                .ParseFile("cfp2\\list.csv", "cfp2\\output.csv", false)
                // clear list of visited pages                
                // scan past events link
                .Action(PagesCrawlerOptions.PastEvents)
                // scan conference pages
                .Action(PagesCrawlerOptions.SingleThreaded)
                ;

            new OpiCrawler()
                // Init DB connetion
                //.Connect()
                // Scan OPI to select Polish subset
                //.ScanOPI("opi\\ludzieNauki")
                // Match names to existing
                //.MatchNames()
                // Close DB connection
                //.Close()
                ;

            //Career line - obsolete
            /*
            new CareerLine()
                .CrawlerFirst()
                .CountLengths()
                .LimitSets(0)
                .LimitSets(1)
                .LimitSets(2)
                ;
            */

            //Triads
            new Triangles()
                // read data
                //.ScanLinks()
                // links between people
                //.EventToPerson()
                //count triads in (-1 - all, 0 - all but Poland, 1 - Poland)
                //.CountTypes(-1)
                //.CountTypes(0)
                //.CountTypes(1)
                // get triad creation stats
                //.GetStats(-1)
                //.GetStats(0)
                //.GetStats(1)

                /* OBSOLETE
                //.Betweenness(1,0)
                //.BetweennessStat(1,0)
                //.Betweenness(1,1)
                //.BetweennessStat(1,1)
                //.Betweenness(1,2)
                //.BetweennessStat(1,2)

                //.FakeEventToPerson()
                //.Betweenness(-1, 10)
                //.BetweennessStat(-1, 10)
                */
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

            // export dabatase data to csv
            new ExportCSV()
                // init
                //.Open()
                // store data sample with cunt limit (0 - no limit)
                //.Store(Program.CACHE_ROOT+"csv", 0)
                // count committee size stats
                //.CountCommitteeSizes()
                // close
                //.Close()
                ;

            /*
            new ConditionalProbability()
                // prepare phase one
                .Prepare()
                // prepare phase two
                .Prepare2()
                // calculate 
                .CalculateAll()
                ;
             */


            new ConditionalGroups()
                // init
                .Open()
                // read events from databae to file
                //.ReadEvents()
                // identify event reasons
                //.IdentifyReasons()                
                // collect reason stats and save
                .Collect()
                // close
                .Close()
                ;

            /*
            // deprecated object score
            new ScoreObjects()
                .Prepare()
                .CalculateAll()
                ;
            */

            // calculate scores for people
            new ScorePeople()
                // init
                .Prepare()
                // correlation test friendship transition probability based on year and number of connections
                //.CalculateTranfers()
                // calculate scores
                //.CalculateAll()
                // save scores to file 
                //.SaveAll()
                // import scores from file
                //.BulkImport()
                // TBC - calculate h index
                .CalculateHIndex()
                // TBC - save h index
                .SaveHIndex()
                // close
                .Close()
                ;            
           
            // calculate career lines (scores based on years since career start)
            new ScoreCareerLine()
                // get scores in years for people
                .CrawlerFirst()
                // groupping sets (min career length, max length considered, top classes to return, minimal coverage of top classes)
                //.LimitSets(5, 20, 5, 0.99)
                // groupping with default values
                .LimitSets()                
                ;

            // calculate sccore for events
            new ScoreEvents()
                // init
                //.Prepare()                
                // calculate scores
                //.CalculateAll()
                // save scores
                //.SaveAll()  
                // calculate for year 2012
                //.CalculateAllNow(2012)
                // save results for selected year
                //.SaveAllNow()                
                // close
                //.Close()
                ;
              
            Console.WriteLine("END");
            Console.ReadKey();           
        }
    }
}
