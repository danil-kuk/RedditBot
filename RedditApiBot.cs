using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedditBot
{
    class RedditApiBot
    {
        readonly RedditAPI reddit = new RedditAPI();
        readonly string appId;
        readonly string refreshToken;
        readonly string appSecret;
        readonly string accessToken;
        private readonly VkApiBot vkBot;

        public List<string> MonitoredSubs { get; set; } = new List<string>();
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
            var subreddit = reddit.Subreddit(subredditName);
            subreddit.Posts.GetNew();
            subreddit.Posts.NewUpdated += Posts_NewUpdated;
            subreddit.Posts.MonitorNew();
        }

        public async Task GetSubredditNewPostsAsync(string subredditName, 
            CancellationToken cancellationToken)
        {
            var subreddit = reddit.Subreddit(subredditName);
            cancellationToken.Register(() =>
            {
                subreddit.Posts.MonitorNew();
                subreddit.Posts.NewUpdated -= Posts_NewUpdated;
                MonitoredSubs.Remove(subredditName);
                vkBot.SendMessageToSelectedUser(89939784, "Остановлено получение контента из r/" + subredditName);
            });
            var task = Task.Run(() =>
            {
                subreddit.Posts.GetNew();
                subreddit.Posts.NewUpdated += Posts_NewUpdated;
                subreddit.Posts.MonitorNew();
                MonitoredSubs.Add(subredditName);
                vkBot.SendMessageToSelectedUser(89939784, "Подготавливаю контент с r/" + subredditName);
            }, cancellationToken);
            await task;
        }

        private void Posts_NewUpdated(object sender, PostsUpdateEventArgs e)
        {
            var post = e.Added[0];
            SendPostToUser(post, 89939784);
        }

        public void GetSubredditLastPost(string subredditName)
        {
            var subreddit = reddit.Subreddit(subredditName);
            var post = subreddit.Posts.New[0];
            SendPostToUser(post, 89939784);
            GetSubredditNewPosts(subredditName);
        }

        private void SendPostToUser(Post post, long userId)
        {
            vkBot.WriteToSelectedUserWithFile(userId,
                $"{post.Title} \n" +
                $"from r/{post.Subreddit}, \n" +
                $"by u/{post.Author} \n" +
                $"{post.UpVotes} ⬆️ \n",
                post.Listing.URL);
        }
    }
}
