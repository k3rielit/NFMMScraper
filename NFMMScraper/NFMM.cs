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

namespace NFMMScraper {

    public enum NFMMType : byte { Account, Car, Stage }

    public sealed record NFMMItem {
        public NFMMType Type { get; set; } = NFMMType.Account;
        public string Name { get; set; } = string.Empty;
    }

    public static class NFMM {

        private static ulong ActionCount = 0;

        private static ConcurrentDictionary<NFMMType, ConcurrentDictionary<string,bool>> Items = new() {
            [NFMMType.Account] = new(),
            [NFMMType.Car] = new(),
            [NFMMType.Stage] = new(),
        };

        public static async Task<NFMMItem> GetItem() {
            NFMMItem nItem = new();
            foreach(var category in Items) {
                foreach(var item in category.Value) {
                    if(!item.Value) {
                        nItem.Type = category.Key;
                        nItem.Name = item.Key;
                        Items[nItem.Type].TryUpdate(nItem.Name,true,false);
                        break;
                    }
                }
            }
            return nItem;
        }

        public static async void AddItem(NFMMItem nItem) {
            if(nItem.Name.Length > 0) {
                Items[nItem.Type].TryAdd(nItem.Name,false);
            }
        }

        public static async Task<string> GetUIString() {
            string result = "";
            foreach(var category in Items) { // print true/false count
                result += $"{category.Key}[{category.Value.Count}]\n";
            }
            return result;
        }

        public static async Task<string> GetJsonString() {
            return JsonConvert.SerializeObject(Items,Program.RConfig.IndentJson ? Formatting.Indented : Formatting.None);
        }
    }
}
