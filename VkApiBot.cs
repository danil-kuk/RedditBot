using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;

namespace RedditBot
{
    class VkApiBot
    {
        public VkApi Vk { get; private set; } = new VkApi();
        readonly string token;
        public Dictionary<long, UserSession> AllUserSessions { get; set; } = new Dictionary<long, UserSession>();
        public ulong MyGroupId { get; private set; }

        /// <summary>
        /// Создание бота для группы
        /// </summary>
        /// <param name="token">Ключ доступа API</param>
        /// <param name="groupId">ID группы, в которой будет работать бот</param>
        public VkApiBot(VkBotConfig config)
        {
            MyGroupId = config.GroupId;
            token = config.Token;
            Authorize();
        }

        /// <summary>
        /// Авторизация бота
        /// </summary>
        private void Authorize()
        {
            Vk.Authorize(new ApiAuthParams() { AccessToken = token });
        }

        /// <summary>
        /// Получение LongPollHistory
        /// </summary>
        private BotsLongPollHistoryResponse GetLongPollHistory()
        {
            var server = Vk.Groups.GetLongPollServer(MyGroupId);
            return Vk.Groups.GetBotsLongPollHistory(
               new BotsLongPollHistoryParams()
               { Server = server.Server, Ts = server.Ts, Key = server.Key, Wait = 25 });
        }

        /// <summary>
        /// Получить список всех диалогов бота
        /// </summary>
        public List<Peer> GetAllUsers()
        {
            var allConversations = Vk.Messages.GetConversations(new GetConversationsParams() { GroupId = MyGroupId });
            var usersToWrite = new List<Peer>();
            foreach (var conversation in allConversations.Items.Select(c => c.Conversation))
            {
                usersToWrite.Add(conversation.Peer);
            }
            return usersToWrite;
        }

        /// <summary>
        /// Отслеживание ботом всех событий сообщества
        /// </summary>
        public void Listen()
        {
            while (true)
            {
                var poll = GetLongPollHistory();
                if (poll?.Updates == null) continue; // Проверка на новые события
                foreach (var update in poll.Updates)
                {
                    if (update.Type == GroupUpdateType.MessageNew)
                    {
                        var userId = update.Message.FromId.Value;
                        if (!AllUserSessions.ContainsKey(userId))
                            AllUserSessions.Add(userId, new UserSession(userId, this));
                        RegularMessageResponse(update.Message);
                    }
                }
            }
        }

        private async void RegularMessageResponse(Message message)
        {
            var userId = message.FromId.Value;
            var text = message.Text.ToLower();
            var supportedSubs = new List<string> { "pics", "dankmemes", "memes" };
            if (supportedSubs.Contains(text) &&
                !AllUserSessions[userId].RedditBot.MonitoredSubs.Contains(text))
            {
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); //Отправлять посты в течении 2 минут
                await AllUserSessions[userId].RedditBot.GetSubredditNewPostsAsync(text, cts.Token);
            }
            else if (AllUserSessions[userId].RedditBot.MonitoredSubs.Contains(text))
                SendMessageToSelectedUser(userId, "Уже отслеживается");
            else
                SendMessageToSelectedUser(userId, "Неверная команда");
        }

        /// <summary>
        /// Написать пользователю сообщение с файлом
        /// </summary>
        /// <param name="userId">Id получателя</param>
        /// <param name="message">Сообщение</param>
        /// <param name="fileUrl">Ссылка на файл</param>
        public async void WriteToSelectedUserWithFile(long userId, string message, string fileUrl)
        {
            // Проверка на наличие подходящего файла в посте
            var fileExtension = fileUrl.Split('.').Last();
            var supportedFileExtensions = new List<string> { "jpg", "png", "gif" };
            if (!supportedFileExtensions.Contains(fileExtension) && fileExtension.Length < 5)
                return;
            // Получить адрес сервера для загрузки файлов
            var uploadServer = Vk.Docs.GetMessagesUploadServer(userId, DocMessageType.Doc);
            if (fileExtension == "jpg" || fileExtension == "png")
                uploadServer = Vk.Photo.GetMessagesUploadServer(userId);
            // Загрузить файл.
            var response = await UploadFile(uploadServer.UploadUrl, fileUrl, fileExtension);
            // Сохранить загруженный файл
            List<MediaAttachment> attachment;
            if (fileExtension == "jpg" || fileExtension == "png")
                attachment = TryGetPhotos(response).ToList<MediaAttachment>();
            else
            {
                var docs = TryGetDocs(response)?.Instance;
                attachment = new List<MediaAttachment> { docs };
            }
            //Отправить сообщение
            SendMessageToSelectedUser(userId, message, attachment);
        }

        /// <summary>
        /// Попытаться получить документ из ответа после загрузки файла
        /// </summary>
        /// <param name="response">Ответ от сервера после загрузки файла</param>
        /// <returns>Загруженное фото в необходимом для ВК формате, иначе null</returns>
        private Attachment TryGetDocs(string response)
        {
            try
            {
                return Vk.Docs.Save(response, Guid.NewGuid().ToString())[0];
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Написать пользователю сообщение
        /// </summary>
        /// <param name="userId">Id получателя</param>
        /// <param name="message">Сообщение</param>
        /// <param name="attachment">Вложения к сообщению</param>
        public void SendMessageToSelectedUser(long userId, string message,
            ICollection<MediaAttachment> attachment = null)
        {
            var messageSendParams = new MessagesSendParams
            {
                Attachments = attachment?.Where(item => item != null),
                UserId = userId,
                Message = message,
                RandomId = new Random().Next(999999)
            };
            Vk.Messages.Send(messageSendParams);
        }

        /// <summary>
        /// Попытаться получить фото из ответа после загрузки файла
        /// </summary>
        /// <param name="response">Ответ от сервера после загрузки картинки</param>
        /// <returns>Загруженное фото в необходимом для ВК формате, иначе null</returns>
        private ICollection<Photo> TryGetPhotos(string response)
        {
            try
            {
                return Vk.Photo.SaveMessagesPhoto(response);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Загрузка файла на сервера ВК
        /// </summary>
        /// <param name="url">URL сервера загрузки ВК</param>
        /// <param name="fileUrl">Ссылка на файл, который нужно загрузить</param>
        /// <param name="fileExtension">Расширение загружаемого файла</param>
        /// <returns>Запрос на загрузку файла на сервера ВК</returns>
        private async Task<string> UploadFile(string url, string fileUrl, string fileExtension)
        {
            // Получение массива байтов из файла
            byte[] data;
            using (var webClient = new WebClient())
            {
                data = webClient.DownloadData(fileUrl);
            }
            // Создание запроса на загрузку файла на сервер
            using (var client = new HttpClient())
            {
                var requestContent = new MultipartFormDataContent();
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                requestContent.Add(content, "file", $"file.{fileExtension}");

                var response = await client.PostAsync(url, requestContent);
                return Encoding.Default.GetString(await response.Content.ReadAsByteArrayAsync());
            }
        }
    }
}