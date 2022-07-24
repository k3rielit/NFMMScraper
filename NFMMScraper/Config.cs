using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFMMScraper {
    public class Config {
        public string InitialAccount = "hrz";
        public string DataPath = "scraped/data.json";
        public string Top20DataPath = "scraped/top20.json";
        public string AccDetailsDir = "profiles";
        public string CarDetailsDir = "cars";
        public string StgDetailsDir = "tracks";
        public string CarFileDir = "cars";
        public string StageFileDir = "tracks";
        public string MusicFileDir = "tracks/music";
        public ulong SoftwareThreadCount = 10;
        public int SaveDataRateMS = 2000;
        public int UIUpdateRateMS = 200;
        public bool SaveDetails = true;
        public bool DownloadTop20 = false;
        public bool ExtractMusic = false;
        public bool ExtractRadq = false;
        public bool RescanCompletedItemsOnStartup = false;
        public bool ReDownloadFilesOnStartup = false;
        public bool BackupDataOnStartup = false;
        public bool IndentJson = true;
        // ... clans, other profile data
    }
}
