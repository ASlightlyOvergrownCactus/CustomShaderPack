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
using System.Reflection;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace CustomShaderPack;

[BepInPlugin("custom.shaders", "CustomShaderPack", "0.1")]
sealed class Plugin : BaseUnityPlugin
{
    public static readonly object mossSprite = new object();
    static bool loaded = false;

    public void OnEnable()
    {
        On.RainWorld.LoadResources += RainWorld_LoadResources;
        On.Water.InitiateSprites += Water_InitiateSprites;
        On.Water.DrawSprites += Water_DrawSprites;
        On.Water.AddToContainer += Water_AddToContainer;
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

    private void Water_AddToContainer(On.Water.orig_AddToContainer orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig(self, sLeaser, rCam, newContatiner);
        int index = -1;
        bool found = false;
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            if (sLeaser.sprites[i].data == mossSprite)
            {
                index = i;
                found = true;
                break;
            }
        }
        if (found && index > -1)
        {
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[index]);
            sLeaser.sprites[index].MoveBehindOtherNode(sLeaser.sprites[1]);
        }

        
    }

    private void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);

        int index = sLeaser.sprites.Length;
        Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
        Debug.Log(self.pointsToRender);
        int multiplyFactor = 8;
        TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[self.pointsToRender * 2  * multiplyFactor];
        for (int i = 0; i < self.pointsToRender; i++)
        {
            int num = i * 2 * multiplyFactor;

            for (int j = 0; j < multiplyFactor; j++)
            {
                tris[num + j * 2] = new TriangleMesh.Triangle(num + j, num + 1 + j, num + multiplyFactor + j);
                tris[num + j * 2 + 1] = new TriangleMesh.Triangle(num + 1 + j, num + multiplyFactor + j, num + 1 + multiplyFactor + j);
            } 
        }

        sLeaser.sprites[index] = new TriangleMesh("Futile_White", tris, true)
        {
            data = mossSprite,
            shader = self.room.game.rainWorld.Shaders["MossWater"]
        };

        self.AddToContainer(sLeaser, rCam, null);

    }

    private void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        int index = -1;
        Vector2 waterFront = Vector2.zero;
        Vector2 waterBack = Vector2.zero;

        foreach (var sprite in sLeaser.sprites)
        {
            index++;
            if (sprite?.data != mossSprite) continue;
            // "sprite" now contains the moss sprite
            break;
        }
        int offset = self.PreviousSurfacePoint(camPos.x - 30f) * 2;
        for (int i = 0; i < (sLeaser.sprites[index] as TriangleMesh).vertices.Length; i++)
        {
            //(sLeaser.sprites[index] as TriangleMesh).UVvertices[i] = new Vector2((i + offset) / 2, i % 2);
            (sLeaser.sprites[index] as TriangleMesh).UVvertices[i] = new Vector2(i / 8, i % 8 / 7f);
        }
        
        for (int i = 0; i < (sLeaser.sprites[index] as TriangleMesh).vertices.Length; i++)
        {
                waterFront = (sLeaser.sprites[0] as WaterTriangleMesh).vertices[(i / 8) * 2];
                waterBack = (sLeaser.sprites[0] as WaterTriangleMesh).vertices[(i / 8) * 2 + 1];

            (sLeaser.sprites[index] as TriangleMesh).MoveVertice(i, Vector2.Lerp(waterFront, waterBack, i % 8 / 7f));
        }
        
        (sLeaser.sprites[index] as TriangleMesh).Refresh();

    }





}

