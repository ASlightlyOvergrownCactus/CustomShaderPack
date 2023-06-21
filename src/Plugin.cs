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
    public static readonly object mossSprite = new();
    static bool loaded = false;
    const int vertsPerColumn = 64;
    public static RoomSettings.RoomEffect.Type MossWater;

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
            loaded = true;
            var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assetbundles/liquidshaderpack"));
            self.Shaders["MossWater"] = FShader.CreateShader("MossWater", bundle.LoadAsset<Shader>("Assets/shaders 1.9.03/MossWater.shader"));
            MossWater = new(nameof(MossWater), true);
        }
    }

    private void Water_AddToContainer(On.Water.orig_AddToContainer orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig(self, sLeaser, rCam, newContatiner);

        if (self.room.roomSettings.GetEffect(MossWater) != null)
        {
            if (sLeaser.sprites.FirstOrDefault(x => x.data == mossSprite) is TriangleMesh mossMesh)
            {
                rCam.ReturnFContainer("Water").AddChild(mossMesh);
                mossMesh.MoveBehindOtherNode(sLeaser.sprites[1]);
            }
        }
    }

    private void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        
        orig(self, sLeaser, rCam);
        int index = 0;
        if (self.room.roomSettings.GetEffect(MossWater) != null)
        {
            index = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);

            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[self.pointsToRender * 2 * (vertsPerColumn - 1)];
            int triIndex = 0;
            for (int column = 0; column < self.pointsToRender; column++)
            {
                int firstVertex = column * vertsPerColumn;

                for (int row = 0; row < vertsPerColumn - 1; row++)
                {
                    int i = firstVertex + row;
                    tris[triIndex++] = new TriangleMesh.Triangle(i, i + 1, i + 1 + vertsPerColumn);
                    tris[triIndex++] = new TriangleMesh.Triangle(i, i + 1 + vertsPerColumn, i + vertsPerColumn);
                }
            }

            sLeaser.sprites[index] = new TriangleMesh("Futile_White", tris, true)
            {
                data = mossSprite,
                shader = self.room.game.rainWorld.Shaders["MossWater"]
            };

            self.AddToContainer(sLeaser, rCam, null);
        }
    }

    private void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (self.room.roomSettings.GetEffect(MossWater) != null)
        {
            if (sLeaser.sprites.FirstOrDefault(x => x.data == mossSprite) is TriangleMesh mossMesh)
            {
                WaterTriangleMesh waterMesh = sLeaser.sprites[0] as WaterTriangleMesh;
                int offset = self.PreviousSurfacePoint(camPos.x - 30f);

                // Calculate vertex positions and UVs
                for (int column = 0; column <= self.pointsToRender; column++)
                {
                    Vector2 waterFront = waterMesh.vertices[column * 2 + 0];
                    Vector2 waterBack = waterMesh.vertices[column * 2 + 1];

                    for (int row = 0; row < vertsPerColumn; row++)
                    {
                        float u = column + offset;
                        float v = row / (vertsPerColumn - 1f);
                        mossMesh.UVvertices[column * vertsPerColumn + row] = new Vector2(u, v);
                        mossMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
                    }
                }
            }
        }
    }
}

