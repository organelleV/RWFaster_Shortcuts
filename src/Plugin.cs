using BepInEx;
using System;
using System.Security.Permissions;
using UnityEngine;


//Credit:
//https://github.com/Dual-Iron/catnap (MainLoopProcess_RawUpdate) method to change tick rate
//https://github.com/Dual-Iron/TestMod.git example project used as base
//https://github.com/SchuhBaum/SBCameraScroll/blob/Rain-World-v1.9/SourceCode/MainModOptions.cs options script
//https://github.com/forthbridge/faster-gates/blob/main/src/Options.cs options script

// figure out how to upload on steam workshop, https://github.com/forthbridge/fast-roll-button
// create a github (catnap license), add gh links to workshop,mod json
//clean up code, make a banner
// make a video and screenshots
// release!

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace FasterDoors;

[BepInPlugin("com.Uten."+ MOD_ID, MOD_NAME, VERSION)]
sealed class Plugin : BaseUnityPlugin
{
    public const string MOD_NAME = "Faster Shortcuts";
    public const string MOD_ID = "FasterShortcuts";
    public const string AUTHOR = "Uten";
    public const string VERSION = "1.0.1";

    bool init;

    public void OnEnable()
    {
        // Add hooks here
        On.RainWorld.OnModsInit += OnModsInit;
        On.Player.Update += PlayerUpdateHook;
        On.MainLoopProcess.RawUpdate += MainLoopProcess_RawUpdate;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        if (init) return;

        init = true;

        // Initialize assets, your mod config, and anything that uses RainWorld here
        Logger.LogDebug("Faster Shortcuts");

        MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Options.instance);
        Logger.LogDebug("registered: Options.instance");
    }

    bool in_door = false;
    int last_lowerBodyFramesOnGround;
    int last_lowerBodyFramesOffGround;

    void PlayerUpdateHook(On.Player.orig_Update orig, Player self, bool eu)
    {
        /* This method will be subscribed to Player.Update. */
        orig(self, eu);
        /*Logger.LogDebug("PlayerUpdateHook:animationvalue: " + self.animation.value);
        Logger.LogDebug("PlayerUpdateHook:animationframe: " + self.animationFrame);
        Logger.LogDebug("PlayerUpdateHook:standing: " + self.standing);
        Logger.LogDebug("PlayerUpdateHook:onground: " + self.lowerBodyFramesOnGround);
        Logger.LogDebug("PlayerUpdateHook:offground: " + self.lowerBodyFramesOffGround);*/
        
        // if on and off ground counters are not being updated, slugcat
        // is room transitioning or dead or in another state
        if (last_lowerBodyFramesOnGround == self.lowerBodyFramesOnGround 
        && last_lowerBodyFramesOffGround == self.lowerBodyFramesOffGround
        && !self.dead)
        {
            in_door = true;
        }
        else
        {
            in_door = false;
        }

        last_lowerBodyFramesOnGround = self.lowerBodyFramesOnGround;
        last_lowerBodyFramesOffGround = self.lowerBodyFramesOffGround;
    }

    private void MainLoopProcess_RawUpdate(On.MainLoopProcess.orig_RawUpdate orig, MainLoopProcess self, float dt)
    {
        try
        {
            orig(self, dt);

            if (self is not RainWorldGame game || !game.IsStorySession || game.pauseMenu != null || !game.processActive)
            {
                return;
            }


            // If it takes more than one "tick duration" to finish an update, the game is lagging. Normally, the game would tick multiple times to make up for this,
            // but we don't want to make the lag worse, so we simply pretend only one "tick duration" has passed.
            if (dt > 1 / 40f)
                dt = 1 / 40f;

            // run game up to N× faster while room transitioning
            var gameSpeed = Options.TransitionSpeed.Value;

            if (in_door == true)
            {
                self.myTimeStacker += dt * gameSpeed;
                while (self.myTimeStacker > 1)
                {
                    self.myTimeStacker -= 1;
                    //self.manager.rainWorld.rewiredInputManager.SendMessage("Update");
                    self.Update();
                }
                //self.GrafUpdate(self.myTimeStacker);
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Main loop process {self.GetType()} threw an uncaught exception.\n" + e);
        }
    }


}
