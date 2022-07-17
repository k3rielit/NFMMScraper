using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;

namespace NFMMScraper {
    public class ScrapeProgress {                           // Account, Car, Stage
        public HashSet<string> AccCompleted = new();
        public HashSet<string>[] AccQueue = new HashSet<string>[2] { new HashSet<string>(),new HashSet<string>() };
        public HashSet<string> CarCompleted = new();
        public HashSet<string> CarRemoved = new();
        public HashSet<string>[] CarQueue = new HashSet<string>[2] { new HashSet<string>(),new HashSet<string>() };
        public HashSet<string> StgCompleted = new();
        public HashSet<string> StgRemoved = new();
        public HashSet<string>[] StgQueue = new HashSet<string>[2] { new HashSet<string>(),new HashSet<string>() };
        public string DebugString() {
            return $"AccCompleted[{AccCompleted.Count}]  AccQueue[{AccQueue[0].Count}][{AccQueue[1].Count}]\n" +
                   $"CarCompleted[{CarCompleted.Count}]  CarQueue[{CarQueue[0].Count}][{CarQueue[1].Count}]  CarRemoved[{CarRemoved.Count}]\n" +
                   $"StgCompleted[{StgCompleted.Count}]  StgQueue[{StgQueue[0].Count}][{StgQueue[1].Count}]  StgRemoved[{StgRemoved.Count}]";
        }
        public void Rescan() {
            AccQueue[0].UnionWith(AccCompleted);
            AccCompleted.Clear();
            CarQueue[0].UnionWith(CarCompleted);
            CarCompleted.Clear();
            StgQueue[0].UnionWith(StgCompleted);
            StgCompleted.Clear();
        }
    }
    internal class Program {
        // [UI string, action, param]
        static readonly List<string[]> menuItems = new() {
            new[]{"Help","help",""},
            new[]{"Scrape","scrape",""},
            new[]{"Rescan","rescan",""},
            new[]{"Export To Json","exportjson",""},
            new[]{"View Data","viewdata","*"},
            new[]{"Search Data","viewdata",""},
            new[]{"Clear Data","cleardata",""},
        };
        static ScrapeProgress sp = new();
        static void Main(string[] args) {
            // Startup
            if(File.Exists("progress.json")) {
                try {
                    ScrapeProgress? prevSp = JsonConvert.DeserializeObject<ScrapeProgress>(File.ReadAllText("progress.json"));
                    if(prevSp != null) {
                        sp = prevSp;
                    }
                }
                catch(Exception ex) {
                    Console.WriteLine("Error loading previous scraped data. Press any key to continue... \n\n"+ex.Message);
                    Console.ReadKey();
                };
            }
            // Program
            byte selected = 0;
            ConsoleKey ck;
            do {
                Console.Clear();
                Console.WriteLine($"{sp.DebugString()}\n\n{string.Join("\n",menuItems.Select((s,i) => (i==selected ? " > " : "   ") + s[0]))}\n\n[Esc] Exit  [E/Enter] Select  [W/Up] Up  [S/Down] Down\n\n");
                ck = Console.ReadKey().Key;
                if(ck == ConsoleKey.Enter || ck == ConsoleKey.E) ActionHandler(menuItems[selected][1],menuItems[selected][2]);
                else if(ck == ConsoleKey.W || ck == ConsoleKey.UpArrow) selected = (byte)(selected > 0 ? selected-1 : selected);
                else if(ck == ConsoleKey.S || ck == ConsoleKey.DownArrow) selected = (byte)(selected < byte.MaxValue && selected < menuItems.Count-1 ? selected+1 : selected);
            }
            while(ck != ConsoleKey.Escape) ;
        }

        static void ActionHandler(string action, string? param) {
            Console.Clear();
            switch(action) {

                case "help":
                    Console.WriteLine("[Unimplemented]\nHelp\n\nSketchUp > All");
                    Console.ReadKey();
                    break;

                case "exportjson":
                    Console.Write("Export data to JSON file. Default is 'exported'.\n\nEnter a file name/path: ");
                    SaveJson(sp, Console.ReadLine() ?? "exported");
                    break;

                case "viewdata":
                    if(param == string.Empty) {
                        Console.Write("Search the data with a simple expression. Not case sensitive. Default is  *  (anything).\nExamples:  most*  *t*  kekw\nEnter a search filter: ");
                        param = Console.ReadLine() ?? "*";
                        Console.Clear();
                    }
                    // ...
                    Console.WriteLine("[Unimplemented]");
                    Console.ReadKey();
                    break;

                case "cleardata":
                    Console.Write("This will clear the scraped data from memory, and empty the default 'progress.json' file.\nAre you sure? (Y/N  Default:N) ");
                    if(Console.ReadKey().Key == ConsoleKey.Y) {
                        sp = new ScrapeProgress();
                        SaveJson(sp);
                    }
                    break;

                case "scrape":
                    // Confirm
                    Console.Write("This will start scraping from NFMM as much as possible, which is a long process.\nIt will autosave to 'progress.json' frequently, so don't open that file in any other program, and keep an eye out for error messages here.\n\nAre you sure to continue? (Y/N  Default:N) ");
                    if(Console.ReadKey().Key != ConsoleKey.Y) {
                        break;
                    }
                    if(sp.AccQueue[0].Count==0 && sp.AccQueue[1].Count==0 && sp.CarQueue[0].Count==0 && sp.CarQueue[1].Count==0 && sp.StgQueue[0].Count==0 && sp.StgQueue[1].Count==0) {
                        Console.Write("\n\nStart scraping from an account. Not case sensitive. Default is  toxicgamer  (xd).\nEnter a name: ");
                        string name = (Console.ReadLine() ?? "toxicgamer").Trim().ToLower();
                        if(!sp.AccCompleted.Contains(name)) {
                            sp.AccQueue[0].Add(name); // if.. if.. if.. if.. if..........
                        }
                    }
                    Console.Clear();
                    Scrape(sp);
                    Console.ReadLine();
                    break;

                case "rescan":
                    // Confirm
                    Console.Write("This will move completed items to the queue and start scraping, which is a long process.\nIt will autosave to 'progress.json' frequently, so don't open that file in any other program, and keep an eye out for error messages here.\n\nAre you sure to continue? (Y/N  Default:N) ");
                    if(Console.ReadKey().Key != ConsoleKey.Y) {
                        break;
                    }
                    Console.Clear();
                    Scrape(sp);
                    Console.ReadLine();
                    break;
            }
        }

        static async void Scrape(ScrapeProgress sp) {
            byte accInd = 0;
            byte carInd = 0;
            byte stgInd = 0;
            HashSet<string> accTemp= new();
            HashSet<string> carTemp = new();
            HashSet<string> stgTemp = new();
            while(sp.AccQueue[0].Count>0 || sp.AccQueue[1].Count>0 || sp.CarQueue[0].Count>0 || sp.CarQueue[1].Count>0 || sp.StgQueue[0].Count>0 || sp.StgQueue[1].Count>0) {
                accInd = sp.AccQueue[0].Count > sp.AccQueue[1].Count ? (byte)0 : (byte)1;
                carInd = sp.CarQueue[0].Count > sp.CarQueue[1].Count ? (byte)0 : (byte)1;
                stgInd = sp.StgQueue[0].Count > sp.StgQueue[1].Count ? (byte)0 : (byte)1;
                // Go through accounts
                foreach(string acc in sp.AccQueue[accInd]) {
                    // friend list
                    try {
                        string friendlist = await GetTextHTTP($"http://multiplayer.needformadness.com/profiles/{acc}/friends.txt",Encoding.Latin1);
                        if(friendlist.Contains('|')) {
                            accTemp.UnionWith(friendlist.Split("\r\n")[0].Split('|').Select(s => s.ToLower()).ToHashSet());
                        }
                    }
                    catch(Exception) {
                        Console.WriteLine($"Error[FriendListRequest] Account[{acc}]");
                    }
                    // cars
                    try {
                        string carlist = await GetTextHTTP($"http://multiplayer.needformadness.com/cars/lists/{acc}.txt",Encoding.Latin1);
                        if(carlist.Contains("mycars(")) {
                            carTemp.UnionWith(carlist.Split('(',')')[1].Split(',').Select(s => s.Replace(' ','_')).ToHashSet());
                        }
                    }
                    catch(Exception) {
                        Console.WriteLine($"Error[CarListRequest] Account[{acc}]");
                    }
                    // stages
                    try {
                        string stagelist = await GetTextHTTP($"http://multiplayer.needformadness.com/tracks/lists/{acc}.txt",Encoding.Latin1);
                        if(stagelist.Contains("mystages(")) {
                            stgTemp.UnionWith(stagelist.Split('(',')')[1].Split(',').Select(s => s.Replace(' ','_')).ToHashSet());
                        }
                    }
                    catch(Exception) {
                        Console.WriteLine($"Error[StageListRequest] Account[{acc}]");
                    }
                }
                Console.WriteLine(sp.DebugString());
                sp.AccCompleted.UnionWith(sp.AccQueue[accInd]);
                sp.AccQueue[accInd].Clear(); // use queue[0], push new items to [1], clear [0] -> use [1], push to [0], clear [1], ...
                SaveJson(sp);
                // Go through cars
                foreach(string car in sp.CarQueue[carInd]) {
                    try {
                        // added by list
                        string details = await GetTextHTTP($"http://multiplayer.needformadness.com/cars/{car.Replace(' ','_')}.txt",Encoding.Latin1);
                        if(details.Contains("details(")) {
                            List<string> detailsSplit = details.Split('(',')')[1].Split(',').ToList();
                            detailsSplit.RemoveRange(1,2);
                            accTemp.UnionWith(detailsSplit.Select(ds => ds.Replace(' ','_')).Where(w => w.Length > 0 && !sp.AccCompleted.Contains(w)).ToHashSet());
                        }
                    }
                    catch(Exception) {
                        Console.WriteLine($"Error[CarDetailsRequest] Car[{car}]");
                        sp.CarRemoved.Add(car);
                    }
                }
                Console.WriteLine(sp.DebugString());
                sp.CarCompleted.UnionWith(sp.CarQueue[carInd].Where(w => !sp.CarRemoved.Contains(w)));
                sp.CarQueue[carInd].Clear();
                SaveJson(sp);
                // Go through stages
                foreach(string stg in sp.StgQueue[stgInd]) {
                    try {
                        // added by list
                        string details = await GetTextHTTP($"http://multiplayer.needformadness.com/tracks/{stg.Replace(' ','_')}.txt",Encoding.Latin1);
                        if(details.Contains("details(")) {
                            List<string> detailsSplit = details.Split('(',')')[1].Split(',').ToList();
                            detailsSplit.RemoveAt(1);
                            accTemp.UnionWith(detailsSplit.Select(ds => ds.Replace(' ','_')).Where(w => w.Length > 0 && !sp.AccCompleted.Contains(w)).ToHashSet());
                        }
                    }
                    catch(Exception) {
                        Console.WriteLine($"Error[StageDetailsRequest] Stage[{stg}]");
                        sp.StgRemoved.Add(stg);
                    }
                }
                Console.WriteLine(sp.DebugString());
                sp.StgCompleted.UnionWith(sp.StgQueue[stgInd].Where(w => !sp.StgRemoved.Contains(w)));
                sp.StgQueue[stgInd].Clear();
                SaveJson(sp);
                // Move temp to next queue
                sp.AccQueue[Math.Abs(accInd-1)].UnionWith(accTemp.Where(w => !sp.AccCompleted.Contains(w)));
                sp.CarQueue[Math.Abs(carInd-1)].UnionWith(carTemp.Where(w => !sp.CarCompleted.Contains(w)));
                sp.StgQueue[Math.Abs(stgInd-1)].UnionWith(stgTemp.Where(w => !sp.StgCompleted.Contains(w)));
                accTemp.Clear();
                carTemp.Clear();
                stgTemp.Clear();
                Console.WriteLine("Loop complete...");
                SaveJson(sp);
            }
        }


        static void SaveJson(object? jsonData, string path = "progress") {
            try {
                File.WriteAllText($"{path}.json",JsonConvert.SerializeObject(sp,Formatting.Indented));
            }
            catch(Exception ex) {
                Console.Clear();
                if(ex is JsonSerializationException) Console.WriteLine($"Error during JSON serialization.\n\n{ex.Message}");
                if(ex is UnauthorizedAccessException) Console.WriteLine($"Authorization error during file creation.\n{path}.json\nRun the program with administrator privileges, or move it to a non-restricted directory.\n\n{ex.Message}");
                if(ex is IOException) Console.WriteLine($"Error while writing to {path}.json.\n\n{ex.Message}");
                else Console.WriteLine($"Unknown error while saving the data to {path}.json.\n\n{ex.Message}");
            }
        }

        public async static Task<string> GetTextHTTP(string url, Encoding encoding, bool rethrowError = true) {
            string result = "";
            try {
                HttpClient client = new();
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
                result = encoding is null ? Encoding.Latin1.GetString(responseBody) : encoding.GetString(responseBody);
            }
            catch(Exception ex) {
                if(rethrowError) throw new HttpRequestException();
                else if(ex is HttpRequestException) Console.WriteLine($"Error during the HTTP requests. Maybe the resource doesn't exist, or there's no internet connection, or Omar's server is down.\n\n{ex.Message}");
                else Console.WriteLine($"Unknown error during the HTTP requests. Please check your internet connection, or Omar's server.\n\n{ex.Message}");
            }
            return result;
        }
        public static List<string> ExtractList(string str) {
            List<string> list = new();
            list = str.Split(',').ToList();
            return list;
        }
    }
}