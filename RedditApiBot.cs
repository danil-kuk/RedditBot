using Reddit;
using Reddit.Controllers;
using System.Collections.Generic;

namespace RedditBot
{
    class RedditApiBot
    {
        public readonly RedditAPI reddit = new RedditAPI();
        readonly string appId;
        readonly string refreshToken;
        readonly string appSecret;
        readonly string accessToken;
        private readonly VkApiBot vkBot;
        public RedditApiBot(RedditBotConfig config, VkApiBot vkBot)
        {
            appId = config.AppId;
            refreshToken = config.RefreshToken;
            appSecret = config.AppSecret;
            accessToken = config.AccessToken;
            this.vkBot = vkBot;
            reddit = new RedditAPI(appId, refreshToken, appSecret, accessToken);
        }

        public void GetSubredditNewPosts(string subredditName)
        {
            var subreddit = reddit.Subreddit(subredditName).About();
            subreddit.Posts.GetNew();
            subreddit.Posts.NewUpdated += (sender, arg) =>
            {
                var post = arg.Added[0];
                SendPostToUser(post, subreddit, 89939784);
            };
            subreddit.Posts.MonitorNew();
        }

        public void GetSubredditLastPost(string subredditName)
        {
            var subreddit = reddit.Subreddit(subredditName).About();
            var post = subreddit.Posts.New[0];
            SendPostToUser(post, subreddit, 89939784);
            GetSubredditNewPosts(subredditName);
        }

        private void SendPostToUser(Post post, Subreddit subreddit, long userId)
        {
            vkBot.WriteToSelectedUserWithFile(userId,
                $"{post.Title} \n" +
                $"from r/{subreddit.Title}, \n" +
                $"by u/{post.Author} \n" +
                $"{post.UpVotes} ⬆️ \n",
                post.Listing.URL);
        }
    }
}
