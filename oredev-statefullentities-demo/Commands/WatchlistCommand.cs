using Convo.Options;
using Convo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oredev_statefullentities_demo.Commands
{
    public class WatchlistCommand : ConvoCommand
    {
        public WatchlistCommand()
        {
            Id = "WatchlistCommand";
            RequireAuthentication = true;
            Command = "watchlist";
            Description = "Manage your stock watchlist";
        }

        public override Task<ConvoResponse?> HandleCommand(IConvoContext context, ConvoMessage command)
        {
            ConvoResponse? response = null;

            if (command.Arguments.Any())
            {
                if (command.Arguments.Length >= 1)
                {
                    switch (command.Arguments[0].ToLowerInvariant())
                    {
                        case "add":
                            {
                                if (command.Arguments.Length > 1)
                                {
                                    context.Data.Remove("watchlist-addview");

                                    AddWatchedAsset(context, command.Arguments[1]);
                                    response = DisplayMainMenu(updateMessageId: command.MessageId);
                                }
                                else
                                {
                                    context.ExpectingReplyActionId = Id;

                                    context.Data.AddOrUpdate("watchlist-addview", command.MessageId);

                                    response = new OptionsResponse
                                    {
                                        Text = "Please respond with a ticker you would like to add to the watchlist",
                                        UpdateMessageId = command.MessageId,
                                        ReplyOptions = new ConvoOptions
                                        {
                                            new ConvoOption
                                            {
                                                Text = "<< Back to menu",
                                                Command = "/watchlist",
                                            }
                                        }
                                    };
                                }
                            }
                            break;
                        case "remove":
                            {
                                if (command.Arguments.Length > 1)
                                {
                                    RemoveWatchedAsset(context, command.Arguments[1]);

                                    response = DisplayMainMenu(updateMessageId: command.MessageId);
                                }
                                else
                                {
                                    ConvoOptions assets = GetWatchedAssets(context)
                                        .Select(x => new ConvoOption
                                        {
                                            Text = x.ToUpperInvariant(),
                                            Command = $"/watchlist remove {x}"
                                        }).ToConvoOptions();

                                    response = new OptionsResponse
                                    {
                                        Text = "Please select the ticker you would like to remove from your watchlist",
                                        UpdateMessageId = command.MessageId,
                                        ReplyOptions = new ConvoOptions(assets)
                                        {
                                            new ConvoOption
                                            {
                                                Text = "<< Back to menu",
                                                Command = "/watchlist",
                                            }
                                        }
                                    };
                                }
                            }
                            break;
                        case "exit":
                            response = new ConvoResponse
                            {
                                DeleteMessageId = command.MessageId,
                            };
                            break;
                        case "view":
                            response = ManageWatchlistSubscriptions(context, command.Arguments);
                            response.UpdateMessageId = command.MessageId;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    response = DisplayMainMenu(updateMessageId: command.MessageId);
                }
            }
            else
            {
                response = DisplayMainMenu(deleteMessageId: command.MessageId);
            }

            return Task.FromResult(response);
        }

        public override Task<ConvoResponse?> HandleReply(IConvoContext context, ConvoMessage reply)
        {
            if (context.Data.ContainsKey("watchlist-addview"))
            {
                IEnumerable<string> assets = new List<string>
                {
                    "NSKOG", "GSF", "MOWI", "SRBANK", "WSTEP", "SIOFF", "SOFF", "KOA", "FROY", "KOMP", "KOMPL", "VAR", "OKEA", "NHY", "FJELL", "FJORD"
                }.Where(x => x.Contains(reply.Text.ToUpperInvariant()));

                ConvoOptions result = assets.Select(x => new ConvoOption
                {
                    Text = x.ToUpperInvariant(),
                    Command = $"/watchlist add {x}"
                }).ToConvoOptions();

                return Task.FromResult<ConvoResponse?>(new OptionsResponse
                {
                    Text = result.Any() ? "Found the following assets fitting the query" : $"Could not find any asset fitting the query {reply.Text}",
                    UpdateMessageId = context.Data["watchlist-addview"] as string,
                    DeleteMessageId = reply.MessageId,
                    ReplyOptions = new ConvoOptions(result)
                    {
                        new ConvoOption
                        {
                            Text = "<< Back to menu",
                            Command = "/watchlist",
                        }
                    }
                });
            }
            else
            {
                return Task.FromResult<ConvoResponse?>(DisplayMainMenu(deleteMessageId: reply.MessageId));
            }
        }

        private OptionsResponse DisplayMainMenu(string? deleteMessageId = null, string? updateMessageId = null)
        {
            return new OptionsResponse
            {
                DeleteMessageId = deleteMessageId,
                UpdateMessageId = updateMessageId,
                Text = "Please choose a option",
                ReplyOptions = new ConvoOptions
                {
                    new ConvoOptionRow
                    {
                        new ConvoOption
                        {
                            Text = "Add",
                            Command = "/watchlist add",
                        },
                        new ConvoOption
                        {
                            Text = "Remove",
                            Command = "/watchlist remove"
                        }
                    },
                    new ConvoOption
                    {
                        Text = "View / Manage",
                        Command = "/watchlist view",
                    },
                    new ConvoOption
                    {
                        Text = "Exit",
                        Command = "/watchlist exit",
                    }
                }
            };
        }

        private List<string> GetWatchedAssets(IConvoContext context)
        {
            List<string> items = new List<string>();
            if (context.Data.ContainsKey("watchlist-assets"))
            {
                if (context.Data["watchlist-assets"] is Newtonsoft.Json.Linq.JArray assets)
                {
                    items = assets.Values<string>().ToList();
                }
            }
            return items;
        }

        private void RemoveWatchedAsset(IConvoContext context, string asset)
        {
            List<string> items = GetWatchedAssets(context);
            items.Remove(asset);
            context.Data.AddOrUpdate("watchlist-assets", items);
        }

        private void AddWatchedAsset(IConvoContext context, string asset)
        {
            List<string> items = GetWatchedAssets(context);
            if (!items.Contains(asset))
            {
                items.Add(asset);
            }
            context.Data.AddOrUpdate("watchlist-assets", items);
        }

        private OptionsResponse ManageWatchlistSubscriptions(IConvoContext context, string[] arguments)
        {
            List<string> items = GetWatchedAssets(context);

            OptionsResponse resp = new OptionsResponse("You have the following assets on your watchlist:");

            if (items.Any())
            {
                foreach (string asset in items)
                {
                    resp.ReplyOptions.Add(new ConvoOption
                    {
                        Text = asset.ToUpperInvariant(),
                        Command = $"/watchlist view {asset}",
                    });
                }
            }
            else
            {
                resp.Text = "You have no items on your watchlist. Please add some assets to watch.";
            }

            resp.ReplyOptions.Add(new ConvoOption
            {
                Text = "<< Back to menu",
                Command = "/watchlist",
            });

            return resp;
        }
    }
}
