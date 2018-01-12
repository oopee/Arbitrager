using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;

namespace ArbitrageDataOutputter
{
    class GoogleSheetsArbitrageDataOutputter : AbstractArbitrageDataOutputter
    {
        // If modifying these scopes, delete your previously saved credentials
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        
        public string SpreadSheetId { get; private set; }

        private UserCredential Credentials { get; set; }
        private SheetsService Service { get; set; }
        private Google.Apis.Sheets.v4.Data.Spreadsheet Spreadsheet { get; set; }

        public GoogleSheetsArbitrageDataOutputter(IArbitrageDataSource dataSource, string spreadSheetId)
            : base(dataSource) 
        {
            SpreadSheetId = spreadSheetId;
        }

        public async override Task Initialize()
        {
            EnsureCredentials();

            Service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credentials
            });

            var getRequest = Service.Spreadsheets.Get(SpreadSheetId);
            Spreadsheet = await getRequest.ExecuteAsync();
        }

        protected override void OnStarted()
        {
            Console.WriteLine($"Started outputting data to spreadsheet '{Spreadsheet.Properties.Title}'");
        }

        protected override void OnStopped()
        {
            Console.WriteLine($"Stopped outputting data to spreadsheet '{Spreadsheet.Properties.Title}'");
        }

        protected override async Task OutputData(ArbitrageDataPoint info)
        {
            EnsureCredentials();

            // Appended rows start from column A
            string range = "A:A";

            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credentials
            });
            
            var rowData = new List<object>();
            rowData.Add(info.BestAsk);
            rowData.Add(info.BestBid);
            rowData.Add(info.MaxNegativeSpreadEur);
            rowData.Add(info.MaxNegativeSpreadPercentage);
            rowData.Add(info.MaxProfitEur);
            rowData.Add(info.MaxProfitPercentage);
            rowData.Add(info.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss").Replace(".", ":"));

            var body = new Google.Apis.Sheets.v4.Data.ValueRange();
            body.Values = new List<IList<object>>();
            body.Values.Add(rowData);

            var appendReq = service.Spreadsheets.Values.Append(body, SpreadSheetId, range);
            appendReq.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var response = await appendReq.ExecuteAsync();
        }

        private void EnsureCredentials()
        {
            if (Credentials != null)
            {
                return;
            }

            var clientSecret = Properties.Resources.arbitrager_client_secret;

            using (var stream = new MemoryStream(clientSecret))
            {
                string credPath = ".credentials/sheets.googleapis.com-arbitrager.json";
                bool credentialFileExisted = Directory.Exists(credPath);

                Credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;

                if (!credentialFileExisted)
                {
                    Console.WriteLine("Credential file saved to: " + credPath);
                }
            }
        }
    }
}
