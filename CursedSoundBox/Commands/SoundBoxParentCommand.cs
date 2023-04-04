using System;
using CommandSystem;

namespace CursedSoundBox.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SoundBoxParentCommand : ParentCommand
{
    public SoundBoxParentCommand()
    {
        LoadGeneratedCommands();
    }
    
    public override void LoadGeneratedCommands()
    {
        RegisterCommand(new SoundBoxPlayCommand());
        RegisterCommand(new SoundBoxLoopCommand());
        RegisterCommand(new SoundBoxRestartCommand());
        RegisterCommand(new SoundBoxStopCommand());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "Subcommands: play, stop, loop, restart.";
        return false;
    }

    public override string Command { get; } = "soundbox";
    public override string[] Aliases { get; } = Array.Empty<string>();
    public override string Description { get; } = "Grants you access to SoundBox commands.";
}