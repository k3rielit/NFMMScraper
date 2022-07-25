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
    public class Utils {
        public static async Task<string> GetTextHTTP(string url, Encoding encoding, bool rethrowError = false) {
            string result = "";
            try {
                HttpClient client = new();
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
                result = encoding is null ? Encoding.Latin1.GetString(responseBody) : encoding.GetString(responseBody);
            }
            catch(Exception) {
                if(rethrowError) throw;
            }
            return result;
        }
        public static async Task<List<string>> GetAddedByList(NFMMItem nItem) {
            List<string> result = new();
            string details = "";
            try { details = await Utils.GetTextHTTP($"http://multiplayer.needformadness.com/{(nItem.Type==NFMMType.Car ? "cars" : "tracks")}/{nItem.Name.Replace(' ','_')}.txt",Encoding.Latin1); } catch(Exception) { }
            if(details.StartsWith("details(")) {
                result = details.Split('(',')')[1].Split(',').Select(s => s.Replace(' ','_').ToLower()).Where(w => w.Length > 0).ToList();
                result.RemoveRange(1,nItem.Type == NFMMType.Car ? 2 : 1);
            }
            return result;
        }
    }
}
