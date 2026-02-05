using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace GurtBot;

public class Gurt
{
	private static DiscordSocketClient _client;
	
	public static async Task Main()
	{
		var intents = new DiscordSocketConfig
		{
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
		};
		
		SettingsManager.LoadSettings();
		_client = new DiscordSocketClient(intents);
		_client.Log += Log;

		var token = File.ReadAllText("../../../token.txt");
		
		await _client.LoginAsync(TokenType.Bot, token);
		await _client.StartAsync();
		
		_client.MessageReceived += MessageReceived;
		_client.Ready += RegisterCommands;
		_client.Ready += () =>
		{
			_client.SetStatusAsync(UserStatus.Online);
			_client.SetCustomStatusAsync("yo!");
			_client.SlashCommandExecuted += SlashCommandHandler;
			Console.WriteLine("Gurt is online. Yo!");
			return Task.CompletedTask;
		};
		
		await Task.Delay(-1);
	}

	private static async Task RegisterCommands()
	{
		var downloadCommand = new SlashCommandBuilder()
			.WithName("downloadmessages")
			.WithDescription("Downloads all messages in the current channel. Only run this once...")
			.WithDefaultMemberPermissions(GuildPermission.Administrator);

		var settingsCommand = new SlashCommandBuilder()
			.WithName("settings")
			.WithDescription("Change bot settings")
			.WithDefaultMemberPermissions(GuildPermission.Administrator)
			.AddOption("maxwords", ApplicationCommandOptionType.Integer, "Maximum words in a generated message", isRequired: false)
			.AddOption("chance", ApplicationCommandOptionType.Integer, "1 in X chance to talk", isRequired: false);

		try
		{
			await _client.CreateGlobalApplicationCommandAsync(downloadCommand.Build());
			await _client.CreateGlobalApplicationCommandAsync(settingsCommand.Build());
		}
		catch(HttpException exception)
		{
			var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
			Console.WriteLine(json);
		}
	}

	private static async Task SlashCommandHandler(SocketSlashCommand command)
	{
		switch (command.Data.Name)
		{
			case "downloadmessages":
				await command.RespondAsync("Downloading...");
				var messages = await command.Channel.GetMessagesAsync(int.MaxValue).FlattenAsync();
				await LoggingManager.LogMessages(messages);
				await command.Channel.SendMessageAsync("Downloaded " + messages.Count() + " messages!");
				break;
			case "settings":
				int newMaxWords = SettingsManager.GetMaxWords();
				int newChance = SettingsManager.GetChanceToTalk();

				var maxWordsOption = command.Data.Options.FirstOrDefault(x => x.Name == "maxwords");
				var chanceOption = command.Data.Options.FirstOrDefault(x => x.Name == "chance");

				if (maxWordsOption != null) newMaxWords = Convert.ToInt32(maxWordsOption.Value);
				if (chanceOption != null) newChance = Convert.ToInt32(chanceOption.Value);
          
				await SettingsManager.SaveSettings(newMaxWords, newChance);
				await command.RespondAsync($"Settings updated! Max Words: {newMaxWords}, Chance: 1/{newChance}");
				break;
		}
	}
	
	private static async Task MessageReceived(SocketMessage message)
	{
		if (message.Author.IsBot) return;

		if (!LoggingManager.IsFileLocked(LoggingManager.Path))
		{
			await LoggingManager.LogMessage(message);
			if (LoggingManager.Random.Next(SettingsManager.GetChanceToTalk()) != 0) return;
			await LoggingManager.SendRandomMessageCombination(message);
		}
	}

	private static Task Log(LogMessage message)
	{
		Console.WriteLine(message);
		return Task.CompletedTask;
	}
}
