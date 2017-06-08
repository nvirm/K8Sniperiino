# K8Sniperiino
![alt text](https://github.com/kitsun8/K8Sniperiino/blob/master/screenshots/sniperiino.PNG)

A bot for Discord that searches and announces Twitch streams with given community id to a discord channel.

# What it does
0. Reads settings from appsettings.json (see 'settings' in this repo)
1. Starts a timer
2. When timer is up, runs a query against Twitch API
3. Checks JSON results of streams
4. Checks if stream has been announced before
5. Announces streams at discord channel

# Details
Automatic Twitch stream announcer for communities.

Bot is running on Discore 2.3.0 (https://github.com/BundledSticksInkorperated/Discore)

Project is running on .NET Core 1.1 / .NET Standard 1.6
