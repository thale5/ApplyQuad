﻿using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;


namespace TerraformTool
{
    public class Mod : IUserMod
    {
        public string Description
        {
            get { return "Allows terraforming in game."; }
        }

        public string Name
        {
            get { return "Terraform Tool"; }
        }
    }

    public class LoadingExtension : LoadingExtensionBase
    {

        public InGameTerrainTool buildTool;
        public UITextureAtlas terraform_atlas;

        private bool registered = false;

        public void LoadResources()
        {
            string[] spriteNames = {
                                           "TerrainLevel", 
                                           "TerrainLevelDisabled", 
                                           "TerrainLevelFocused", 
                                           "TerrainLevelHovered", 
                                           "TerrainLevelPressed", 
                                           "TerrainShift", 
                                           "TerrainShiftDisabled", 
                                           "TerrainShiftFocused", 
                                           "TerrainShiftHovered", 
                                           "TerrainShiftPressed", 
                                           "TerrainSlope", 
                                           "TerrainSlopeDisabled", 
                                           "TerrainSlopeFocused", 
                                           "TerrainSlopeHovered", 
                                           "TerrainSlopePressed", 
                                           "TerrainSoften", 
                                           "TerrainSoftenDisabled", 
                                           "TerrainSoftenFocused", 
                                           "TerrainSoftenHovered", 
                                           "TerrainSoftenPressed", 
                                           "ToolbarIconTerrain", 
                                           "ToolbarIconTerrainDisabled", 
                                           "ToolbarIconTerrainFocused", 
                                           "ToolbarIconTerrainHovered", 
                                           "ToolbarIconTerrainPressed", 
                                           "ToolbarIconGroup6Focused", 
                                           "ToolbarIconGroup6Hovered", 
                                           "ToolbarIconGroup6Pressed", 
                                           "ResourceSand",
                                           "ResourceSandDisabled", 
                                           "ResourceSandFocused", 
                                           "ResourceSandHovered", 
                                           "ResourceSandPressed",
                                           "TerrainDitch",
                                           "TerrainDitchFocused",
                                           "TerrainDitchPressed",
                                       };

            terraform_atlas = CreateTextureAtlas("TerraformUI", UIView.GetAView().defaultAtlas.material, spriteNames, "TerraformTool.icon.");

        }
        UITextureAtlas CreateTextureAtlas(string atlasName, Material baseMaterial, string[] spriteNames, string assemblyPath)
        {
            var size = 1024;
            Texture2D atlasTex = new Texture2D(size, size, TextureFormat.ARGB32, false);

            Texture2D[] textures = new Texture2D[spriteNames.Length];
            Rect[] rects = new Rect[spriteNames.Length];

            for(int i = 0; i < spriteNames.Length; i++)
            {
                textures[i] = loadTextureFromAssembly(assemblyPath + spriteNames[i] + ".png", false);
            }

            rects = atlasTex.PackTextures(textures, 2, size);


            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();

            // Setup atlas
            Material material = Material.Instantiate(baseMaterial);
            material.mainTexture = atlasTex;
            atlas.material = material;
            atlas.name = atlasName;
            
            // Add SpriteInfo
            for (int i = 0; i < spriteNames.Length; i++)
            {
                var spriteInfo = new UITextureAtlas.SpriteInfo()
                {
                    name = spriteNames[i],
                    texture = atlasTex,
                    region = rects[i]
                };
                atlas.AddSprite(spriteInfo);
            }
            return atlas;
        }
        Texture2D loadTextureFromAssembly(string path, bool readOnly = true)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.IO.Stream textureStream = assembly.GetManifestResourceStream(path);

            byte[] buf = new byte[textureStream.Length];  //declare arraysize
            textureStream.Read(buf, 0, buf.Length); // read from stream to byte array
            Texture2D tex = new Texture2D(2,2, TextureFormat.ARGB32, false);
            tex.LoadImage(buf);
            tex.Apply(false, readOnly);
            return tex;
        }
        UITextureAtlas CreateTextureAtlas(string textureFile, string atlasName, Material baseMaterial, int spriteWidth, int spriteHeight, string[] spriteNames)
        {
            Texture2D tex = new Texture2D(spriteWidth * spriteNames.Length, spriteHeight, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;

            { // LoadTexture
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.IO.Stream textureStream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + textureFile);

                byte[] buf = new byte[textureStream.Length];  //declare arraysize
                textureStream.Read(buf, 0, buf.Length); // read from stream to byte array

                tex.LoadImage(buf);

                tex.Apply(true, true);
            }

            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();

            { // Setup atlas
                Material material = (Material)Material.Instantiate(baseMaterial);
                material.mainTexture = tex;

                atlas.material = material;
                atlas.name = atlasName;
            }

            // Add sprites
            for (int i = 0; i < spriteNames.Length; ++i)
            {
                float uw = 1.0f / spriteNames.Length;

                var spriteInfo = new UITextureAtlas.SpriteInfo()
                {
                    name = spriteNames[i],
                    texture = tex,
                    region = new Rect(i * uw, 0, uw, 1),
                };

                atlas.AddSprite(spriteInfo);
            }
            return atlas;
        }
        
        public override void OnLevelLoaded(LoadMode mode)
        {
            if (!(mode == LoadMode.LoadGame || mode == LoadMode.NewGame))
            {
                return;
            }
            try
            {
                LoadResources();
                if (buildTool == null)
                {
                    GameObject gameController = GameObject.FindWithTag("GameController");
                    buildTool = gameController.AddComponent<InGameTerrainTool>();
                    Texture2D tex = loadTextureFromAssembly("TerraformTool.builtin_brush_4.png", false);
                    buildTool.m_atlas = terraform_atlas;
                    buildTool.CreateButtons();
                    buildTool.m_brush = tex;
                    buildTool.m_mode = InGameTerrainTool.Mode.Point;
                    buildTool.enabled = false;
                    GameObject.FindObjectOfType<ToolController>().Tools[0].enabled = true;

                    if (!registered)
                    {
                        registered = true;
                        TerrainManager.RegisterTerrainManager(new MyManager());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }
}
