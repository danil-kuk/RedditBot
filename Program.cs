using Newtonsoft.Json;
using System;
using System.IO;

namespace RedditBot
{
    class Program
    {
        public static RedditBotConfig RedditBotConfig { get; private set; }
        public static VkBotConfig VkBotConfig { get; private set; }
        static void Main()
        {
            try
            {
                var redditConfig = File.ReadAllText("redditbot_config.json");
                var vkConfig = File.ReadAllText("vkbot_config.json");
                RedditBotConfig = JsonConvert.DeserializeObject<RedditBotConfig>(redditConfig);
                VkBotConfig = JsonConvert.DeserializeObject<VkBotConfig>(vkConfig);
            }
            catch (Exception)
            {
                Console.WriteLine("Ошибка! Проверьте файлы конфигурации");
            };
            var vkBot = new VkApiBot(VkBotConfig);
            vkBot.Listen();
            //var redditBot = new RedditApiBot(RedditBotConfig, vkBot);
            //redditBot.GetSubredditNewPosts("dankmemes");
        }
    }
}
