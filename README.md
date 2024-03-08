# SlipStreamer.bot

Trigger a Streamer.bot action on in-game events!

## Requirements

- [Slipstream: Rogue Space (on Steam)](https://playslipstream.com)
- [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html)
- [Streamer.bot](https://streamer.bot)

## Game Installation

1) Launch Slipstream at least once.
2) Following the instructions [here](https://docs.bepinex.dev/articles/user_guide/installation/index.html), install BepInEx into Slipstream
3) Download the latest release from [here](https://github.com/MoSadie/SlipStreamer.bot/releases/latest) and place it in the BepInEx/plugins folder
4) Launch the game to generate the config file
5) Modify the config file in BepInEx/config to match your setup. (see the config file for more details)

## Streamer.bot Requirements

1) Follow the [Streamer.bot setup instructions](https://docs.streamer.bot/get-started/installation)
2) In the "Servers/Clients" tab, under HTTP Server check "Auto Start" and click "Start Server" (making note of the IP and port)
3) (Optional, but helpful) Click "Import" and paste the contents of [ImportSlipStreamerBot.txt](ImportSlipStreamerBot.txt) in the "Input String" text box and click "Import" This provices a set of premade actions for you to use. There is a dedicated action for each event type, as well as a few fun premade features such as the "!ss poll" chat command to poll the chat on a choice.

## Events

### GameLaunch

Sent when the game is launched.

Arguments:

- eventType: "GameLaunch"


### GameExit

Sent when the game is exited.

Arguments:

- eventType: "GameExit"

### JoinShip

Sent when a ship is joined.

Arguments:

- eventType: "JoinShip"

### StartFight

Sent when a fight is started.

Arguments:

- eventType: "StartFight"
- enemy: Name of the enemy ship.
- invaders: Types of invading slugs.
- intel: Available intel on the enemy ship.
- threatLevel: Enemy threat level.
- speedLevel: Enemy speed level.
- cargoLevel: Enemy cargo level.

### EndFight

Sent when a fight is ended.

Arguments:

- eventType: "EndFight"
- outcome: "NoneYet", "CaptainVictory", "CaptainDefeat", or "CaptainRetreat"

### NodeChange

Sent when the ship moves to a new node.

Arguments:

- eventType: "NodeChange"
- isBacktrack - "True" or "False" if a backtrack.
- scenarioKey - Unique identifier for the node's type scenario.
- visited: "True" or "False" if the node has been visited before.
- completed: "True" or "False" if the node has been completed before.
- captainVictory: "True" or "False"

### ChoiceAvailable

Sent when a choice is available.

Arguments:

- eventType: "ChoiceAvailable"
- isBacktrack - "True" or "False" if a backtrack.
- scenarioKey - Unique identifier for the node's type scenario.
- visited: "True" or "False" if the node has been visited before.
- completed: "True" or "False" if the node has been completed before.
- captainVictory: "True" or "False"
- scenarioName: Name of the encounter
- scenarioDescription: Description of the encounter
- proposition: The question posed by the encounter
- choice1: The first choice
- choice2: The second choice

### KnockedOut

Sent when you are knocked out.

Arguments:

- eventType: "KnockedOut"
- message: The message displayed when knocked out. Includes color information so will need parsing.

### RunStarted

Sent when a run is started.

Arguments:

- eventType: "RunStarted"
- campaign: Counts the number of campaigns this ship has been on. (Starts at 0)
- region: The region the ship is headed to.

### RunFailed

Sent when a run is failed for any reason.

Arguments:

- eventType: "RunFailed"

### RunSucceeded

Sent when a run is succeeded.

Arguments:

- eventType: "RunSucceeded"

### NextSector

Sent when the ship moves to a new sector.

Arguments:

- eventType: "NextSector"
- sectorIndex: The index of the new sector.
- sectorName: The name of the new sector.