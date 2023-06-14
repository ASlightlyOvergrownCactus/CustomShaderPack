using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using System.Security.Permissions;
using UnityEngine;
using RWCustom;
using System.Security;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Placek;

[BepInPlugin("liquid.shaders", "CustomLiquidShaders", "0.1")]
sealed class Plugin : BaseUnityPlugin
{
    public void OnEnable()
    {
        On.RainWorld.LoadResources += RainWorld_LoadResources;
        On.Water.InitiateSprites += Water_InitiateSprites;
    }

    private void RainWorld_LoadResources(On.RainWorld.orig_LoadResources orig, RainWorld self)
    {
        orig(self);
        if (!loaded)
        {
            var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assetbundles/liquidshaderpack"));
            self.Shaders["MossWater"] = FShader.CreateShader("MossWater", bundle.LoadAsset<Shader>("Assets/shaders 1.9.03/MossWater.shader"));
            loaded = true;

        }
    }

    static bool loaded = false;


    private void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);

        sLeaser.sprites[0].shader = self.room.game.rainWorld.Shaders["MossWater"];
    }
}

