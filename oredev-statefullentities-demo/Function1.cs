using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace oredev_statefullentities_demo
{
    public static class TelegramEntityStarter
    {
        [FunctionName("telegram-entity")]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            [DurableClient] IDurableEntityClient client,
            ILogger log)
        {
            log.LogInformation("Invoking telegram-entity");
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if(!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    Update data = JsonConvert.DeserializeObject<Update>(body);
                    if(data != null)
                    {
                        long chatId = data.Message?.Chat?.Id ?? data.EditedMessage?.Chat?.Id ?? data.CallbackQuery?.Message?.Chat?.Id ?? -1;

                        if(chatId > 0)
                        {
                            EntityId entityId = new EntityId("TelegramActor", chatId.ToString());

                            await client.SignalEntityAsync<ITelegramActor>(entityId, actor => actor.HandleMessage((data, chatId)));

                            return new OkResult();
                        }
                        log.LogError("Could not find a valid chatId to use for entity");
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error processing message from telegram");
                }
            }
            return new BadRequestErrorMessageResult("Could not process message");
        }
    }
}