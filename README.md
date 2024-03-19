# YBOT

## YBot.Command.Abstracts
Sharing definition between YBOT and YBot.Command.Generator

## YBot.Command.Generator
Generate the code for
1. Add command server into DI container
2. Handle command distribute and priority

## YBot.Tests
Unit test for dice parser

## YBot

### SignalRService
Start the SignalR client to listen chat message

### SingalRClient
Handle comm to SignalR server, handle message

### DiceParser
Make Dice command string to Expression like "1d3 + 1d6 > 5"

### Commands
#### Dice
Dice tool for TRPG

#### Image
Search image using nature language, translate it to image tags via GPT
