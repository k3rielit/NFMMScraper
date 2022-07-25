using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using ConcurrentCollections;

namespace NFMMScraper {

    public enum NFMMType : byte { Account, Car, Stage }

    public sealed record NFMMItem {
        public NFMMType Type { get; set; } = NFMMType.Account;
        public string Name { get; set; } = string.Empty;
    }

    class NFMMJson {
        public Dictionary<NFMMType,List<string>> Completed = new() {
            [NFMMType.Account] = new(),
            [NFMMType.Car] = new(),
            [NFMMType.Stage] = new(),
        };
        public Dictionary<NFMMType,List<string>> Queue = new() {
            [NFMMType.Account] = new(),
            [NFMMType.Car] = new(),
            [NFMMType.Stage] = new(),
        };
    }

    public static class NFMM {
        public static ConcurrentHashSet<NFMMItem> Completed = new();
        public static ConcurrentHashSet<NFMMItem> Queue = new();
        public static async Task<string> GetUIString() {
            string result = $"Queue[{Queue.Count}] Completed[{Completed.Count}]";
            return result;
        }

        public static async Task<string> GetJsonString() {
            NFMMJson nj = new();
            foreach(var item in Completed) {
                nj.Completed[item.Type].Add(item.Name);
            }
            foreach(var item in Queue) {
                nj.Queue[item.Type].Add(item.Name);
            }
            return JsonConvert.SerializeObject(nj,Program.RConfig.IndentJson ? Formatting.Indented : Formatting.None);
        }
    }
}
