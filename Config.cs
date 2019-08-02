using Newtonsoft.Json;
using System;

namespace RedditBot
{
    [Serializable]
    class RedditBotConfig
    {
        [JsonProperty("app_id")]
        public string AppId { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("app_secret")]
        public string AppSecret { get; set; }
    }

    [Serializable]
    class VkBotConfig
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("group_id")]
        public ulong GroupId { get; set; }
    }
}
