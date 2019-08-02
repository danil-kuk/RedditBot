using Newtonsoft.Json;
using System;
using System.IO;

namespace RedditBot
{
    class Program
    {
        static RedditBotConfig redditBotConfig;
        static VkBotConfig vkBotConfig;
        static void Main()
        {
            try
            {
                var redditConfig = File.ReadAllText("redditbot_config.json");
                var vkConfig = File.ReadAllText("vkbot_config.json");
                redditBotConfig = JsonConvert.DeserializeObject<RedditBotConfig>(redditConfig);
                vkBotConfig = JsonConvert.DeserializeObject<VkBotConfig>(vkConfig);
            }
            catch (Exception)
            {
                Console.WriteLine("Ошибка! Проверьте файлы конфигурации");
            };
            var vkBot = new VkApiBot(vkBotConfig);
            var redditBot = new RedditApiBot(redditBotConfig, vkBot);
            redditBot.GetSubredditNewPosts("dankmemes");
        }
    }
}
