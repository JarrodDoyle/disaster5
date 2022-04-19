using Jurassic;
using Jurassic.Library;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DisasterAPI
{
    [ClassDescription("All your favourite ways to draw stuff to the screen!")]
    public class Draw : ObjectInstance {

        public Draw(ScriptEngine engine) : base(engine) {
            this.PopulateFunctions();
        }

        [JSFunction(Name = "loadFont")]
        [FunctionDescription("Load a font for the software renderer. All future Draw.text calls will use the specified font.")]
        [ArgumentDescription("fontPath", "Path to the font texture. Fonts are 2-color images where pixels with a red value above zero are considered filled.")]
        public static void LoadFont(string fontPath) {
            if (!Disaster.Assets.LoadPath(fontPath, out string fontAssetPath))
            {
                return;
            }
            Disaster.SoftwareCanvas.LoadFont(fontAssetPath);
        }

        [JSFunction(Name = "loadFontTTF")]
        [FunctionDescription("Load a TTF font. All future Draw.textTTF calls will use the specified font.")]
        [ArgumentDescription("fontPath", "Path to the font file.")]
        public static void LoadFontTTF(string fontPath) {
            if (!Disaster.Assets.LoadPath(fontPath, out string fontAssetPath))
            {
                return;
            }
            Disaster.Assets.Font(fontPath); // Loads the font if it isn't already loaded
            Disaster.Assets.currentFont = fontPath;
        }

        [JSFunction(Name = "clear")]
        [FunctionDescription("Clear the 2D canvas.")]
        public static void Clear() {
            Disaster.SoftwareCanvas.Clear();
            // TODO: This also clears the 3D canvas
            if (!Disaster.SoftwareCanvas.inBuffer)
            {
                Disaster.ShapeRenderer.EnqueueRender(
                    () => {
                        Raylib_cs.Raylib.ClearBackground(Raylib_cs.Color.BLACK);
                    }
                );
                Disaster.NativeResRenderer.EnqueueRender(
                    () => {
                        Raylib_cs.Raylib.ClearBackground(new Disaster.Color32(0, 0, 0, 0));
                    }
                );
            }
        }

        [JSProperty(Name = "fontHeight")] 
        [PropertyDescription("Height, in pixels, of the currently loaded font.")]
        public static int fontHeight { get { return Disaster.SoftwareCanvas.fontHeight; } }
        [JSProperty(Name = "fontWidth")]
        [PropertyDescription("Width, in pixels, of the currently loaded font.")]
        public static int fontWidth { get { return Disaster.SoftwareCanvas.fontWidth; } }
        [JSProperty(Name = "screenWidth")]
        [PropertyDescription("Width, in pixels, of the screen resolution.")] 
        public static int screenWidth { get { return Disaster.ScreenController.screenWidth; } }
        [JSProperty(Name = "screenHeight")]
        [PropertyDescription("Height, in pixels, of the screen resolution.")]
        public static int screenHeight { get { return Disaster.ScreenController.screenHeight; } }

        [JSFunction(Name = "offset")]
        [FunctionDescription("Set a global offset for 2D rendering.")]
        [ArgumentDescription("x", "Pixels in the x axis to offset by")]
        [ArgumentDescription("y", "Pixels in the y axis to offset by")]
        public static void Offset(int x, int y)
        {
            Disaster.SoftwareCanvas.offset.x = x;
            Disaster.SoftwareCanvas.offset.y = y;
        }


        [JSFunction(Name = "rect")]
        [FunctionDescription("Draw a rectangle")]
        [ArgumentDescription("x", "x position of the rectangle")]
        [ArgumentDescription("y", "y position of the rectangle")]
        [ArgumentDescription("width", "width of the rectangle")]
        [ArgumentDescription("height", "height of the rectangle")]
        [ArgumentDescription("color", "Rectangle color", "{r, g, b, a}")]
        [ArgumentDescription("filled", "(optional) Draw a filled rect (true) or an outline (false, default)")]
        public static void Rect(int x, int y, int width, int height, ObjectInstance color, bool filled = false)
        {
            var col = Disaster.TypeInterface.Color32(color);
            if (Disaster.SoftwareCanvas.inBuffer)
            {
                if (filled)
                    Disaster.SoftwareCanvas.FillRect(x, y, width, height, Disaster.TypeInterface.Color32(color));
                else
                    Disaster.SoftwareCanvas.DrawRect(x, y, width, height, Disaster.TypeInterface.Color32(color));
            } else 
            {
                x += Disaster.SoftwareCanvas.offset.x;
                y += Disaster.SoftwareCanvas.offset.y;
                Disaster.ShapeRenderer.EnqueueRender(
                    () => {
                        if (filled)
                            Raylib_cs.Raylib.DrawRectangle(x, y, width, height, col);
                        else
                            Raylib_cs.Raylib.DrawRectangleLines(x, y, width, height, col);
                    }
                );
            }            
        }

        [JSFunction(Name = "triangle")]
        [FunctionDescription("Draw a triangle")]
        [ArgumentDescription("x1", "x position of the first point")]
        [ArgumentDescription("y1", "y position of the first point")]
        [ArgumentDescription("x2", "x position of the second point")]
        [ArgumentDescription("y2", "y position of the second point")]
        [ArgumentDescription("x3", "x position of the third point")]
        [ArgumentDescription("y3", "y position of the third point")]
        [ArgumentDescription("color", "color for the triangle", "{r, g, b, a}")]
        [ArgumentDescription("filled", "(optional) Draw a filled triangle (true) or an outline (false, default)")]
        public static void Triangle(int x1, int y1, int x2, int y2, int x3, int y3, ObjectInstance color, bool filled = false)
        {
            var col = Disaster.TypeInterface.Color32(color);
            if (Disaster.SoftwareCanvas.inBuffer)
            {
                if (filled)
                    Disaster.SoftwareCanvas.Triangle(x1, y1, x2, y2, x3, y3, col);
                else
                {
                    Disaster.SoftwareCanvas.Line(x1, y1, x2, y2, col);
                    Disaster.SoftwareCanvas.Line(x3, y3, x2, y2, col);
                    Disaster.SoftwareCanvas.Line(x1, y1, x3, y3, col);
                }
            } else 
            {
                x1 += Disaster.SoftwareCanvas.offset.x;
                y1 += Disaster.SoftwareCanvas.offset.y;
                x2 += Disaster.SoftwareCanvas.offset.x;
                y2 += Disaster.SoftwareCanvas.offset.y;
                x3 += Disaster.SoftwareCanvas.offset.x;
                y3 += Disaster.SoftwareCanvas.offset.y;

                // Put these points in counter-clockwise order for raylib
                var a = new Vector2(x1, y1);
                var b = new Vector2(x2, y2);
                var c = new Vector2(x3, y3);
                var ab = b - a;
                var ac = c - a;
                var crossz = ab.X * ac.Y - ab.Y * ac.X;

                Vector2[] points;
                if (crossz < 0)
                    points = new Vector2[]{a, b, c};
                else
                    points = new Vector2[]{a, c, b};

                Disaster.ShapeRenderer.EnqueueRender(
                    () => {
                        if (filled)
                            Raylib_cs.Raylib.DrawTriangle(points[0], points[1], points[2], col);
                        else
                            Raylib_cs.Raylib.DrawTriangleLines(points[0], points[1], points[2], col);
                    }
                );
            }
        }

        [JSFunction(Name = "circle")]
        [FunctionDescription("Draw a circle")]
        [ArgumentDescription("x", "x position of the center")]
        [ArgumentDescription("y", "y position of the center")]
        [ArgumentDescription("radius", "radius of the circle (distance from center to edge)")]
        [ArgumentDescription("color", "color for the triangle", "{r, g, b, a}")]
        [ArgumentDescription("filled", "(optional) Draw a filled circle (true) or an outline (false, default)")]
        public static void Circle(int x, int y, double radius, ObjectInstance color, bool filled = false)
        {
            var col = Disaster.TypeInterface.Color32(color);
            var radius_f = (float)radius;
            if (Disaster.SoftwareCanvas.inBuffer)
            {
                if (filled)
                    Disaster.SoftwareCanvas.CircleFilled(x, y, radius_f, col);
                else
                    Disaster.SoftwareCanvas.Circle(x, y, radius_f, col);
            } else 
            {
                x += Disaster.SoftwareCanvas.offset.x;
                y += Disaster.SoftwareCanvas.offset.y;
                Disaster.ShapeRenderer.EnqueueRender(
                    () => {
                        if (filled)
                            Raylib_cs.Raylib.DrawCircle(x, y, radius_f, col);
                        else
                            Raylib_cs.Raylib.DrawCircleLines(x, y, radius_f, col);
                    }
                );
            }
        }

        [JSFunction(Name = "line")]
        [FunctionDescription("Draw a 2d line, with an optional gradient.")]
        [ArgumentDescription("x1", "starting x position")]
        [ArgumentDescription("y1", "starting y position")]
        [ArgumentDescription("x2", "ending x position")]
        [ArgumentDescription("y2", "ending y position")]
        [ArgumentDescription("color", "line color", "{r, g, b, a}")]
        [ArgumentDescription("colorEnd", "(optional) line end color. if specified, will blend between the two colors along the line.", "{r, g, b, a}")]
        public static void Line(int x1, int y1, int x2, int y2, ObjectInstance color, ObjectInstance colorEnd = null)
        {
            var col = Disaster.TypeInterface.Color32(color);
            if (Disaster.SoftwareCanvas.inBuffer)
            {
                if (colorEnd == null)
                    Disaster.SoftwareCanvas.Line(x1, y1, x2, y2, col);
                else
                    Disaster.SoftwareCanvas.Line(x1, y1, x2, y2, col, Disaster.TypeInterface.Color32(colorEnd));
            } else
            {
                // TODO: Find a way to do gradient lines with raylib
                x1 += Disaster.SoftwareCanvas.offset.x;
                y1 += Disaster.SoftwareCanvas.offset.y;
                x2 += Disaster.SoftwareCanvas.offset.x;
                y2 += Disaster.SoftwareCanvas.offset.y;
                Disaster.ShapeRenderer.EnqueueRender(
                    () => {
                        Raylib_cs.Raylib.DrawLine(x1, y1, x2, y2, col);
                    }
                );
            }
        }

        [JSFunction(Name = "line3d")]
        [FunctionDescription("Draw a 3d line!")]
        [ArgumentDescription("start", "start position", "{x, y, z}")]
        [ArgumentDescription("end", "end position", "{x, y, z}")]
        [ArgumentDescription("color", "line color", "{r, g, b, a}")]
        public static void Line3d(ObjectInstance start, ObjectInstance end, ObjectInstance color)
        {
            //if (colorEnd == null) colorEnd = color;
            Disaster.ShapeRenderer.EnqueueRender(
                () => {
                    Raylib_cs.Raylib.BeginMode3D(Disaster.ScreenController.camera);
                    Raylib_cs.Raylib.DrawLine3D(
                        Disaster.TypeInterface.Vector3(start),
                        Disaster.TypeInterface.Vector3(end),
                        Disaster.TypeInterface.Color32(color)
                    );
                    Raylib_cs.Raylib.EndMode3D();
                }
            );
            //Disaster.SoftwareCanvas.Line(
            //    Disaster.TypeInterface.Vector3(start),
            //    Disaster.TypeInterface.Vector3(end),
            //    Disaster.TypeInterface.Color32(color),
            //    Disaster.TypeInterface.Color32(colorEnd)
            //);
        }

        [JSFunction(Name = "worldToScreenPoint")]
        [FunctionDescription("Transform a point from world position to screen position.", "{x, y}")]
        [ArgumentDescription("position", "World position to transform", "{x, y, z}")]
        public static ObjectInstance WorldToScreenPoint(ObjectInstance position)
        {
            float ratioW = (float)Disaster.ScreenController.screenWidth / (float)Disaster.ScreenController.windowWidth;
            float ratioH = (float)Disaster.ScreenController.screenHeight / (float)Disaster.ScreenController.windowHeight;
            var p = Raylib_cs.Raylib.GetWorldToScreen(Disaster.TypeInterface.Vector3(position), Disaster.ScreenController.camera);
            p.X *= ratioW;
            p.Y *= ratioH;
            return Disaster.TypeInterface.Object(p);
        }

        [JSFunction(Name = "text")]
        [FunctionDescription("Draw a line of text.")]
        [ArgumentDescription("text", "the text content to draw")]
        [ArgumentDescription("x", "x position of the text")]
        [ArgumentDescription("y", "x position of the text")]
        [ArgumentDescription("color", "text color", "{r, g, b, a}")]
        public static void Text(string text, int x, int y, ObjectInstance color)
        {
            Disaster.SoftwareCanvas.Text(x, y, Disaster.TypeInterface.Color32(color), text);
        }

        [JSFunction(Name = "textTTF")]
        [FunctionDescription("Draw a line of text using a TTF font. TTF text is drawn on top of all other draw elements.")]
        [ArgumentDescription("text", "the text content to draw")]
        [ArgumentDescription("x", "x position of the text")]
        [ArgumentDescription("y", "x position of the text")]
        [ArgumentDescription("color", "text color", "{r, g, b, a}")]
        public static void TextTTF(string text, int x, int y, ObjectInstance color)
        {
            int scale = Disaster.ScreenController.windowHeight / Disaster.ScreenController.screenHeight;
            var font = Disaster.Assets.Font(Disaster.Assets.currentFont).font;
            var fontSize = Disaster.SoftwareCanvas.fontHeight * scale;
            var position = new Vector2(x + Disaster.SoftwareCanvas.offset.x, y + Disaster.SoftwareCanvas.offset.y);
            Disaster.NativeResRenderer.EnqueueRender(
                () => {
                    Raylib_cs.Raylib.DrawTextEx(font, text, position * scale, fontSize, 4, Disaster.TypeInterface.Color32(color));
                }
            );
        }

        [JSFunction(Name = "textStyled")]
        [FunctionDescription("Draw a line of text with styling options")]
        [ArgumentDescription("text", "the text to draw. $b for bold, $w for wavey, $s for drop shadow, $c for color, 0-F (e.g $c5hello $cAthere), $n to reset styling")]
        [ArgumentDescription("x", "x position of the text")]
        [ArgumentDescription("y", "x position of the text")]
        public static void TextStyled(string text, int x, int y)
        {
            Disaster.SoftwareCanvas.TextStyled(x, y, text);
        }

        [JSFunction(Name = "wireframe")]
        [FunctionDescription("Draw a 3D wireframe.")]
        [ArgumentDescription("modelPath", "Path of the model to draw")]
        [ArgumentDescription("position", "Position to draw at", "{x, y, z}")]
        [ArgumentDescription("rotation", "Rotation in euler angles", "{x, y, z}")]
        [ArgumentDescription("color", "Color of the wireframe", "{r, g, b, a}")]
        [ArgumentDescription("backfaceCulling", "(optional) Whether to skip triangles that face away from the camera (default: false)")]
        [ArgumentDescription("drawDepth", "(optional) Whether to render depth on the lines (default: false)")]
        [ArgumentDescription("filled", "(optional) Whether to draw the triangles filled (default: false)")]
        public static void Wireframe(string modelPath, ObjectInstance position, ObjectInstance rotation, ObjectInstance color, bool backfaceCulling = false, bool drawDepth = false, bool filled = false)
        {
            // TODO: Replace software rendering
            var rot = Disaster.TypeInterface.Vector3(rotation);
            var pos = Disaster.TypeInterface.Vector3(position);
            var transform = new Disaster.Transformation(pos, rot, Vector3.One);
            var col = Disaster.TypeInterface.Color32(color);

            var model = Disaster.Assets.Model(modelPath);
            if (model.succeeded)
            {
                unsafe
                {
                    var mesh = ((Raylib_cs.Mesh*)model.model.meshes.ToPointer())[0];
                    Disaster.SoftwareCanvas.Wireframe(mesh, transform.ToMatrix(), col, backfaceCulling, drawDepth, filled);
                }
            }
        }

        [JSFunction(Name = "model")]
        [FunctionDescription("Draw a 3D model, optionally with a specified shader and parameters to use.")]
        [ArgumentDescription("modelPath", "Path of the model to draw")]
        [ArgumentDescription("position", "Position to draw at", "{x, y, z}")]
        [ArgumentDescription("rotation", "Rotation in euler angles", "{x, y, z}")]
        [ArgumentDescription("shaderPath", "(optional) Path of the shader to use (without extension)")]
        [ArgumentDescription("parameters", "(optional) Object of key/value pairs to send to the shader")]
        public static void Model(string modelPath, ObjectInstance position, ObjectInstance rotation, string shaderPath = "", ObjectInstance parameters = null)
        {
            var rot = Disaster.TypeInterface.Vector3(rotation);
            var pos = Disaster.TypeInterface.Vector3(position);
            var transform = new Disaster.Transformation(pos, rot, Vector3.One);
            var model = Disaster.Assets.Model(modelPath);

            if (!model.succeeded)
            {
                return;
            }

            Raylib_cs.Shader shader;

            if (shaderPath == "")
            {
                shader = Disaster.Assets.defaultShader;
            } else
            {
                var loadedShader = Disaster.Assets.Shader(shaderPath);
                if (loadedShader.succeeded)
                {
                    shader = loadedShader.shader;
                } else
                {
                    shader = Disaster.Assets.defaultShader;
                }
            }

            if (parameters == null)
            {
                Disaster.ModelRenderer.EnqueueRender(model.model, shader, transform);
            } 
            else
            {
                var parms = Disaster.TypeInterface.ShaderParameters(parameters);
                Disaster.ModelRenderer.EnqueueRender(model.model, shader, transform, parms);
            }
        }

        [JSFunction(Name = "colorBuffer")]
        [FunctionDescription("Draw a color buffer.")]
        [ArgumentDescription("colors", "color array defining the image", "{r, g, b, a}[]")]
        [ArgumentDescription("x", "x position to draw at")]
        [ArgumentDescription("y", "y position to draw at")]
        [ArgumentDescription("width", "width of the image")]
        public static void ColorBuffer(ObjectInstance colors, int x, int y, int width)
        {
            var pixelBuffer = new Disaster.PixelBuffer(Disaster.TypeInterface.Color32Array(colors), width);
            Disaster.SoftwareCanvas.PixelBuffer(pixelBuffer, x, y, Disaster.Transform2D.identity);
        }

        [JSFunction(Name = "startBuffer")]
        [FunctionDescription("Start drawing to a pixel buffer instead of the screen. Call with Draw.endBuffer();")]
        [ArgumentDescription("width", "Width of the new buffer to draw to")]
        [ArgumentDescription("height", "Height of the new buffer to draw to")]
        public static void StartBuffer(int width, int height)
        {
            Disaster.SoftwareCanvas.StartBuffer(width, height);
        }

        [JSFunction(Name = "endBuffer")]
        [FunctionDescription("Finish drawing to a pixel buffer and return a reference to the new texture.")]
        public static string EndBuffer()
        {
            string output = Disaster.SoftwareCanvas.CreateAssetFromBuffer();
            Disaster.SoftwareCanvas.EndBuffer();
            return output;
        }

        [JSFunction(Name = "texture")]
        [FunctionDescription("Draw a part of an image to the software canvas, with transformations")]
        [ArgumentDescription("texturePath", "path to the image asset")]
        [ArgumentDescription("x", "x position of the image")]
        [ArgumentDescription("y", "x position of the image")]
        [ArgumentDescription("rectangle", "(optional) rectangle defining the portion of the image to draw", "{ x, y, w, h }")]
        [ArgumentDescription("transformation", "(optional) scaling, rotation and origin properties", "{ originX, originY, rotation, scaleX, scaleY, alpha }")]
        public static void Texture(string texturePath, int x, int y, ObjectInstance rectangle = null, ObjectInstance transformation = null)
        {
            var pixelBuffer = Disaster.Assets.PixelBuffer(texturePath);
            if (pixelBuffer.succeeded)
            {
                if (Disaster.SoftwareCanvas.inBuffer)
                {
                    if (rectangle == null && transformation == null)
                    {
                        Disaster.SoftwareCanvas.PixelBuffer(pixelBuffer.pixelBuffer, x, y);
                    } else
                    {
                        if (transformation == null)
                        {
                            Disaster.SoftwareCanvas.PixelBuffer(
                                pixelBuffer.pixelBuffer, 
                                x, y, 
                                Disaster.TypeInterface.Rect(rectangle)
                            );
                        } else if (rectangle == null)
                        {
                            Disaster.SoftwareCanvas.PixelBuffer(
                                pixelBuffer.pixelBuffer, 
                                x, y, 
                                new Disaster.Rect(0, 0, pixelBuffer.pixelBuffer.width, pixelBuffer.pixelBuffer.height), 
                                Disaster.TypeInterface.Transform2d(transformation)
                            );
                        } else
                        {
                            Disaster.SoftwareCanvas.PixelBuffer(
                                pixelBuffer.pixelBuffer,
                                x, y,
                                Disaster.TypeInterface.Rect(rectangle),
                                Disaster.TypeInterface.Transform2d(transformation)
                            );
                        }
                    }
                }
                else
                {
                    x += Disaster.SoftwareCanvas.offset.x;
                    y += Disaster.SoftwareCanvas.offset.y;
                    Disaster.ShapeRenderer.EnqueueRender(
                        () => {
                            var texture = pixelBuffer.pixelBuffer.texture;
                            Disaster.Rect rect;
                            if (rectangle != null)
                                rect = Disaster.TypeInterface.Rect(rectangle);
                            else
                                rect = new Disaster.Rect(0, 0, texture.width, texture.height);

                            
                            Disaster.Transform2D trans;
                            if (transformation != null)
                                trans = Disaster.TypeInterface.Transform2d(transformation);
                            else
                                trans = new Disaster.Transform2D(new Vector2(0, 0), new Vector2(1, 1), 0f, 1f);

                            if (trans.scale.X < 0) rect.width *= -1;
                            if (trans.scale.Y < 0) rect.height *= -1;
                            
                            var source_rect = new Raylib_cs.Rectangle(rect.x, rect.y, rect.width, rect.height);
                            var dest_rect = new Raylib_cs.Rectangle(x, y, rect.width * trans.scale.X, rect.height * trans.scale.Y);

                            trans.scale.X = Math.Abs(trans.scale.X);
                            trans.scale.Y = Math.Abs(trans.scale.Y);
                            
                            Raylib_cs.Raylib.DrawTexturePro(texture, source_rect, dest_rect, new Vector2(trans.origin.X * trans.scale.X, trans.origin.Y * trans.scale.Y), trans.rotation, Raylib_cs.Color.WHITE);
                        }
                    );
                }               
            } else
            {
                System.Console.WriteLine($"Failed to draw texture: {texturePath}");
            }
        }

        [JSFunction(Name = "nineSlice")]
        [FunctionDescription("Draw a 9-sliceds sprite. Tiles the center and edges of a sprite over a given area. (look up 9-slice!)")]
        [ArgumentDescription("texturePath", "Texture to draw")]
        [ArgumentDescription("nineSliceArea", "A rectangle defining the center region of the 9-slide", "{x, y, w, h}")]
        [ArgumentDescription("x", "x position to draw to")]
        [ArgumentDescription("y", "y position to draw to")]
        [ArgumentDescription("width", "width of the area to draw to")]
        [ArgumentDescription("height", "height of the area to draw to")]
        public static void NineSlice(string texturePath, ObjectInstance nineSliceArea, int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;
            
            var pixelBuffer = Disaster.Assets.PixelBuffer(texturePath);
            if (pixelBuffer.succeeded)
            {
                var rect = Disaster.TypeInterface.Rect(nineSliceArea);
                if (Disaster.SoftwareCanvas.inBuffer)
                // if (true)
                {
                    Disaster.SoftwareCanvas.NineSlice(pixelBuffer.pixelBuffer, rect, new Disaster.Rect(x, y, width, height));
                } else
                {
                    x += Disaster.SoftwareCanvas.offset.x;
                    y += Disaster.SoftwareCanvas.offset.y;
                    var texture = pixelBuffer.pixelBuffer.texture;

                    // We don't use raylibs built in NPatch because it stretches rather than tiles
                    // void DrawTextureTiled(Texture2D texture, Rectangle source, Rectangle dest, Vector2 origin, float rotation, float scale, Color tint);
                    var xs = new int[]{0, (int)rect.x, (int)(rect.x + rect.width), texture.width};
                    var ys = new int[]{0, (int)rect.y, (int)(rect.y + rect.height), texture.height};

                    var srcRects = new Raylib_cs.Rectangle[3,3];
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            srcRects[i, j] = new Raylib_cs.Rectangle(xs[j], ys[i], xs[j+1] - xs[j], ys[i + 1] - ys[i]);
                        }
                    }

                    var w1 = (int)(Math.Min(xs[1] - xs[0], width));
                    var w3 = (int)(Math.Min(xs[3] - xs[2], width - w1));
                    var w2 = width - w1 - w3;
                    var ws = new int[]{w1, w2, w3};

                    var h1 = (int)(Math.Min(ys[1] - ys[0], height));
                    var h3 = (int)(Math.Min(ys[3] - ys[2], height - h1));
                    var h2 = height - h1 - h3;
                    var hs = new int[]{h1, h2, h3};

                    Disaster.ShapeRenderer.EnqueueRender(
                        () => {
                            int ry = y;
                            for (int i = 0; i < 3; i++)
                            {
                                if (hs[i] <= 0) break;

                                int rx = x;
                                for (int j = 0; j < 3; j++)
                                {
                                    if (ws[j] <= 0) break;

                                    var dstRect = new Raylib_cs.Rectangle(rx, ry, ws[j], hs[i]);
                                    Raylib_cs.Raylib.DrawTextureTiled(texture, srcRects[i,j], dstRect, Vector2.Zero, 0, 1, Raylib_cs.Color.WHITE);
                                    rx += ws[j];
                                }
                                ry += hs[i];
                            }
                        }
                    );
                }
            }
            else
            {
                System.Console.WriteLine($"Failed to draw texture: {texturePath}");
            }
        }

        [JSFunction(Name = "setCamera")]
        [FunctionDescription("Set the 3d camera position and rotation")]
        [ArgumentDescription("position", "position to set the camera to", "{x, y, z}")]
        [ArgumentDescription("rotation", "rotation to set the camera to, in euler angles", "{x, y, z}")]
        public static void SetCamera(ObjectInstance position, ObjectInstance rotation)
        {
            var rot = Disaster.TypeInterface.Vector3(rotation);
            var pos = Disaster.TypeInterface.Vector3(position);
            var forward = Disaster.Util.EulerToForward(rot);
            Disaster.ScreenController.camera.position = pos;
            Disaster.ScreenController.camera.target = pos + forward;
        }

        [JSFunction(Name = "setFOV")]
        [FunctionDescription("Set the field of view of the camera")]
        [ArgumentDescription("fov", "field of view")]
        public static void SetFOV(double fov)
        {
            Disaster.ScreenController.camera.fovy = (float)fov;
        }

        [JSFunction(Name = "getCameraTransform")]
        [FunctionDescription("Get the 3d camera transformation", "{forward, up, right, position, rotation}")]
        public static ObjectInstance GetCameraTransform()
        {
            var output = Disaster.JS.instance.engine.Object.Construct();
            output["forward"] = Disaster.TypeInterface.Object(Vector3.Normalize(Disaster.ScreenController.camera.target - Disaster.ScreenController.camera.position));
            output["up"] = Disaster.TypeInterface.Object(Disaster.ScreenController.camera.up);
            output["right"] = Disaster.TypeInterface.Object(Vector3.Cross(Disaster.ScreenController.camera.target - Disaster.ScreenController.camera.position, Disaster.ScreenController.camera.up));
            output["position"] = Disaster.TypeInterface.Object(Disaster.ScreenController.camera.position);
            output["rotation"] = Disaster.TypeInterface.Object(Disaster.Util.ForwardToEuler(Disaster.ScreenController.camera.target - Disaster.ScreenController.camera.position));
            return output;
        }

        [JSFunction(Name ="setBlendMode")]
        [FunctionDescription("Set the blending mode for future draw operations. normal, noise, add")]
        public static void SetBlendMode(string blendMode)
        {
            // TODO: Set raylib blendmode
            switch (blendMode)
            {
                case "normal":
                    Disaster.SoftwareCanvas.blendMode = Disaster.SoftwareCanvas.BlendMode.Normal;
                    break;
                case "noise":
                    Disaster.SoftwareCanvas.blendMode = Disaster.SoftwareCanvas.BlendMode.Noise;
                    break;
                case "add":
                    Disaster.SoftwareCanvas.blendMode = Disaster.SoftwareCanvas.BlendMode.Add;
                    break;
                case "dither":
                    Disaster.SoftwareCanvas.blendMode = Disaster.SoftwareCanvas.BlendMode.Dither;
                    break;
                case "subtract":
                    Disaster.SoftwareCanvas.blendMode = Disaster.SoftwareCanvas.BlendMode.Subtract;
                    break;
                default:
                    System.Console.WriteLine($"Unknown blendmode: {blendMode}");
                    break;
            }
        }

    }
}