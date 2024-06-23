## v1.2.0: Captain Config

### Additions
- Added a new configuration option to require being the Captain of the ship in order for an event to be sent to Streamer.bot. There is an option to set the default behavior and then you can override it on a per-event basis. More details in the config file after launching once.
    - Note this does not work with the following events: "GameLaunch", "GameExit", "JoinShip" (These will always be sent since captain data is not available when these are triggered.)

## v1.1.0: Crew Events

### Additions
- Three new crew-based events: [CrewmateCreated](https://github.com/MoSadie/SlipStreamer.bot?tab=readme-ov-file#crewmatecreated), [CrewmateRemoved](https://github.com/MoSadie/SlipStreamer.bot?tab=readme-ov-file#crewmateremoved), and [CrewmateSwapped](https://github.com/MoSadie/SlipStreamer.bot?tab=readme-ov-file#crewmateswapped)
- Configurable cooldowns for each event. Any events that hit this cooldown will be skipped. More details in the config file after launching once.

### Streamer.bot Action Additions
- Added actions to handle the new events.
- Added automatic predictions on run start/end.
- Added crew tracking system. Creates non-persisted global variables showing how many of each archetype are currently on the ship.

If you encounter any issues, please report them on the [Issues](https://github.com/MoSadie/SlipStreamer.bot/issues) page!

**Full Changelog**: https://github.com/MoSadie/SlipStreamer.bot/compare/v1.0.1...v1.1.0

## v1.0.1: Stability

Added a little more robust error handling. Now if anything crashes in the mod my code should handle it and the game should ignore it.

## v1.0.0: Initial Release!

Yay! It's finally here!

I'm working on a "How to install" video right now, will have that linked in the readme soon. In the meantime the readme has written instructions.

Please report any issues you find [here](https://github.com/MoSadie/SlipStreamer.bot/issues)!