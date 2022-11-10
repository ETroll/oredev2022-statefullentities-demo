using Convo;
using Convo.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oredev_statefullentities_demo.Commands
{
    public class EchoCommand : ConvoCommand
    {
        public EchoCommand()
        {
            Id = "EchoCommand";
            RequireAuthentication = false;
            Command = "echo";
            Description = "Echo command";
        }

        public override Task<ConvoResponse?> HandleCommand(IConvoContext context, ConvoMessage command)
        {
            ConvoResponse? response = null;

            if(!command.Arguments.Any())
            {
                // Show the main command menu if no arguments are passed
                response = new OptionsResponse
                {
                    Text = "Echo command - Pick an option:",
                    DeleteMessageId = command.MessageId,
                    ReplyOptions = new ConvoOptions
                    {
                        new ConvoOption
                        {
                            Text = "Echo",
                            Command = "/echo reply"
                        },
                        new ConvoOption
                        {
                            Text = "Exit",
                            Command = "/echo exit"
                        }
                    }
                };
            }
            else
            {
                switch (command.Arguments[0].ToLowerInvariant())
                {
                    case "reply":
                        context.ExpectingReplyActionId = Id;
                        context.Data.AddOrUpdate("echo-messageid", command.MessageId);

                        response = new ConvoResponse
                        {
                            Text = "What do you want me to echo?",
                            UpdateMessageId = command.MessageId,
                        };
                        break;
                    case "exit":
                        response = new ConvoResponse
                        {
                            DeleteMessageId = command.MessageId
                        };
                        break;
                    default:
                        break;
                }
            }

            return Task.FromResult(response);
        }

        public override Task<ConvoResponse?> HandleReply(IConvoContext context, ConvoMessage reply)
        {
            if(context.Data.ContainsKey("echo-messageid"))
            {
                string? originatorMessageId = context.Data["echo-messageid"] as string;
                context.Data.Remove("echo-messageid");

                return Task.FromResult<ConvoResponse?>(new OptionsResponse
                {
                    Text = reply.Text,
                    DeleteMessageId = reply.MessageId,
                    UpdateMessageId = originatorMessageId,
                    ReplyOptions = new ConvoOptions
                    {
                        new ConvoOption
                        {
                            Text = "<< Back to menu",
                            Command = "/echo"
                        }
                    }
                });
            }

            return Task.FromResult<ConvoResponse?>(null);
        }
    }
}
