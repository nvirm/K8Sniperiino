using Discore;
using Discore.Http;
using Discore.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace K8Sniperiino
{
    class Program
    {
        //Helpers to keep things in memory, duh.
        public static class ProgHelpers
        {
            public static IConfigurationRoot Configuration { get; set; }
            
            public static int announcedifference = 0; //Time in hours to compare against

            public static string apiclientid = ""; //insert client id
            public static string communityid = ""; //insert community id
            public static string channelid = ""; //insert channel id to shout out in
            public static string bottoken = ""; //insert discore bot token
            public static string language = ""; //language
            public static string version = ""; //txtversion

            public static string txtdeveloper = "Kehittäjä";
            public static string txtpurpose = "Botin tarkoitus";
            public static string txtpurpose2 = "Ilmoittaa alkaneista streameista!";
            public static string txtcommands = "Komennot";
            public static string txtviewers = "Katsojia";
            public static string txtstartedtime = "Streami alkanut";
            public static string txtstreamtitle = "Kuvaus";
            public static string txtgame = "Peli";

            public static DateTime lastfetch = new DateTime(); //Time to compare against when parsing streams (IS THIS NEEDED?)

            //streamerslists
            public static List<string> urlslist = new List<string>(); //fill found streamers here (IS THIS NEEDED?)
            public static List<string> avatarurlslist = new List<string>(); //fill found streamers here (IS THIS NEEDED?)
            public static List<string> titleslist = new List<string>(); //fill found streamers here (IS THIS NEEDED?)
            public static List<string> nameslist = new List<string>(); //fill found streamers here (IS THIS NEEDED?)
            public static List<int> viewerslist = new List<int>(); //fill found streamers here (IS THIS NEEDED?)
            public static List<string> gameslist = new List<string>(); //fill found streamers here (IS THIS NEEDED?)
            public static List<string> streamstarttimes = new List<string>(); //fill found streamers here (IS THIS NEEDED?)
            
            //Who has already been announced
            public static List<DateTime> alreadyannouncedtime = new List<DateTime>(); //<- this is how i track those already announced
            public static List<string> alreadyannouncedurls = new List<string>(); //<- this is how i track those already announced

            //dateslist
            public static List<DateTime> dateslist = new List<DateTime>(); //fill found streamers here (IS THIS NEEDED?)



            //Timer lenght
            public static int _counter = 0; //initial value
            public static int counterlimit = 60; //maxvalue

            //Counter
            public static int streamcount = 0; //initialvalue
        }

        //MAIN
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
            ProgHelpers.Configuration = builder.Build();

            //fetch settings from file
            ProgHelpers.bottoken = ProgHelpers.Configuration["Settings:BotToken"];
            ProgHelpers.apiclientid = ProgHelpers.Configuration["Settings:TwitchClientID"];
            ProgHelpers.communityid = ProgHelpers.Configuration["Settings:TwitchCommunityID"];
            ProgHelpers.counterlimit = Convert.ToInt32(ProgHelpers.Configuration["Settings:CheckintervalSeconds"]);
            ProgHelpers.announcedifference = Convert.ToInt32(ProgHelpers.Configuration["Settings:AnnouncedifferenceHours"]);
            ProgHelpers.language = ProgHelpers.Configuration["Settings:Language"];
            ProgHelpers.version = ProgHelpers.Configuration["Settings:Version"];
            ProgHelpers.channelid = ProgHelpers.Configuration["Settings:ChatChannel"];

            //print out settings to log
            Console.WriteLine("# START SETTINGS-----------------------------");
                Console.WriteLine("! TwitchClientID:" + ProgHelpers.Configuration["Settings:TwitchClientID"]);
                Console.WriteLine("! TwitchCommunityID:" + ProgHelpers.Configuration["Settings:TwitchCommunityID"]);
                Console.WriteLine("! CheckintervalSeconds:" + Convert.ToInt32(ProgHelpers.Configuration["Settings:CheckintervalSeconds"]));
                Console.WriteLine("! AnnouncedifferenceHours:" + Convert.ToInt32(ProgHelpers.Configuration["Settings:AnnouncedifferenceHours"]));
                Console.WriteLine("! Language:" + ProgHelpers.Configuration["Settings:Language"]);
                Console.WriteLine("! Version:" + ProgHelpers.Configuration["Settings:Version"]);
                Console.WriteLine("! ChatChannel:" + ProgHelpers.Configuration["Settings:ChatChannel"]);
            Console.WriteLine("# END SETTINGS-----------------------------");

            //Running english languages
            if (ProgHelpers.language == "en")
            {
                ProgHelpers.txtdeveloper = "Developer";
                ProgHelpers.txtpurpose = "Bot's purpose";
                ProgHelpers.txtpurpose2 = "To announce start of stream!";
                ProgHelpers.txtcommands = "Commands";
                ProgHelpers.txtviewers = "Viewers";
                ProgHelpers.txtstartedtime = "Stream started";
                ProgHelpers.txtstreamtitle = "Description";
                ProgHelpers.txtgame = "Game";
            }

            //end of commands, going live!
            Console.WriteLine("# KITSUN8's STREAMSNIPER BOT STARTED ----- "+DateTime.Now.ToString());

            Program program = new Program();
            program.Run().Wait();
        }

        //-------------------------------------------------------------------------
        public async Task Run()
        {

            // Create authenticator using a bot user token.
            DiscordBotUserToken token = new DiscordBotUserToken(ProgHelpers.bottoken); //token
            // Create a WebSocket application.
            DiscordWebSocketApplication app = new DiscordWebSocketApplication(token);
            // Create and start a single shard.
            Shard shard = app.ShardManager.CreateSingleShard();
            await shard.StartAsync(CancellationToken.None);
            // Subscribe to the message creation event.
            shard.Gateway.OnMessageCreated += Gateway_OnMessageCreated;

            Console.WriteLine("# Timer started");
            Program.StartTimer();

            // Wait for the shard to end before closing the program.
            while (shard.IsRunning)
                await Task.Delay(1000);
        }

        //-------------------------------------------------------------------------
        public async Task RunTwitch()
        {
            //Go query Twitch!
            var twitchurli = "https://api.twitch.tv/kraken/streams?community_id=" + ProgHelpers.communityid;

            //placeholders
            var apiresultURL = "-";
            var apiresultTITLE = "-";
            var apiresultNAME = "-";
            var apiresultVIEWERS = 0;
            var apiresultTIMESTAMP = "-";
            var apiresultLOGO = "-";
            var apiresultGAME = "-";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.twitchtv.v5+json");
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Client-ID", ProgHelpers.apiclientid);

                try
                {
                    var responseText = await client.GetStringAsync(twitchurli);
                    dynamic data = responseText;
                    JObject o = JObject.Parse(data);
                    JArray jarr = (JArray)o["streams"]; //07-06

                    //Foreach loop for all the streamers!
                    foreach (var item in jarr)
                    {
                        if (item.SelectToken("stream_type").ToString() == "live") //first ensure that stream is live
                        {
                            Console.WriteLine("# Adding Stream");
                            if (item.SelectToken("viewers") != null)
                            {
                                apiresultVIEWERS = (int) item.SelectToken("viewers");
                                //Console.WriteLine(apiresultVIEWERS);
                                ProgHelpers.viewerslist.Add((int) apiresultVIEWERS);
                            }
                            if (item.SelectToken("created_at") != null)
                            {
                                DateTime neu = DateTime.ParseExact((string)item.SelectToken("created_at"), "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                                apiresultTIMESTAMP = neu.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

                                //Console.WriteLine(apiresultTIMESTAMP);
                                ProgHelpers.streamstarttimes.Add(apiresultTIMESTAMP);
                            }
                            if (item.SelectToken("channel.url") != null)
                            {
                                apiresultURL = (string) item.SelectToken("channel.url");
                                //Console.WriteLine(apiresultURL);
                                ProgHelpers.urlslist.Add(apiresultURL);
                            }
                            if (item.SelectToken("channel.status") != null)
                            {
                                apiresultTITLE = (string) item.SelectToken("channel.status");
                                //Console.WriteLine(apiresultTITLE);
                                ProgHelpers.titleslist.Add(apiresultTITLE);
                            }
                            if (item.SelectToken("channel.display_name") != null)
                            {
                                apiresultNAME = (string) item.SelectToken("channel.display_name");
                                //Console.WriteLine(apiresultNAME);
                                ProgHelpers.nameslist.Add(apiresultNAME);
                            }
                            if (item.SelectToken("channel.logo") != null)
                            {
                                apiresultLOGO = (string) item.SelectToken("channel.logo");
                                //Console.WriteLine(apiresultLOGO);
                                ProgHelpers.avatarurlslist.Add(apiresultLOGO);
                            }
                            if (item.SelectToken("game") != null)
                            {
                                apiresultGAME = (string) item.SelectToken("game");
                                //Console.WriteLine(apiresultGAME);
                                ProgHelpers.gameslist.Add(apiresultGAME);
                            }
                            ProgHelpers.dateslist.Add(DateTime.Now);
                        } //islive check end
                        
                    } //foreach end
                    
                }//try end
                catch (Exception ex)
                {
                    //Let's see what the error is..
                    Console.WriteLine(ex);
                }

            }//using end
            Console.WriteLine("# TwitchAPI Rows: " + ProgHelpers.dateslist.Count.ToString());
            //helper variable
            ProgHelpers.streamcount = ProgHelpers.dateslist.Count;

            //TODO: Check for timestamps
            if (ProgHelpers.streamcount > 0)
            {
                //check time
                //foreach function

                //check timestamps, if exists in urls --> IF MORE THAN LIMIT, DELETE from alreadyannounced, IF LESS THAN LIMIT DELETE From resultslist -->
                var countalreadyann = ProgHelpers.alreadyannouncedtime.Count; 
                if (countalreadyann > 0)
                {
                    for (var ix = 0; ix<countalreadyann; ix++) 
                    {
                        var hours = (ProgHelpers.alreadyannouncedtime[ix] - DateTime.Now).TotalHours;
                        if (hours<ProgHelpers.announcedifference)
                        {
                            
                            var urlstring = ProgHelpers.alreadyannouncedurls[ix].ToString();
                            Console.WriteLine("# Removing Row from announces");
                            int finder = ProgHelpers.urlslist.IndexOf(urlstring);

                            ProgHelpers.urlslist.RemoveAt(finder);
                            ProgHelpers.titleslist.RemoveAt(finder);
                            ProgHelpers.nameslist.RemoveAt(finder);
                            ProgHelpers.viewerslist.RemoveAt(finder);
                            ProgHelpers.streamstarttimes.RemoveAt(finder);
                            ProgHelpers.avatarurlslist.RemoveAt(finder);
                            ProgHelpers.gameslist.RemoveAt(finder);
                            
                            ProgHelpers.dateslist.RemoveAt(finder);
                        }
                        else
                        {
                            Console.WriteLine("# Allowing announce because of timedifference");
                            ProgHelpers.alreadyannouncedtime.RemoveAt(ix);
                            ProgHelpers.alreadyannouncedurls.RemoveAt(ix);

                        }
                    }   
                }

                //reapplying streamcount in case of removed rows
                ProgHelpers.streamcount = ProgHelpers.dateslist.Count;

                //foreach (string item in ProgHelpers.urlslist)
                for (var iz = 0; iz<ProgHelpers.streamcount; iz++)
                {
                    //Get index of
                    Console.WriteLine("# Announcing now: "+apiresultNAME);
                    apiresultURL = ProgHelpers.urlslist[iz];
                    apiresultTITLE = ProgHelpers.titleslist[iz];
                    apiresultNAME = ProgHelpers.nameslist[iz];
                    apiresultVIEWERS = ProgHelpers.viewerslist[iz];
                    apiresultTIMESTAMP = ProgHelpers.streamstarttimes[iz];
                    apiresultLOGO = ProgHelpers.avatarurlslist[iz];
                    apiresultGAME = ProgHelpers.gameslist[iz];
                  
                    Program rdyprog = new Program();
                    await rdyprog.RunStreamAnnounce(apiresultURL, apiresultTITLE, apiresultNAME, apiresultVIEWERS, apiresultTIMESTAMP, apiresultLOGO, apiresultGAME);

                    //timestamp
                    ProgHelpers.alreadyannouncedtime.Add(DateTime.Now);
                    ProgHelpers.alreadyannouncedurls.Add(apiresultURL);
                    Console.WriteLine("! Announce complete");
                }

                //inside foreach: add to alreadyannouncedtime&urls

                //after foreach functions
                //dispose of the streamerslists
                ProgHelpers.urlslist.Clear();
                ProgHelpers.avatarurlslist.Clear();
                ProgHelpers.titleslist.Clear();
                ProgHelpers.nameslist.Clear();
                ProgHelpers.viewerslist.Clear();
                ProgHelpers.streamstarttimes.Clear();
                ProgHelpers.dateslist.Clear();
                ProgHelpers.gameslist.Clear();
                //dont clear alreadyannounced, it will be checked on next run

                Console.WriteLine("# All lists emptied");

            }
            else
            {
                Console.WriteLine("! No Streams this time");
            }
        }
        //-------------------------------------------------------------------------
        public async Task RunStreamAnnounce(string url,string title,string name,int viewers,string timestamp,string avatarurl,string game)
        {

            DiscordBotUserToken token = new DiscordBotUserToken(ProgHelpers.bottoken); //token
            DiscordWebSocketApplication app = new DiscordWebSocketApplication(token);
            Shard shard = app.ShardManager.CreateSingleShard();
            await shard.StartAsync(CancellationToken.None);

            Snowflake xx = new Snowflake();
            ulong xxid = (ulong)Convert.ToInt64(ProgHelpers.channelid);
            xx.Id = xxid;
            //ITextChannel textChannel = (ITextChannel)shard.Cache.Channels.Get(xx);
            //ITextChannel textChannel = (ITextChannel)shard.Application.HttpApi.Channels.Get(xx);
            
            try
            {
                //Announcing stream
                DiscordMessage annch = await app.HttpApi.Channels.CreateMessage(xx, new DiscordMessageDetails()
                .SetEmbed(new DiscordEmbedBuilder()
                .SetTitle(name)
                .SetFooter("kitsun8's Sniperiino, " + ProgHelpers.version)
                .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                .SetUrl(url)
                .SetThumbnail(avatarurl)
                .AddField(ProgHelpers.txtstreamtitle, title, false)
                .AddField(":busts_in_silhouette: " + ProgHelpers.txtviewers + " ", viewers.ToString(), true)
                .AddField(":game_die: "+ ProgHelpers.txtgame + " ", game, true)
                .AddField(":clock10: " + ProgHelpers.txtstartedtime + " ", timestamp, false)
                ));
            }
            catch(Exception ex)
            {

                Console.WriteLine(ex);
            }

            await shard.StopAsync();
            //returning
            return;


        }

        //------------------------------------------------------------------------
        public static Timer _tm = null;
        public static AutoResetEvent _autoEvent = null;

        public static void StartTimer()
        {
            //timer init

            _autoEvent = new AutoResetEvent(false);
            _tm = new Timer(Timergo, _autoEvent, 1000, 1000);

            Console.WriteLine("# Waiting for timer to complete");
        }


        //
        public async static void Timergo(Object stateInfo)
        {
            if (ProgHelpers._counter < ProgHelpers.counterlimit)
            {
                ProgHelpers._counter++;
                return;
            }
            ProgHelpers._counter = 0;
            Console.WriteLine("# Disposing Timer");
            _tm.Dispose();

            Console.WriteLine("! Timer is Up");
            Console.WriteLine("# TwitchAPI Start");
            Program rdytwitch = new Program();
            await rdytwitch.RunTwitch();
            //Timer is up, another round of streamcheck!
            //1 empty the streamers list
            //2 Fill up streamers list, 
            //3 Compare timestamp (started time) to Datetime now, difference in hours > Parameter -> Valid for posting!
            
            //Starting another timer!
            Program.StartTimer();
            Console.WriteLine("# Timer started  " + DateTime.Now.ToString());

        }
        //------------------------------------------------------------------------Gateway messages parsing
        private static async void Gateway_OnMessageCreated(object sender, MessageEventArgs e)
        {
            Shard shard = e.Shard;
            DiscordMessage message = e.Message;

            if (message.Author == shard.User)
                // Ignore messages created by our bot.
                return;

            if (message.ChannelId.Id.ToString() == ProgHelpers.channelid)//Prevent DM abuse, only react to messages sent on a set channel.
            { 
                //-----------------------------------------------------------------------------------------INFO - VALMIS V1
                if (message.Content == "!sniperiinoinfo" || message.Content == "!si")
                {
                    ITextChannel textChannel = (ITextChannel)shard.Cache.Channels.Get(message.ChannelId);

                    try
                    {

                        // Reply to the user who posted "!info".
                        await textChannel.CreateMessage(new DiscordMessageDetails()
                         .SetEmbed(new DiscordEmbedBuilder()
                         .SetTitle($"kitsun8's Sniperiino")
                         .SetFooter(ProgHelpers.version)
                         .SetColor(DiscordColor.FromHexadecimal(0xff9933))
                         .AddField(ProgHelpers.txtdeveloper + " ", "kitsun8#4567", false)
                         .AddField(ProgHelpers.txtpurpose + " ", ProgHelpers.txtpurpose2, false)
                         .AddField(ProgHelpers.txtcommands + " ", "!sniperiinoinfo/si", false)
                        ));

                        Console.WriteLine($"!sniperiinoinfo - " + message.Author.Username + "-" + message.Author.Id + " --- " + DateTime.Now);
                    }
                    catch (Exception) { Console.WriteLine($"!sniperiinoinfo - EX -" + message.Author.Username + "-" + message.Author.Id + " --- " + DateTime.Now); }
                }

            }
        }

    }
}