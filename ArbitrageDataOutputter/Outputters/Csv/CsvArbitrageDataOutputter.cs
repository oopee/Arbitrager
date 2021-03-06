﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using CsvHelper;

namespace ArbitrageDataOutputter
{
    public class CsvArbitrageDataOutputter : AbstractArbitrageDataOutputter
    {
        public string FilePath { get; private set; }

        public CsvArbitrageDataOutputter(IArbitrageDataSource dataSource, string outputFilePath)
            : base(dataSource)
        {
            FilePath = outputFilePath;
        }

        public override Task Initialize()
        {
            EnsureFileExists();
            return Task.FromResult(0);
        }

        protected override Task OutputData(ArbitrageDataPoint info)
        {
            using (var ws = new StreamWriter(FilePath, append: true, encoding: Encoding.UTF8))
            {
                using (var csvWwriter = new CsvWriter(ws, GetConfiguration()))
                {
                    csvWwriter.WriteRecord(info);
                    csvWwriter.NextRecord();
                }
            }

            return Task.FromResult(0);
        }

        void EnsureFileExists()
        {
            if (!File.Exists(FilePath))
            {
                // Create file and write header
                using (var ws = new StreamWriter(FilePath, append: true, encoding: Encoding.UTF8))
                {
                    using (var csvWwriter = new CsvWriter(ws, GetConfiguration()))
                    {
                        csvWwriter.WriteHeader<ArbitrageDataPoint>();
                        csvWwriter.NextRecord();
                    }
                }
            }
        }
        
        CsvHelper.Configuration.Configuration GetConfiguration()
        {
            return new CsvHelper.Configuration.Configuration()
            {
                Delimiter = ";"
            };
        }

        protected override void OnStarted()
        {
            Console.WriteLine($"Started outputting CSV data to {GetFullPath()}");
        }

        protected override void OnStopped()
        {
            Console.WriteLine($"Stopped outputting CSV data to {GetFullPath()}");
        }
        
        private string GetFullPath()
        {
            string fullPath = FilePath;

            if (!Path.IsPathRooted(fullPath))
            {
                fullPath = Path.Combine(Directory.GetCurrentDirectory(), FilePath);
            }

            return fullPath;
        }
    }
}
