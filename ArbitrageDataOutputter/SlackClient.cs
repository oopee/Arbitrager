using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ArbitrageDataOutputter
{
    public class SlackClient
    {
        public class SlackMessage
        {
            public string Text { get; set; }
            public List<SlackAttachment> Attachments { get; private set; }

            public SlackMessage()
            {
                Attachments = new List<SlackAttachment>();
            }

            public void AddAttachment(SlackAttachment attachment)
            {
                Attachments.Add(attachment);
            }
        }

        public class SlackAttachment
        {
            public string Color { get; set; }
            public string Pretext { get; set; }

            // Timestamp
            // public int Ts { get; set; }

            public List<SlackAttachmentField> Fields { get; private set; }

            public SlackAttachment()
            {
                Fields = new List<SlackAttachmentField>();

                // Enable if bottom timestamp seems useful
                // TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                // Ts = (int)t.TotalSeconds;
            }

            public void AddField(string title, string value)
            {
                var field = new SlackAttachmentField()
                {
                    Title = title,
                    Value = value
                };

                Fields.Add(field);
            }
        }

        public class SlackAttachmentField
        {
            public string Title { get; set; }
            public string Value { get; set; }
        }

        private readonly Uri m_webhookUrl;
        private readonly HttpClient m_httpClient = new HttpClient();

        public SlackClient(Uri webhookUrl)
        {
            m_webhookUrl = webhookUrl;
        }

        public Task<HttpResponseMessage> SendMessageAsync(string message,
            string channel = null, string username = null)
        {
            var slackMessage = new SlackMessage()
            {
                Text = message
            };

            return SendMessageAsync(slackMessage, channel, username);
        }

        public async Task<HttpResponseMessage> SendMessageAsync(SlackMessage message,
            string channel = null, string username = null)
        {
            var payload = new
            {
                text = message.Text,
                channel,
                username,
                message.Attachments
            };

            var serializedPayload = JsonConvert.SerializeObject(payload, GetSerializerSettings());
            var response = await m_httpClient.PostAsync(m_webhookUrl,
                new StringContent(serializedPayload, Encoding.UTF8, "application/json"));

            return response;
        }

        private JsonSerializerSettings GetSerializerSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new LowercaseContractResolver();

            return settings;
        }

        // Setting all property names to lowercase makes the default serialization
        // compatible with what Slack API expects
        public class LowercaseContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return propertyName.ToLower();
            }
        }
    }
}
