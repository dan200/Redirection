using Dan200.Core.Assets;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Script;
using Dan200.Core.Util;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dan200.Core.Animation
{
    public class LuaAnimation : IAnimation
    {
        private static LuaMachine s_animMachine = null;
        private static LuaFunction s_install_animation = null;
        private static LuaFunction s_call_animation = null;
        private static Dictionary<string, LuaAnimation> s_loadedAnims = new Dictionary<string, LuaAnimation>();

        public static bool Exists(string path)
        {
            return s_loadedAnims.ContainsKey(path);
        }

        public static LuaAnimation Get(string path)
        {
            if (s_loadedAnims.ContainsKey(path))
            {
                return s_loadedAnims[path];
            }
            else
            {
                App.Log("Error: Animation " + path + " does not exist. Using defaults/default.anim.lua instead.");
                return s_loadedAnims["defaults/default.anim.lua"];
            }
        }

        private static void Init()
        {
            if (s_animMachine == null)
            {
                s_animMachine = new LuaMachine();
                s_animMachine.AllowByteCodeLoading = false;
                s_animMachine.EnforceTimeLimits = true;
                s_animMachine.RemoveUnsafeGlobals();

                s_animMachine.SetGlobal("print", (LuaCFunction)delegate (LuaArgs args)
                {
                    var output = new StringBuilder();
                    for (int i = 0; i < args.Length; ++i)
                    {
                        output.Append(args.ToString(i));
                        if (i < args.Length - 1)
                        {
                            output.Append(", ");
                        }
                    }
                    App.UserLog(output.ToString());
                    return LuaArgs.Empty;
                });

                s_animMachine.SetGlobal("dofile", (LuaCFunction)delegate (LuaArgs args)
                {
                    var path = args.GetString(0);
                    if (Assets.Assets.Exists<LuaScript>(path))
                    {
                        var script = LuaScript.Get(path);
                        return s_animMachine.DoString(script.Code, "@" + AssetPath.GetFileName(path));
                    }
                    throw new LuaError("Script not found: " + path);
                });
                s_animMachine.DoString(
                    @"do
                        function expect( value, sExpectedType, nArg )
                            local sFoundType = type( value )
                            if sFoundType ~= sExpectedType then
                                error( ""Expected "" .. sExpectedType .. "" at argument "" .. nArg .. "", got "" .. sFoundType, 3 )
                            end
                        end

                        local tResults = {}
                        function require( s )
                            expect( s, ""string"", 1 )
                            if tResults[s] == nil then
                                local ok, result = pcall( dofile, ""scripts/"" .. s .. "".lua"" )
                                if not ok then
                                    error( result, 0 )
                                elseif result == nil then
                                    tResults[s] = true
                                else
                                    tResults[s] = result
                                end
                            end
                            return tResults[s]
                        end

                        abs = math.abs
                        floor = math.floor
                        ceil = math.ceil
                        min = math.min
                        max = math.max
                        pow = math.pow
                        sqrt = math.sqrt
                        pi = math.pi

                        function clamp( x, low, hi )
                            expect( x, ""number"", 1 )
                            expect( low, ""number"", 2 )
                            expect( hi, ""number"", 3 )
                            if x < low then
                                return low
                            elseif x > hi then
                                return hi
                            else
                                return x
                            end
                        end
                        
                        local math_rad, math_deg = math.rad, math.deg
                        local math_sin, math_cos, math_tan = math.sin, math.cos, math.tan
                        local math_asin, math_acos, math_atan = math.asin, math.acos, math.atan

                        function sin( x )
                            expect( x, ""number"", 1 )
                            return math_sin( math_rad( x ) )
                        end

                        function cos( x )
                            expect( x, ""number"", 1 )
                            return math_cos( math_rad( x ) )
                        end

                        function tan( x )
                            expect( x, ""number"", 1 )
                            return math_tan( math_rad( x ) )
                        end

                        function asin( x )
                            expect( x, ""number"", 1 )
                            return math_deg( math_asin( x ) )
                        end

                        function acos( x )
                            expect( x, ""number"", 1 )
                            return math_deg( math_acos( x ) )
                        end

                        function atan( x )
                            expect( x, ""number"", 1 )
                            return math_deg( math_atan( x ) )
                        end

                        function atan2( y, x )
                            expect( y, ""number"", 1 )
                            expect( x, ""number"", 2 )
                            return math_deg( math_atan( y, x ) )
                        end

                        function ease( f )
                            expect( f, ""number"", 1 )
                            f = clamp( f, 0.0, 1.0 )
                            return (3 - 2*f) * f * f
                        end

                        function lerp( a, b, f )
                            expect( a, ""number"", 1 )
                            expect( b, ""number"", 2 )
                            expect( f, ""number"", 3 )
                            return a + (b-a) * f
                        end

                        function lerp3( x0, y0, z0, x1, y1, z1, f )
                            expect( x0, ""number"", 1 )
                            expect( y0, ""number"", 2 )
                            expect( z0, ""number"", 3 )
                            expect( x1, ""number"", 4 )
                            expect( y1, ""number"", 5 )
                            expect( z1, ""number"", 6 )
                            expect( f, ""number"", 7 )
	                        return lerp(x0, x1, f), lerp(y0, y1, f), lerp(z0, z1, f)
                        end

                        function step( a, x )
                            expect( a, ""number"", 1 )
                            expect( x, ""number"", 2 )
                            return (x >= a) and 1 or 0
                        end

                        function smoothstep( a, b, x )
                            expect( a, ""number"", 1 )
                            expect( b, ""number"", 2 )
                            expect( x, ""number"", 3 )
                            local f = clamp( (x - a) / (b - a), 0, 1 )
                            return ease(f)
                        end

                        function rot2D( x, y, a, xo, yo )
                            expect( x, ""number"", 1 )
                            expect( y, ""number"", 2 )
                            expect( a, ""number"", 3 )
                            if xo then
                                expect( xo, ""number"", 4 )
                            end
                            if yo then
                                expect( yo, ""number"", 5 )
                            end
                            xo = xo or 0
                            yo = yo or 0
                            local ar = -math_rad(a)
                            local ca, sa = math_cos(ar), math_sin(ar)
                            return
                                xo + (x - xo) * ca - (y - yo) * sa,
                                yo + (x - xo) * sa + (y - yo) * ca
                        end

                        function setpos( part, x, y, z )
                            expect( part, ""table"", 1 )
                            expect( x, ""number"", 2 )
                            expect( y, ""number"", 3 )
                            expect( z, ""number"", 4 )
                            part.x = x
                            part.y = y
                            part.z = z
                        end

                        function lookat( part, x, y, z )
                            expect( part, ""table"", 1 )
                            expect( x, ""number"", 2 )
                            expect( y, ""number"", 3 )
                            expect( z, ""number"", 4 )
	                        local dx = x - part.x
	                        local dy = y - part.y
	                        local dz = z - part.z
	                        local dxz = sqrt( dx*dx + dz*dz )
	                        part.ry = -atan2( dx, -dz )
	                        part.rx = atan2( dy, dxz )
	                        part.rz = 0
                        end

                        local tAnimations = {}
                        function install_animation( sAnimName, sAnimCode, sChunkName )
                            local tAnimEnv = {}
                            setmetatable( tAnimEnv, { __index = _ENV } )

                            local fnAnim, sError = load( sAnimCode, sChunkName, ""t"", tAnimEnv )
                            if fnAnim then
                                local ok, sError = pcall( fnAnim )
                                if ok then
                                    tAnimations[ sAnimName ] = tAnimEnv.animate
                                else
                                    tAnimations[ sAnimName ] = nil
                                    error( sError, 0 )
                                end
                            else
                                tAnimations[ sAnimName ] = nil
                                error( sError, 0 )
                            end
                        end

                        local tPart = {}
                        function call_animation( sAnimName, sPartName, nTime )
                            tPart.name = sPartName
                            tPart.hide = false
                            tPart.x, tPart.y, tPart.z = 0,0,0
                            tPart.rx, tPart.ry, tPart.rz = 0,0,0
                            tPart.sx, tPart.sy, tPart.sz = 1,1,1
                            tPart.u, tPart.v = 0,0
                            tPart.su, tPart.sv = 1,1
                            tPart.r, tPart.g, tPart.b, tPart.a = 1,1,1,1
                            tPart.fov = nil
                            local fnAnimate = tAnimations[sAnimName]
                            if fnAnimate then
                                fnAnimate( tPart, nTime )
                            end
                            return
                                tPart.hide,
                                tPart.x, tPart.y, tPart.z,
                                tPart.rx, tPart.ry, tPart.rz,
                                tPart.sx, tPart.sy, tPart.sz,
                                tPart.u, tPart.v,
                                tPart.su, tPart.sv,
                                tPart.r, tPart.g, tPart.b, tPart.a,
                                tPart.fov
                        end
                    end",
                    "=LuaAnimation.Init"
                );

                s_install_animation = s_animMachine.GetGlobal("install_animation").GetFunction();
                s_call_animation = s_animMachine.GetGlobal("call_animation").GetFunction();
            }
        }

        public static void ReloadAll()
        {
            // Do a total reset of the animation system
            UnloadAll();
            Init();

            // Load every animation we can find
            Load(LuaScript.Get("defaults/default.anim.lua"));
            foreach (var script in Assets.Assets.Find<LuaScript>("animation"))
            {
                Load(script);
            }
        }

        public static void UnloadAll()
        {
            if (s_animMachine != null)
            {
                s_loadedAnims.Clear();
                s_animMachine.Dispose();
                s_animMachine = null;
                s_install_animation = null;
                s_call_animation = null;
            }
        }

        private static void Load(LuaScript script)
        {
            if (!s_loadedAnims.ContainsKey(script.Path))
            {
                s_loadedAnims.Add(script.Path, new LuaAnimation(script.Path));
                try
                {
                    s_install_animation.Call(new LuaArgs(script.Path, script.Code, AssetPath.GetFileName(script.Path)));
                }
                catch (LuaError e)
                {
                    App.Log("Lua Error: {0}", e.Message);
                }
            }
        }

        private static void Call(string path, string partName, float time, out bool o_visible, out Matrix4 o_transform, out Vector2 o_uvOffset, out Vector2 o_uvScale, out Vector4 o_colour, out float o_cameraFOV)
        {
            try
            {
                var results = s_call_animation.Call(new LuaArgs(path, partName, time));
                var hide = results.GetBool(0);
                var x = results.GetFloat(1);
                var y = results.GetFloat(2);
                var z = results.GetFloat(3);
                var rx = results.GetFloat(4);
                var ry = results.GetFloat(5);
                var rz = results.GetFloat(6);
                var sx = results.GetFloat(7);
                var sy = results.GetFloat(8);
                var sz = results.GetFloat(9);
                var u = results.GetDouble(10);
                var v = results.GetDouble(11);
                var su = results.GetFloat(12);
                var sv = results.GetFloat(13);
                var r = results.GetFloat(14);
                var g = results.GetFloat(15);
                var b = results.GetFloat(16);
                var a = results.GetFloat(17);
                o_visible = !hide;
                MathUtils.CreateTransRotScaleMatrix(
                    new Vector3(x, y, z),
                    new Vector3(rx, ry, rz) * MathUtils.PI_OVER_ONE_EIGHTY,
                    new Vector3(sx, sy, sz),
                    out o_transform
                );
                o_uvOffset = new Vector2((float)(u - Math.Floor(u)), (float)(v - Math.Floor(v)));
                o_uvScale = new Vector2(su, sv);
                o_colour = new Vector4(r, g, b, a);

                if (!results.IsNil(18))
                {
                    var cameraFOV = results.GetFloat(16);
                    o_cameraFOV = cameraFOV * MathUtils.PI_OVER_ONE_EIGHTY;
                }
                else
                {
                    o_cameraFOV = Game.Game.Game.DEFAULT_FOV;
                }
            }
            catch (LuaError e)
            {
                App.Log("Lua Error: {0}", e.Message);
                o_visible = true;
                o_transform = Matrix4.Identity;
                o_uvOffset = Vector2.Zero;
                o_uvScale = Vector2.One;
                o_colour = Vector4.One;
                o_cameraFOV = Game.Game.Game.DEFAULT_FOV;
            }
        }

        private string m_path;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        private LuaAnimation(string path)
        {
            m_path = path;
        }

        public void Animate(string partName, float time, out bool o_visible, out Matrix4 o_transform, out Vector2 o_uvOffset, out Vector2 o_uvScale, out Vector4 o_colour, out float o_cameraFOV)
        {
            Call(m_path, partName, time, out o_visible, out o_transform, out o_uvOffset, out o_uvScale, out o_colour, out o_cameraFOV);
        }
    }
}

