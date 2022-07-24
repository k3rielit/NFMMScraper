using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;

namespace NFMMScraper {
    public class Program {
        public static Config RConfig = new();
        private static bool ShouldContinue = true;
        private static ConcurrentDictionary<int,string[]> WorkingThreads = new(); // [Task.CurrentId]=[NFMMType,ItemName]
        static void Main(string[] args) {
            // Load config and progress

            if(RConfig.RescanCompletedItemsOnStartup) {
                // ...
            }
            // Add initial item
            NFMM.AddItem(new NFMMItem {
                Type = NFMMType.Account,
                Name = RConfig.InitialAccount
            });
            // Start threads
            for(ulong i = 0; i < RConfig.SoftwareThreadCount; i++) {
                Task.Run(() => SThread());
            }
            Task.Run(() => UpdateUI());
            Task.Run(() => SaveData());
            // Keep threads alive
            ConsoleKey ck;
            do {
                ck = Console.ReadKey().Key;
            }
            while(ck!=ConsoleKey.Escape);
        }

        private static async void UpdateUI() {
            Console.WriteLine("To terminate the program at any time, press [Esc].");
            while(Program.ShouldContinue) {
                Console.SetCursorPosition(0,1);
                Console.WriteLine(await NFMM.GetUIString());
                Thread.Sleep(RConfig.UIUpdateRateMS);
            }
        }

        private static async void SaveData() {
            if(!Directory.Exists(Path.GetDirectoryName(Program.RConfig.DataPath))) {
                Directory.CreateDirectory(Path.GetDirectoryName(Program.RConfig.DataPath) ?? "scraped");
            }
            while(Program.ShouldContinue) {
                try {
                    File.WriteAllText(Program.RConfig.DataPath, await NFMM.GetJsonString());
                }
                catch(Exception) {
                    throw;
                }
                Thread.Sleep(RConfig.SaveDataRateMS);
            }
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/
        // c# concurrent collections
        // SQLite
        private static async void SThread() {
            while(Program.ShouldContinue) {
                NFMMItem nItem = await NFMM.GetItem();
                if(nItem.Name == string.Empty) {
                    Thread.Sleep(1000);
                }
                else {
                    switch(nItem.Type) {

                        case NFMMType.Account:
                            string friendlist = "";
                            string carlist = "";
                            string stagelist = "";
                            try { friendlist = await Utils.GetTextHTTP($"http://multiplayer.needformadness.com/profiles/{nItem.Name}/friends.txt",Encoding.Latin1); } catch(Exception) { }
                            try { carlist = await Utils.GetTextHTTP($"http://multiplayer.needformadness.com/cars/lists/{nItem.Name}.txt",Encoding.Latin1); } catch(Exception) { }
                            try { stagelist = await Utils.GetTextHTTP($"http://multiplayer.needformadness.com/tracks/lists/{nItem.Name}.txt",Encoding.Latin1); } catch(Exception) { }
                            if(friendlist.Contains('|')) {
                                foreach(string item in friendlist.Split("\r\n")[0].Split('|')) {
                                    NFMM.AddItem(new NFMMItem {
                                        Type = NFMMType.Account,
                                        Name = item.ToLower()
                                    });
                                }
                            }
                            if(carlist.StartsWith("mycars(")) {
                                foreach(string item in carlist.Split('(',')')[1].Split(',')) {
                                    NFMM.AddItem(new NFMMItem {
                                        Type = NFMMType.Car,
                                        Name = item.Replace(' ','_')
                                    });
                                }
                            }
                            if(stagelist.StartsWith("mystages(")) {
                                foreach(string item in stagelist.Split('(',')')[1].Split(',')) {
                                    NFMM.AddItem(new NFMMItem {
                                        Type = NFMMType.Stage,
                                        Name = item.Replace(' ','_')
                                    });
                                }
                            }
                            break;

                        case NFMMType.Car:
                            List<string> cdetails = await Utils.GetAddedByList(nItem);
                            foreach(string item in cdetails) {
                                NFMM.AddItem(new NFMMItem {
                                    Type = NFMMType.Account,
                                    Name = item
                                });
                            }
                            break;

                        case NFMMType.Stage:
                            List<string> sdetails = await Utils.GetAddedByList(nItem);
                            foreach(string item in sdetails) {
                                NFMM.AddItem(new NFMMItem {
                                    Type = NFMMType.Account,
                                    Name = item
                                });
                            }
                            break;
                    }
                }
            }
        }
    }
}