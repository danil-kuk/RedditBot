using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace RedditBot
{
    class UserSession
    {
        public long SessionOwner { get; set; }
        public Timer SessionTime { get; private set; }
        public VkApiBot VkBot { get; }
        public RedditApiBot RedditBot { get; }

        public UserSession(long userId, VkApiBot bot)
        {
            SessionOwner = userId;
            VkBot = bot;
            RedditBot = new RedditApiBot(Program.RedditBotConfig, bot);
            SetTimer();
        }

        private void SetTimer()
        {
            SessionTime = new Timer(10 * 60 * 1000); //Длина сессии 10 минут
            SessionTime.Elapsed += OnTimedEvent;
            SessionTime.AutoReset = true;
            SessionTime.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            SessionTime.Stop();
            //Лучше не писать об окончании сессии, потому что бот будет спамить людей
            //VkBot.WriteToSelectedUser(SessionOwner, "Сессия окончена", MessageKeyboardSchemes.DefaultButtons);
            VkBot.AllUserSessions.Remove(SessionOwner);
        }
    }
}
