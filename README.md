# Ashen Hollow

A mobile-first 3D fantasy MMORPG that runs in any web browser, phones included. Everything is in one file, `index.html` (three.js r128, loaded from a CDN).

**Play:** open `index.html`, or visit this repository's GitHub Pages site once it is turned on.

## What's in the game

- **Two ways to play.** Adventurers fight, clear dungeons and raids, and follow a 75-quest story. Artisans craft, gather, farm and trade. They level up through their professions, and monsters leave them alone unless they strike first. Anyone can practise any number of professions.
- **Six classes** (Warrior, Mage, Priest, Rogue, Ranger, Druid), each with evolution paths. All 66 spells have their own effects and sounds.
- **Real cities.** Seven cities, each a walled map of its own with a street grid lined with two- and three-storey buildings you can use: homes where neighbours ask for help each day, a temple, a town hall, a post office, and workshops for smithing, tailoring, cooking and brewing. Every door opens onto a room inside. Two houses in each city can be rented by the week (a bed, your bank chest, and you wake there after a fall). After dark, a lantern-lit night market opens in each city's square. Townsfolk walk the streets and lamps light up at night.
- **A living world.** Seven cities, open regions, five dungeons, the Ember Throne raid, world bosses, weekday festivals, and seasonal festivals (Harvest Fair, Lantern Nights for Ramadan with Eid gifts, Winter Feast).
- **Economy.**
  - The Merchants' Quarter: player stalls, with a hall per trade in every city, price floors and ceilings, and townsfolk shoppers.
  - The Auction House, for loot, rare drops, mount reins and gear.
  - Crafting orders and guild work orders.
- **Home and companions.** A homestead with crops, animals and comforts, plus pets, pet battles, mounts and mercenaries.
- **Progression.** Gear quality, enhancement +1 to +9, gem sockets, monster cards, stat points, town reputation, titles and achievements.
- **Online features** (when opened from the Claude artifact):
  - Other players live in the world, with a WoW-style player menu: inspect, trade, duel, whisper, mail, group, follow, friend, visit homestead.
  - Guilds with a hall, bank and perks.
  - The Dungeon Finder, arena seasons and ladders.
  - Cloud saves.

## Hosting on GitHub Pages

1. In the repository, go to **Settings → Pages**.
2. Under **Build and deployment**, choose **Deploy from a branch**, branch `main`, folder `/ (root)`, and save.
3. After a minute the game is live at `https://<your-user>.github.io/<repo-name>/`.

## Online play and saves

The multiplayer features use the Claude artifact runtime (shared database, live rooms and sign-in). When the game runs on GitHub Pages, or any other plain web host, it runs as a **single-player game** and saves each player's progress in their own browser. The multiplayer features need the Claude-hosted version, or a dedicated game server.

## Status

This is a prototype. Trading, the Auction House, guild banks, stalls and orders trust each player's browser. A real game server is needed before any public or app-store release.
