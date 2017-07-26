# K8Sniperiino
![alt text](https://github.com/kitsun8/K8Sniperiino/blob/master/screenshots/sniperiino.PNG)

A bot for Discord that searches and announces Twitch streams with given community id to a discord channel.

# What it does
0. Reads settings from appsettings.json (see 'settings' in this repo)
1. Starts a timer
2. When timer is up, runs a query against Twitch API
3. Checks JSON results of streams
4. Checks if stream has been announced before
4a. If yes, edits the announce message with new information
5. Announces streams at discord channel
6. If stream has ended, deletes the announce message.
7. If stream has been online for over 2 hours, delete initial announce message and reannounce (bump-action)

# Details
Automatic Twitch stream announcer for communities.

Bot is running on Discore 2.4.0 (https://github.com/BundledSticksInkorperated/Discore)

Project is running on .NET Core 1.1 / .NET Standard 1.6

Twitch API is V5 API: https://dev.twitch.tv/docs/v5/reference/channels/

Language support added for english, default/original language was finnish. Can be changed from appsettings.json
