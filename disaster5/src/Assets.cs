using System.Collections.Generic;
using Jurassic;
using Jurassic.Library;
using System.IO;
using System;
using Raylib_cs;

namespace Disaster
{
    public struct PixelBuffer
    {
        public int width;
        public int height;
        public Color32[] pixels;
        public PixelBuffer(Color32[] pixels, int width)
        {
            this.width = width;
            this.height = pixels.Length / width;
            this.pixels = pixels;
        }

        public static PixelBuffer missing = new PixelBuffer(new Color32[4] { new Color32(255, 0, 255), new Color32(255, 0, 255), new Color32(255, 0, 255), new Color32(255, 0, 255) }, 2);
    }

    public class Assets
    {
        public static string basePath;

        static List<string> missingAssetPaths;

        public static Dictionary<string, ObjectInstance> scripts;
        public static List<string> currentlyLoadingScripts;
        //public static Dictionary<string, Texture> textures;
        public static Dictionary<string, PixelBuffer> pixelBuffers;
        public static Dictionary<string, Model> models;
        public static Dictionary<string, Sound> audio;
        public static Dictionary<string, Music> music;
        public static Dictionary<string, string> texts;

        static bool assignedDefaultShader = false;
        static Shader _defaultShader;
        public static Shader defaultShader
        {
            get
            {
                if (!assignedDefaultShader)
                {
                    if (LoadPath("vert.glsl", out string vertShaderPath))
                    {
                        if (LoadPath("frag.glsl", out string fragShaderPath))
                        {
                            var vertShader = File.ReadAllText(vertShaderPath);
                            var fragShader = File.ReadAllText(fragShaderPath);
                            _defaultShader = Raylib.LoadShaderCode(vertShader, fragShader);
                            assignedDefaultShader = true;
                        }
                    }
                }
                return _defaultShader;
            }
        }

        public static bool LoadPath(string path, out string assetPath)
        {
            var output = Path.Combine(basePath, path);
            assetPath = output;
            if (!File.Exists(output))
            {
                if (missingAssetPaths == null) missingAssetPaths = new List<string>();
                if (!missingAssetPaths.Contains(output))
                {
                    missingAssetPaths.Add(output);
                    Console.WriteLine($"Can't find asset: {output}");
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void UnloadAll()
        {
            Dispose();
            if (scripts != null) scripts.Clear();
            //if (textures != null) textures.Clear();
            if (pixelBuffers != null) pixelBuffers.Clear();
            //if (objModels != null) objModels.Clear();
            if (audio != null) audio.Clear();
            if (music != null) music.Clear();
            if (texts != null) texts.Clear();
            GC.Collect();
        }

        public static void Unload(string path)
        {
            //string extension = Path.GetExtension(path).ToLower();
            //switch (extension)
            //{
            //    case ".txt":
            //        texts.Remove(path);
            //        break;
            //    case ".png":
            //        if (textures.ContainsKey(path))
            //        {
            //            textures[path].Dispose();
            //            textures.Remove(path);
            //        }
            //        pixelBuffers.Remove(path);
            //        break;
            //    case ".wav":
            //        audio.Remove(path);
            //        break;
            //    case ".ogg":
            //    case ".mp3":
            //        music.Remove(path);
            //        break;
            //    case ".obj":
            //        objModels.Remove(path);
            //        break;
            //}

        }

        public static void Preload(string path)
        {
            string extension = Path.GetExtension(path).ToLower();
            switch (extension)
            {
                case ".txt":
                    Text(path);
                    break;
                case ".png":
                    //Texture(path);
                    PixelBuffer(path);
                    break;
                case ".wav":
                    Audio(path);
                    break;
                case ".ogg":
                case ".mp3":
                    Music(path);
                    break;
                    //case ".obj":
                    //    ObjModel(path);
                    //    break;
            }
        }

        public static string[] GetAllPaths()
        {
            string[] output = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);
            int len = basePath.Length + 1;
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = output[i].Substring(len);
            }
            return output;
        }

        public static string Text(string path)
        {
            if (texts == null) texts = new Dictionary<string, string>();
            if (!texts.ContainsKey(path))
            {
                if (!LoadPath(path, out string textPath))
                {
                    return null;
                }

                string output = File.ReadAllText(textPath);
                texts.Add(path, output);
            }
            return texts[path];
        }

        //public static Texture Texture(string path)
        //{
        //    if (textures == null) textures = new Dictionary<string, Texture>();
        //    if (!textures.ContainsKey(path))
        //    {
        //        if (!LoadPath(path, out string texturePath))
        //        {
        //            // TODO: exception? return default "missing" texture?
        //            return null;
        //        }

        //        var imgPtr = SDL2.SDL_image.IMG_Load(texturePath);

        //        SDL2.SDL.SDL_Surface surface = System.Runtime.InteropServices.Marshal.PtrToStructure<SDL2.SDL.SDL_Surface>(imgPtr);
        //        var texture = new Texture(surface.pixels, surface.w, surface.h);
        //        textures.Add(path, texture);
        //    }
        //    return textures[path];
        //}

        public static PixelBuffer PixelBuffer(string path)
        {
            if (pixelBuffers == null) pixelBuffers = new Dictionary<string, PixelBuffer>();
            if (!pixelBuffers.ContainsKey(path))
            {
                if (!LoadPath(path, out string pixelBufferPath))
                {
                    return Disaster.PixelBuffer.missing;
                }

                var image = Raylib.LoadImage(pixelBufferPath);

                Color32[] pixels = new Color32[image.width * image.height];
                unsafe
                {
                    var colors = ((Color32*)image.data);
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = colors[i];
                    }
                }

                var pixelBuffer = new PixelBuffer(
                    pixels,
                    image.width
                );
                pixelBuffers.Add(path, pixelBuffer);
            }
            return pixelBuffers[path];
        }

        public static Model Model(string path)
        {
            if (models == null) models = new Dictionary<string, Model>();
            if (!models.ContainsKey(path))
            {
                if (!LoadPath(path, out string modelPath))
                {
                    Program.LoadingMessage($"No model, bud. {modelPath}");
                }
                Console.WriteLine($"loading: {modelPath}");
                var model = Raylib.LoadModel(modelPath);
                models.Add(path, model);
            }
            return models[path];
        }

        public static ObjectInstance Script(string path)
        {
            if (scripts == null) scripts = new Dictionary<string, ObjectInstance>();
            if (!scripts.ContainsKey(path))
            {
                var newScript = LoadScript(path);
                if (newScript != null) scripts.Add(path, newScript);
            }

            return scripts[path];
        }

        public static ObjectInstance LoadScript(string path)
        {
            if (!LoadPath(path, out string scriptPath))
            {
                return null;
            }

            if (currentlyLoadingScripts == null) currentlyLoadingScripts = new List<string>();

            if (currentlyLoadingScripts.Contains(scriptPath))
            {
                Console.WriteLine($"Circular dependency: {scriptPath}");
                return null;
            }

            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"Cannot find script: {scriptPath}");
                return null;
            }

            currentlyLoadingScripts.Add(scriptPath);

            var newEngine = new ScriptEngine();
            JS.LoadStandardFunctions(newEngine);
            newEngine.Execute(File.ReadAllText(scriptPath));
            
            currentlyLoadingScripts.Remove(scriptPath);

            return newEngine.Global;
        }

        public static Music Music(string path)
        {
            if (music == null) music = new Dictionary<string, Music>();
            if (!music.ContainsKey(path))
            {
                if (!LoadPath(path, out string audioPath))
                {
                    Program.LoadingMessage($"No music, bud. {audioPath}");
                }
                var newAudio = Raylib.LoadMusicStream(audioPath);
                music.Add(path, newAudio);
            }
            return music[path];
        }

        public static Sound Audio(string path)
        {
            if (audio == null) audio = new Dictionary<string, Sound>();
            if (!audio.ContainsKey(path))
            {
                if (!LoadPath(path, out string audioPath))
                {
                    Program.LoadingMessage($"No sound, bud. {audioPath}");
                }
                var newAudio = Raylib.LoadSound(audioPath);
                audio.Add(path, newAudio);
            }
            return audio[path];
        }

        public static void Dispose()
        {
            //if (textures != null)
            //{
            //    foreach (var t in textures.Values)
            //    {
            //        t.Dispose();
            //    }
            //}
        }
    }
}