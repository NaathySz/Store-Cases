using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using StoreApi;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Reflection;

namespace Store_Cases;

public class Store_CasesConfig : BasePluginConfig
{
    [JsonPropertyName("Store_Cases_commands")]
    public List<string> StoreCasesCommands { get; set; } = ["storecases", "buycase"];

    [JsonPropertyName("Store_Quick_Buy_Cases_commands")]
    public List<string> StoreQuickBuyCasesCommands { get; set; } = ["quickbuycase"];

    [JsonPropertyName("Store_Give_Cases_commands")]
    public List<string> StoreGiveCasesCommands { get; set; } = ["givecase"];

    [JsonPropertyName("Store_Give_Cases_permission")]
    public string StoreGiveCasesPermission { get; set; } = "@css/root";

    [JsonPropertyName("Buy_Cases_Cooldown")]
    public int BuyCasesCooldown { get; set; } = 10;

    [JsonPropertyName("Menu_Type")]
    public string MenuType { get; set; } = "html"; // html | chat

    [JsonPropertyName("Item_Action_If_Owned")]
    public int ItemActionIfOwned { get; set; } = 1; // 0 = Nothing | 1 = Reward with Credits

    [JsonPropertyName("Credit_Percentage")]
    public int ItemActionCreditPercentage { get; set; } = 50;

    [JsonPropertyName("Cases")]
    public List<CaseItem> Cases { get; set; } = new()
    {
        new CaseItem
        {
            Name = "Standard",
            Price = 500,
            Flag = "@css/vip",
            Rewards = new List<CaseReward>
            {
                new CaseReward { Type = "credits", Value = "1000", Description = "1000 Credits", Chance = 90, PrintToChatAll = false },
                new CaseReward { Type = "playerskin", Value = "characters/models/nozb1/2b_nier_automata_player_model/2b_nier_player_model.vmdl", Description = "2B Player Model", Expiration = 172800, Chance = 10, PrintToChatAll = true }
            }
        },
        new CaseItem
        {
            Name = "Example",
            Price = 1000,
            Rewards = new List<CaseReward>
            {
                new CaseReward { Type = "credits", Value = "3000", Description = "3000 Credits", Chance = 50, PrintToChatAll = false },
                new CaseReward { Type = "playerskin", Value = "characters/models/ctm_diver/ctm_diver_variantb.vmdl", Description = "Fernandez Frogman", Expiration = 86400, Chance = 20, PrintToChatAll = false },
                new CaseReward { Type = "playerskin", Value = "characters/models/tm_professional/tm_professional_vari.vmdl", Description = "Number K", Expiration = 0, Chance = 15, PrintToChatAll = true },
                new CaseReward { Type = "vip", Value = "css_vip_adduser \"{SteamID}\" \"VIP_Bronze\" \"1440\"", Description = "Vip Bronze (1 day)", Chance = 15, PrintToChatAll = true }
            }
        }
    };

    [JsonPropertyName("Animation_Duration")]
    public float AnimationDuration { get; set; } = 3f;

    [JsonPropertyName("Animation_Interval")]
    public float AnimationInterval { get; set; } = 0.1f;

    [JsonPropertyName("Animation_Html")]
    public string AnimationHtml { get; set; } = "Rolling...<br><font color='#FF0000'>{reward}</font>";

    [JsonPropertyName("Reward_Html")]
    public string FinalHtml { get; set; } = "You won:<br><font color='#00FF00'>{reward}</font>";

    [JsonPropertyName("Open_case_sound")]
    public string OpenCaseSound { get; set; } = "/sounds/ui/csgo_ui_crate_open.vsnd_c";

    [JsonPropertyName("Roll_item_sound")]
    public string RollItemSound { get; set; } = "/sounds/ui/csgo_ui_crate_item_scroll.vsnd_c";

    [JsonPropertyName("Won_item_sound")]
    public string WonItemSound { get; set; } = "/sounds/ui/panorama/inventory_new_item_01.vsnd_c";

    [JsonPropertyName("Use_Html")]
    public bool UseHtml { get; set; } = false;

    [JsonPropertyName("Show_Reward_Chances")]
    public bool ShowRewardChances { get; set; } = true;

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 1;
}

public class CaseItem
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Price")]
    public int Price { get; set; } = 0;

    [JsonPropertyName("Flag")]
    public string? Flag { get; set; } = null;

    [JsonPropertyName("Rewards")]
    public List<CaseReward> Rewards { get; set; } = new();
}

public class CaseReward
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("Value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("Expiration")]
    public int Expiration { get; set; } = 0;

    [JsonPropertyName("Chance")]
    public float Chance { get; set; } = 100f;
    
    [JsonPropertyName("PrintToChatAll")]
    public bool PrintToChatAll { get; set; } = false;
}


public class Store_Cases : BasePlugin, IPluginConfig<Store_CasesConfig>
{
    public override string ModuleName => "Store Module [Cases]";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "Nathy";

    public IStoreApi? StoreApi { get; set; }
    public Store_CasesConfig Config { get; set; } = new();
    private ConcurrentDictionary<string, CaseAnimation> activeAnimations = new();
    private readonly ConcurrentDictionary<string, DateTime> playerLastBuyCaseCommandTimes = new();

    public void OnConfigParsed(Store_CasesConfig config)
    {
        Config = config;
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");
        CreateCommands();
    }

    private void CreateCommands()
    {
        foreach (var cmd in Config.StoreCasesCommands)
        {
            AddCommand($"css_{cmd}", "Open the Cases menu", Command_CasesMenu);
        }

        foreach (var cmd in Config.StoreGiveCasesCommands)
        {
            AddCommand($"css_{cmd}", "Give a case to a player", Command_GiveCase);
        }

        foreach (var cmd in Config.StoreQuickBuyCasesCommands)
        {
            AddCommand($"css_{cmd}", "Quick buy a case by name or index", Command_QuickBuyCase);
        }
    }

    [CommandHelper(minArgs: 1, usage: "[case_name_or_index]")]
    public void Command_QuickBuyCase(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) return;

        if (playerLastBuyCaseCommandTimes.TryGetValue(player.SteamID.ToString(), out var lastCommandTime))
        {
            var cooldownRemaining = (DateTime.Now - lastCommandTime).TotalSeconds;
            if (cooldownRemaining < Config.BuyCasesCooldown)
            {
                var secondsRemaining = (int)(Config.BuyCasesCooldown - cooldownRemaining);
                player.PrintToChat(Localizer["Prefix"] + Localizer["In cooldown", secondsRemaining]);
                return;
            }
        }

        playerLastBuyCaseCommandTimes[player.SteamID.ToString()] = DateTime.Now;

        string caseArg = command.GetArg(1);
        CaseItem? caseItem = null;

        if (int.TryParse(caseArg, out int caseIndex))
        {
            if (caseIndex > 0 && caseIndex <= Config.Cases.Count)
            {
                caseItem = Config.Cases[caseIndex - 1];
            }
            else
            {
                player.PrintToChat(Localizer["Prefix"] + Localizer["Invalid case index", caseArg]);
                return;
            }
        }
        else
        {
            caseItem = Config.Cases.FirstOrDefault(c => c.Name.Equals(caseArg, StringComparison.OrdinalIgnoreCase));
            if (caseItem == null)
            {
                player.PrintToChat(Localizer["Prefix"] + Localizer["Case not found", caseArg]);
                return;
            }
        }

        StartCaseAnimation(player, caseItem);
    }

    [CommandHelper(minArgs: 2, usage: "[target] [case_name]")]
    public void Command_GiveCase(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller != null)
        {
            if (!AdminManager.PlayerHasPermissions(caller, Config.StoreGiveCasesPermission))
            {
                caller.PrintToChat(Localizer["Prefix"] + Localizer["No permission to give case"]);
                return;
            }

            var targetResult = command.GetArgTargetResult(1);

            string caseName = command.GetArg(2);

            var caseItem = Config.Cases.FirstOrDefault(c => c.Name.Equals(caseName, StringComparison.OrdinalIgnoreCase));
            if (caseItem == null)
            {
                caller?.PrintToChat(Localizer["Prefix"] + Localizer["Case not found", caseName]);
                return;
            }

            foreach (var player in targetResult.Players)
            {
                if (player.IsValid && !player.IsBot && !player.IsHLTV)
                {
                    Server.PrintToChatAll(Localizer["Prefix"] + Localizer["Case given", player.PlayerName, caseName]);
                    StartCaseAnimation(player, caseItem);
                }
                else
                {
                    caller?.PrintToChat(Localizer["Prefix"] + Localizer["Invalid player"]);
                }
            }
        }
    }

    public void Command_CasesMenu(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        string menuType = Config.MenuType.ToLower();
        if (menuType == "html")
        {
            CenterHtmlMenu menu = new CenterHtmlMenu(Localizer["Cases Menu title"], this);

            foreach (var caseItem in Config.Cases)
            {
                menu.AddMenuOption(Localizer["Case item", caseItem.Name, caseItem.Price], (client, option) =>
                {
                    ShowCaseOptionsMenu(player, caseItem);
                });
            }

            MenuManager.OpenCenterHtmlMenu(this, player, menu);
        }
        else
        {
            var menu = new ChatMenu(Localizer["Cases Menu title"]);

            foreach (var caseItem in Config.Cases)
            {
                menu.AddMenuOption(Localizer["Case item", caseItem.Name, caseItem.Price], (client, option) =>
                {
                    ShowCaseOptionsMenu(player, caseItem);
                });
            }

            MenuManager.OpenChatMenu(player, menu);
        }
    }


    private void ShowCaseOptionsMenu(CCSPlayerController player, CaseItem caseItem)
    {
        string menuType = Config.MenuType.ToLower();
        if (menuType == "html")
        {
            CenterHtmlMenu menu = new CenterHtmlMenu(Localizer["Case Options Menu title"], this);

            menu.AddMenuOption(Localizer["Buy Case"], (client, option) =>
            {
                if (playerLastBuyCaseCommandTimes.TryGetValue(player.SteamID.ToString(), out var lastCommandTime))
                {
                    var cooldownRemaining = (DateTime.Now - lastCommandTime).TotalSeconds;
                    if (cooldownRemaining < Config.BuyCasesCooldown)
                    {
                        var secondsRemaining = (int)(Config.BuyCasesCooldown - cooldownRemaining);
                        player.PrintToChat(Localizer["Prefix"] + Localizer["In cooldown", secondsRemaining]);
                        return;
                    }
                }

                playerLastBuyCaseCommandTimes[player.SteamID.ToString()] = DateTime.Now;
                StartCaseAnimation(player, caseItem);
            });

            menu.AddMenuOption(Localizer["View Case Content"], (client, option) =>
            {
                ShowCaseContent(player, caseItem);
            });

            MenuManager.OpenCenterHtmlMenu(this, player, menu);
        }
        else
        {
            var menu = new ChatMenu(Localizer["Case Options Menu title"]);

            menu.AddMenuOption(Localizer["Buy Case"], (client, option) =>
            {
                StartCaseAnimation(player, caseItem);
            });

            menu.AddMenuOption(Localizer["View Case Content"], (client, option) =>
            {
                ShowCaseContent(player, caseItem);
            });

            MenuManager.OpenChatMenu(player, menu);
        }
    }

    private void ShowCaseContent(CCSPlayerController player, CaseItem caseItem)
    {
        player.PrintToChat(Localizer["Case Content First Line"]);

        foreach (var reward in caseItem.Rewards)
        {
            string content = Localizer["Case Content List", reward.Description];
            if (Config.ShowRewardChances)
            {
                content += Localizer["Case Content Chances", reward.Chance];
            }
            player.PrintToChat(content);
        }

        player.PrintToChat(Localizer["Case Content Last Line"]);
    }

    private void StartCaseAnimation(CCSPlayerController player, CaseItem caseItem)
    {
        if (StoreApi == null) throw new Exception("StoreApi could not be located.");

        if (!string.IsNullOrEmpty(caseItem.Flag) && AdminManager.PlayerHasPermissions(player, caseItem.Flag))
        {
            player.PrintToChat(Localizer["Prefix"] + Localizer["No permission to buy this case"]);
            return;
        }

        player.ExecuteClientCommand($"play {Config.OpenCaseSound}");

        int playerCredits = StoreApi.GetPlayerCredits(player);

        if (playerCredits < caseItem.Price)
        {
            player.PrintToChat(Localizer["Prefix"] + Localizer["Not enough credits"]);
            return;
        }

        StoreApi.GivePlayerCredits(player, -caseItem.Price);

        var animation = new CaseAnimation(player, caseItem);

        activeAnimations[player.SteamID.ToString()] = animation;

        AddTimer(0.1f, () =>
        {
            UpdateCaseAnimation(animation);
        });
        MenuManager.CloseActiveMenu(player);
    }

    private void UpdateCaseAnimation(CaseAnimation animation)
    {
        if (animation.Timer >= Config.AnimationDuration)
        {
            EndCaseAnimation(animation);
            return;
        }

        animation.Timer += Config.AnimationInterval;

        var rewards = animation.CaseItem.Rewards;

        if (rewards.Count == 0)
        {
            animation.Player.PrintToCenter(Localizer["Prefix"] + Localizer["No rewards available in the case"]);
            EndCaseAnimation(animation);
            return;
        }

        animation.RewardIndex = (animation.RewardIndex + 1) % rewards.Count;
        string rewardDescription = rewards[animation.RewardIndex].Description;
        string animationText = GenerateCaseAnimationHtml(rewardDescription);

        if (Config.UseHtml)
        {
            animation.Player.PrintToCenterHtml(animationText);
        }
        else
        {
            animation.Player.PrintToCenter(Localizer["Rolling", rewardDescription]);
        }

        AddTimer(Config.AnimationInterval, () =>
        {
            animation.Player.ExecuteClientCommand($"play {Config.RollItemSound}");
            UpdateCaseAnimation(animation);
        });
    }

    private int GetRandomRewardIndex(List<CaseReward> rewards)
    {
        var randomValue = new Random().NextDouble() * 100;

        float cumulativeChance = 0f;

        for (int i = 0; i < rewards.Count; i++)
        {
            cumulativeChance += rewards[i].Chance;
            if (randomValue <= cumulativeChance)
            {
                return i;
            }
        }

        return rewards.Count - 1;
    }


    private string GenerateCaseAnimationHtml(string currentReward)
    {
        return Config.AnimationHtml
            .Replace("{reward}", currentReward);
    }

    private void EndCaseAnimation(CaseAnimation animation)
    {
        if (StoreApi == null) throw new Exception("StoreApi could not be located.");
        animation.Player.ExecuteClientCommand($"play {Config.WonItemSound}");
        
        activeAnimations.TryRemove(animation.Player.SteamID.ToString(), out _);

        var caseItem = animation.CaseItem;

        int finalRewardIndex = GetRandomRewardIndex(caseItem.Rewards);
        var reward = caseItem.Rewards[finalRewardIndex];

        var item = new Dictionary<string, string>
        {
            { "type", reward.Type },
            { "value", reward.Value }
        };

        if (reward.Type == "credits" && int.TryParse(reward.Value, out int credits))
        {
            StoreApi!.GivePlayerCredits(animation.Player, credits);
            ShowFinalReward(animation.Player, reward.Description);

            if (reward.PrintToChatAll)
            {
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["Player won chat all", animation.Player.PlayerName, reward.Description, caseItem.Name]);
            }
        }
        else if (reward.Type == "vip")
        {
            string command = reward.Value.Replace("{SteamID}", animation.Player.SteamID.ToString());
            NativeAPI.IssueServerCommand(command);

            if (reward.PrintToChatAll)
            {
                Server.PrintToChatAll(Localizer["Prefix"] + Localizer["Player won chat all", animation.Player.PlayerName, reward.Description, caseItem.Name]);
            }

            ShowFinalReward(animation.Player, reward.Description);
        }
        else
        {
            if (reward.Type != "credits")
            {
                item["price"] = "0";
                item["uniqueid"] = reward.Value;
                item["expiration"] = reward.Expiration.ToString();
            }

            bool playerHasItem = StoreApi.Item_PlayerHas(animation.Player, reward.Type, reward.Value, ignoreVip: false);
            if (!playerHasItem)
            {
                if (StoreApi.Item_Give(animation.Player, item))
                {
                    ShowFinalReward(animation.Player, reward.Description);

                    if (reward.PrintToChatAll)
                    {
                        Server.PrintToChatAll(Localizer["Prefix"] + Localizer["Player won chat all", animation.Player.PlayerName, reward.Description, caseItem.Name]);
                    }
                }
                else
                {
                    animation.Player.PrintToCenter(Localizer["Prefix"] + Localizer["Item could not be awarded"]);
                    animation.Player.PrintToChat(Localizer["Prefix"] + Localizer["Item could not be awarded"]);
                }
            }
            else
            {
                if (Config.ItemActionIfOwned == 1)
                {
                    int itemPrice = caseItem.Price;
                    int creditReward = itemPrice * Config.ItemActionCreditPercentage / 100;
                    StoreApi.GivePlayerCredits(animation.Player, creditReward);

                    animation.Player.PrintToChat(Localizer["Prefix"] + Localizer["You already have this item bonus", creditReward]);
                }
                else
                {
                    animation.Player.PrintToChat(Localizer["Prefix"] + Localizer["You already have this item"]);
                }
            }
        }
    }

    private void ShowFinalReward(CCSPlayerController player, string rewardDescription)
    {
        float displayDuration = 2.0f;
        float displayInterval = 0.1f;

        float elapsedTime = 0f;

        void DisplayReward()
        {
            if (elapsedTime >= displayDuration)
            {
                player.PrintToChat(Localizer["Prefix"] + Localizer["You won", rewardDescription]);
                return;
            }

            if (Config.UseHtml)
            {
                string finalText = Config.FinalHtml
                    .Replace("{reward}", rewardDescription);

                player.PrintToCenterHtml(finalText);

                elapsedTime += displayInterval;
                AddTimer(displayInterval, DisplayReward);
            }
            else
            {
                player.PrintToCenter(Localizer["You won center", rewardDescription]);
                player.PrintToChat(Localizer["Prefix"] + Localizer["You won", rewardDescription]);
            }
        }

        DisplayReward();
    }
}

public class CaseAnimation
{
    public CCSPlayerController Player { get; set; }
    public CaseItem CaseItem { get; set; }
    public float Timer { get; set; }
    public int RewardIndex { get; set; }

    public CaseAnimation(CCSPlayerController player, CaseItem caseItem)
    {
        Player = player;
        CaseItem = caseItem;
        Timer = 0f;
        RewardIndex = 0;
    }
}
