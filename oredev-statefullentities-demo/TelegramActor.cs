using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using System;
using Telegram.Bot;
using Convo;
using System.Collections.Generic;
using oredev_statefullentities_demo.Commands;

namespace oredev_statefullentities_demo
{
    public interface ITelegramActor
    {
        Task HandleMessage((Update, long) data);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TelegramActor : ITelegramActor, IConvoContext
    {
        private readonly ILogger logger;
        public TelegramActor(ILogger logger)
        {
            this.logger = logger;
        }

        [JsonProperty("invocations")]
        public int InvocationCounter { get; set; }

        [JsonProperty("chatid")]
        public string ChatId { get; set; } = "";

        [JsonProperty("alias")]
        public string Alias { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("isauthenticated")]
        public bool IsAuthenticated { get; set; }

        [JsonProperty("expectingreplyactionId")]
        public string? ExpectingReplyActionId { get; set; }

        [JsonProperty("data")]
        public Dictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>();



        [FunctionName(nameof(TelegramActor))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx, ILogger log)
        {
            return ctx.DispatchAsync<TelegramActor>(log);
        }

        public async Task HandleMessage((Update, long) data)
        {
            InvocationCounter++;
            ChatId = data.Item2.ToString();

            await new TelegramHandler(logger, new List<ConvoCommand>()
            {
                new EchoCommand(),
                new WatchlistCommand()
            }).HandleMessage(data.Item1, this);
        }

        public void Reset(ConvoMessage message)
        {
            Alias = message.Alias ?? "";
            Name = message.Name ?? "";
            IsAuthenticated = false;
            ExpectingReplyActionId = null;
            Data = new Dictionary<string, object?>();
        }
    }
}