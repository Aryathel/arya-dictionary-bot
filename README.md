# Arya's Dictionary Bot

A simple [Discord.NET](https://docs.discordnet.dev/) bot for fetching dictionary definitions and posting the word of the day.

What makes this project a bit more interesting is that it is built as a Windows Service with a configuration UI, so that once it is set up and running it will always run when your computer runs.

## Installation

1. Download the installer for the most recent [release](https://github.com/Aryathel/arya-dictionary-bot/releases).
2. Run the installer.
3. Open the `AryaDev - DictionaryBot` application that was installed.
4. Create a bot token and enter it.
5. Select a server to connect to.
6. Use the commands:
  - `/define <word>`: Look up the definition of a word.
  - `/set-wotd-channel <channel>`: Set the channel that the word of the day is posted in. It will be posted there as soon as it becomes available from [Merriam-Webster](https://www.merriam-webster.com/word-of-the-day).
  - `/check-wotd-config`: Shows the user what channel the word of the day is being posted in.
