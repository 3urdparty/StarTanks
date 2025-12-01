using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

public class TextureAtlas 
{
    private Dictionary<string, TextureRegion> _regions;
    private Dictionary<string, Animation> _animations;
    private Dictionary<string, Texture2D> _textures;
    
    public TextureAtlas()
    {
        _regions = new Dictionary<string, TextureRegion>();
        _animations = new Dictionary<string, Animation>();
        _textures = new Dictionary<string, Texture2D>();
    }
    
    public void AddTexture(string name, Texture2D texture)
    {
        _textures.Add(name, texture);
    }
    
    public Texture2D GetTexture(string name)
    {
        return _textures[name];
    }
    
    public void AddRegion(string name, string textureName, int x, int y, int width, int height)
    {
        Texture2D texture = _textures[textureName];
        TextureRegion region = new TextureRegion(texture, x, y, width, height);
        _regions.Add(name, region);
    }
    
    public TextureRegion GetRegion(string name)
    {
        return _regions[name];
    }
    
    public bool RemoveRegion(string name)
    {
        return _regions.Remove(name);
    }
    
    public void AddAnimation(string animationName, Animation animation)
    {
        _animations.Add(animationName, animation);
    }
    
    public Animation GetAnimation(string animationName)
    {
        return _animations[animationName];
    }
    
    public bool RemoveAnimation(string animationName)
    {
        return _animations.Remove(animationName);
    }
    
    public void Clear()
    {
        _regions.Clear();
        _animations.Clear();
        _textures.Clear();
    }
    
    public static TextureAtlas FromFile(ContentManager content, string filename)
    {
        TextureAtlas atlas = new TextureAtlas();
        string filePath = Path.Combine(content.RootDirectory, filename);
        
        using (Stream stream = TitleContainer.OpenStream(filePath))
        {
            using (XmlReader reader = XmlReader.Create(stream))
            {
                XDocument doc = XDocument.Load(reader);
                XElement root = doc.Root;
                
                // Load regions grouped by texture
                var regionsBlocks = root.Elements("Regions");
                if (regionsBlocks != null)
                {
                    foreach (var regionsBlock in regionsBlocks)
                    {
                        // Get the texture path for this Regions block
                        string texturePath = regionsBlock.Element("Texture")?.Value;
                        string textureName = regionsBlock.Element("Texture")?.Attribute("name")?.Value;
                        
                        if (!string.IsNullOrEmpty(texturePath))
                        {
                            // If no name specified, use the path as the name
                            if (string.IsNullOrEmpty(textureName))
                            {
                                textureName = texturePath;
                            }
                            
                            // Load the texture if not already loaded
                            if (!atlas._textures.ContainsKey(textureName))
                            {
                                Texture2D texture = content.Load<Texture2D>(texturePath);
                                atlas.AddTexture(textureName, texture);
                            }
                            
                            // Load all regions in this block
                            var regions = regionsBlock.Elements("Region");
                            if (regions != null)
                            {
                                foreach (var region in regions)
                                {
                                    string name = region.Attribute("name")?.Value;
                                    int x = int.Parse(region.Attribute("x")?.Value ?? "0");
                                    int y = int.Parse(region.Attribute("y")?.Value ?? "0");
                                    int width = int.Parse(region.Attribute("width")?.Value ?? "0");
                                    int height = int.Parse(region.Attribute("height")?.Value ?? "0");
                                    
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        atlas.AddRegion(name, textureName, x, y, width, height);
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Load animations
                var animationsElement = root.Element("Animations");
                if (animationsElement != null)
                {
                    var animationElements = animationsElement.Elements("Animation");
                    if (animationElements != null)
                    {
                        foreach (var animationElement in animationElements)
                        {
                            string name = animationElement.Attribute("name")?.Value;
                            float delayInMilliseconds = float.Parse(animationElement.Attribute("delay")?.Value ?? "0");
                            TimeSpan delay = TimeSpan.FromMilliseconds(delayInMilliseconds);
                            List<TextureRegion> frames = new List<TextureRegion>();
                            
                            var frameElements = animationElement.Elements("Frame");
                            if (frameElements != null)
                            {
                                foreach (var frameElement in frameElements)
                                {
                                    string regionName = frameElement.Attribute("region").Value;
                                    TextureRegion region = atlas.GetRegion(regionName);
                                    frames.Add(region);
                                }
                            }
                            
                            Animation animation = new Animation(frames, delay);
                            atlas.AddAnimation(name, animation);
                        }
                    }
                }
                
                return atlas;
            }
        }
    }
    
    public Sprite CreateSprite(string regionName)
    {
        TextureRegion region = GetRegion(regionName);
        return new Sprite(region);
    }
    
    public AnimatedSprite CreateAnimatedSprite(string animationName)
    {
        Animation animation = GetAnimation(animationName);
        return new AnimatedSprite(animation);
    }
}
