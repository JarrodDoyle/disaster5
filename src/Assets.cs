using System.Collections.Generic;
using Jurassic;
using Jurassic.Library;
using System.IO;
using System;
using OpenGL;

namespace Disaster 
{
    public class Assets {
        public static string basePath;

        public static Dictionary<string, ObjectInstance> scripts;
        public static List<string> currentlyLoadingScripts;
        public static Dictionary<string, Texture> textures;
        public static Dictionary<string, ObjModel> objModels;

        static ShaderProgram _defaultShader;
        public static ShaderProgram defaultShader {
            get {
                if (_defaultShader == null) {
                    var vertShader = File.ReadAllText("res/vert.glsl");
                    var fragShader = File.ReadAllText("res/frag.glsl");
                    _defaultShader = new ShaderProgram(vertShader, fragShader);
                }
                return _defaultShader;
            }
        }

        public static string LoadPath(string path)
        {
            var output = Path.Combine(basePath, path);
            if (!File.Exists(output)) {
                Console.WriteLine($"No File: {output}");
            }
            return output;
        }

        public static Texture Texture(string path)
        {
            if (textures == null) textures = new Dictionary<string, Texture>();
            if (!textures.ContainsKey(path)) {
                var texturePath = LoadPath(path);
                
                SDL2.SDL.SDL_Surface surface = System.Runtime.InteropServices.Marshal.PtrToStructure<SDL2.SDL.SDL_Surface>(SDL2.SDL_image.IMG_Load(texturePath));
                var texture = new Texture(surface.pixels, surface.w, surface.h);
                textures.Add(path, texture);
            }
            return textures[path];
        }

        public static ObjModel ObjModel(string path)
        {
            if (objModels == null) objModels = new Dictionary<string, ObjModel>();
            if (!objModels.ContainsKey(path))
            {
                var objModelPath = LoadPath(path);

                var objModel = Disaster.ObjModel.Parse(objModelPath);
                objModels.Add(path, objModel);
            }
            return objModels[path];
        }

        public static ObjectInstance Script(string path)
        {
            if (scripts == null) scripts = new Dictionary<string, ObjectInstance>();
            if (!scripts.ContainsKey(path)) {
                var scriptPath = LoadPath(path);

                if (currentlyLoadingScripts == null) currentlyLoadingScripts = new List<string>();

                if (currentlyLoadingScripts.Contains(scriptPath)) {
                    Console.WriteLine($"Circular dependency: {scriptPath}");
                    return null;
                }

                if (!File.Exists(scriptPath)) {
                    Console.WriteLine($"Cannot find script: {scriptPath}");
                    return null;
                }

                currentlyLoadingScripts.Add(scriptPath);

                var newEngine = new ScriptEngine();
                JS.LoadStandardFunctions(newEngine);
                newEngine.Execute(File.ReadAllText(scriptPath));
                scripts.Add(path, newEngine.Global);
                
                currentlyLoadingScripts.Remove(scriptPath);
            }

            return scripts[path];
        }

        public static void Dispose()
        {
            if (textures != null)
            {
                foreach (var t in textures.Values)
                {
                    t.Dispose();
                }
            }
        }
    }
}