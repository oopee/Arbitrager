using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArbitrageDataOutputter
{
    public class SlackAlertOutputter : AbstractArbitrageDataOutputter
    {
        public class AlertRule
        {
            public decimal AlertThreshold { get; set; }
            public TimeSpan MinTimeSinceLastAlert { get; set; }
            public string Description { get; set; }
            public string Tag { get; set; }

            public AlertRule(decimal threshold, TimeSpan minTimeSinceLastAlert, string description, string tag)
            {
                AlertThreshold = threshold;
                MinTimeSinceLastAlert = minTimeSinceLastAlert;
                Tag = tag;
                Description = description;
            }
        }

        private SlackClient Client { get; set; }

        private List<AlertRule> Rules { get; set; }

        private decimal? LastAlertValue { get; set; }
        private DateTime? LastAlertTime { get; set; }

        public SlackAlertOutputter(IArbitrageDataSource source, string webhookUrl)
            : base(source)
        {
            var uri = new Uri(webhookUrl);
            Client = new SlackClient(uri);
            Rules = new List<AlertRule>();

            AddAlertRule(2, TimeSpan.FromHours(1), "Profitable spread", "SPREAD2");
            AddAlertRule(3, TimeSpan.FromMinutes(30), "Good spread", "SPREAD3");
            AddAlertRule(4, TimeSpan.FromMinutes(15), "Very good spread", "SPREAD4");
            AddAlertRule(5, TimeSpan.FromMinutes(5), "Excellent spread", "SPREAD5");
            AddAlertRule(6, TimeSpan.FromMinutes(5), "All in!", "SPREAD6+");
        }

        protected override void OnStarted()
        {
            var slackMessage = GetColoredMessage("#36a64f", "Bot started");
            // Client.SendMessageAsync(slackMessage).Wait();
        }

        protected override void OnStopped()
        {
            var slackMessage = GetColoredMessage("#d00000", "Bot stopped");
            // Client.SendMessageAsync(slackMessage).Wait();
        }

        private SlackClient.SlackMessage GetColoredMessage(string hexColor, string message)
        {
            var slackMessage = new SlackClient.SlackMessage();
            var attachment = new SlackClient.SlackAttachment()
            {
                Color = hexColor
            };

            attachment.Fallback = message;
            attachment.AddField(message, "");
            slackMessage.AddAttachment(attachment);

            return slackMessage;
        }

        public void AddAlertRule(decimal threshold, TimeSpan minTimeSinceLastAlert, string description, string tag)
        {
            var rule = new AlertRule(threshold, minTimeSinceLastAlert, description, tag);
            Rules.Add(rule);
        }

        public override Task Initialize()
        {
            return Task.FromResult(0);
        }

        protected override Task OutputData(ArbitrageDataPoint info)
        {
            var alert = CheckAlerts(info);
            if (alert == null)
            {
                return Task.FromResult(0);
            }

            LastAlertTime = DateTime.Now;
            LastAlertValue = GetThresholdComparisonValue(info);

            var sm = GetSpreadAlertMessage(info, alert);

            return Client.SendMessageAsync(sm);
        }

        private SlackClient.SlackMessage GetSpreadAlertMessage(ArbitrageDataPoint info, AlertRule alert)
        {
            var slackMessage = new SlackClient.SlackMessage();

            var attachment = new SlackClient.SlackAttachment()
            {
                Color = "#36a64f",
                Pretext = GetPretext(info, alert)
            };

            attachment.Fallback = attachment.Pretext;

            attachment.AddField("Max negative spread", string.Format("{0:0.00} EUR / {1:0.00}%", info.MaxNegativeSpreadEur, info.MaxNegativeSpreadPercentage * 100));
            attachment.AddField(string.Format("{0} EUR profit", info.FiatLimit), string.Format("{0:0.00} EUR / {1:0.00}%", info.MaxProfitEur, info.MaxProfitPercentage * 100));

            attachment.AddField("Kraken best ask", string.Format("{0:0.00} EUR", info.BestAsk));
            attachment.AddField("GDAX best bid", string.Format("{0:0.00} EUR", info.BestBid));

            slackMessage.AddAttachment(attachment);

            return slackMessage;
        }

        private string GetPretext(ArbitrageDataPoint info, AlertRule alert)
        {
            string text = string.Format("{0} - {1:0.00}%", alert.Description, info.MaxNegativeSpreadPercentage * 100);
            if (!string.IsNullOrWhiteSpace(alert.Tag))
            {
                text = $"{text} [{alert.Tag}]";
            }

            return text;
        }

        private AlertRule CheckAlerts(ArbitrageDataPoint info)
        {
            decimal comparisonValue = GetThresholdComparisonValue(info);
            var highestRule = GetHighestRule(comparisonValue);

            // If there is no last alert, post current spread
            if (LastAlertTime == null && highestRule == null)
            {
                // return new AlertRule(0, TimeSpan.FromDays(1), "Current spread", "");
            }

            if (highestRule == null)
            {
                return null;
            }

            // If at least minimum time has passed, post again
            if (LastAlertTime == null || LastAlertTime + highestRule.MinTimeSinceLastAlert < DateTime.Now)
            {
                return highestRule;
            }
            // If value has risen from previous, always alert
            else if (comparisonValue > LastAlertValue)
            {
                return highestRule;
            }

            return null;
        }

        private decimal GetThresholdComparisonValue(ArbitrageDataPoint info)
        {
            return info.MaxProfitPercentage * 100;
        }

        private AlertRule GetHighestRule(decimal comparisonValue)
        {
            return Rules.Where(x => x.AlertThreshold <= comparisonValue).OrderBy(x => x.AlertThreshold).LastOrDefault();
        }
    }
}
