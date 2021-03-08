using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.Audio.Null;
using Dan200.Core.Audio.OpenAL;
using Dan200.Core.Render;
using Dan200.Core.Script;
using Dan200.Core.Util;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using SDL2;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dan200.Core.Main
{
    public enum LogLevel
    {
        Debug,
        Info,
        User,
        Error
    }

    public class App
    {
        public const int MAX_FPS = 125;

        public const int MAX_LOG_FILES = 10;
        public const int RECENT_LOG_SIZE = 8;

#if DEBUG
        public static readonly bool Debug = true;
#else
        public const bool Debug = false;
#endif

#if STEAM
#if DEBUG
        public static readonly bool Steam = true;
#else
        public const bool Steam = true;
#endif
#else
#if DEBUG
        public static readonly bool Steam = false;
#else
        public const bool Steam = false;
#endif
#endif

        public static float FPS
        {
            get;
            private set;
        }

        public static ProgramArguments Arguments
        {
            get;
            private set;
        }

        public static bool Demo
        {
            get
            {
                return Arguments.GetBool("demo", false);
            }
        }

        public static GameInfo Info
        {
            get;
            private set;
        }

        public static Platform Platform
        {
            get;
            private set;
        }

        public static string SavePath
        {
            get;
            private set;
        }

        public static string AssetPath
        {
            get;
            private set;
        }

        private static SDLException MakeSDLException(string functionName)
        {
            string error = SDL.SDL_GetError();
            if (error != null && error.Length > 0)
            {
                SDL.SDL_ClearError();
                return new SDLException(functionName, error);
            }
            else
            {
                return new SDLException(functionName);
            }
        }

        public static void CheckSDLResult(string functionName, int result)
        {
            if (result < 0)
            {
                throw MakeSDLException(functionName);
            }
        }

        public static void CheckSDLResult(string functionName, IntPtr result)
        {
            if (result == IntPtr.Zero)
            {
                throw MakeSDLException(functionName);
            }
        }

        public static void CheckSDLResult(string functionName, string result)
        {
            if (result == null)
            {
                throw MakeSDLException(functionName);
            }
        }

        public static void CheckSDLError(string functionName)
        {
            string error = SDL.SDL_GetError();
            if (error != null && error.Length > 0)
            {
                throw MakeSDLException(functionName);
            }
        }

        public static void CheckOpenGLError(bool throwExceptionInDebug = false)
        {
            if (App.Debug || throwExceptionInDebug)
            {
                var error = GL.GetError();
                if (error != ErrorCode.NoError)
                {
                    throw new OpenGLException(error);
                }
            }
        }

        private static bool s_openALErrorPrinted = false;

        public static void CheckOpenALError(bool throwExceptionInDebug = false)
        {
            var error = AL.GetError();
            if (error != ALError.NoError)
            {
                if (App.Debug || throwExceptionInDebug)
                {
                    throw new OpenALException(error);
                }
                else if (!s_openALErrorPrinted)
                {
                    App.Log("Encountered OpenAL error: " + error);
                    App.Log(Environment.StackTrace);
                    s_openALErrorPrinted = true;
                }
            }
        }

        public static void CheckSteamworksResult(string functionName, bool result)
        {
            if (!result)
            {
                throw new SteamworksException(functionName);
            }
        }

        private static string LogFilePath = null;
        private static TextWriter LogFile = null;
        private static List<string> EarlyLogText = new List<string>();
        private static Queue<string> RecentLogText = new Queue<string>(RECENT_LOG_SIZE);

        public static IReadOnlyCollection<string> RecentLog
        {
            get
            {
                return RecentLogText.ToReadOnly();
            }
        }

        public static void Log(string text, LogLevel level)
        {
            // Discard debug
            if (!App.Debug && level < LogLevel.Info)
            {
                return;
            }

            // Promote errors
            if (text.ToLowerInvariant().Contains("error"))
            {
                level = LogLevel.Error;
            }

            // Write to console
            if (App.Debug)
            {
                System.Diagnostics.Debug.WriteLine(text);
                System.Diagnostics.Debug.Flush();
            }
            else
            {
                Console.WriteLine(text);
                Console.Out.Flush();
            }

            // Write to file
            if (LogFile != null)
            {
                LogFile.WriteLine(text);
                LogFile.Flush();
            }
            else
            {
                EarlyLogText.Add(text);
            }

            // Add to recent error log
            if (level >= LogLevel.User)
            {
                while (RecentLogText.Count >= RECENT_LOG_SIZE)
                {
                    RecentLogText.Dequeue();
                }
                RecentLogText.Enqueue(text);
            }
        }

        public static void DebugLog(string format, params object[] args)
        {
            Log(string.Format(format, args), LogLevel.Debug);
        }

        public static void Log(string format, params object[] args)
        {
            Log(string.Format(format, args), LogLevel.Info);
        }

        public static void UserLog(string format, params object[] args)
        {
            Log(string.Format(format, args), LogLevel.User);
        }

        public static void ErrorLog(string format, params object[] args)
        {
            Log(string.Format(format, args), LogLevel.Error);
        }

        private static void InitSavePath()
        {
            if (App.Steam)
            {
                // Save to Steam user folder
                string savePath;
                CheckSteamworksResult("SteamUser.GetUserDataFolder", SteamUser.GetUserDataFolder(out savePath, 4096));
                SavePath = savePath;
            }
            else if (App.Debug)
            {
                // Save to Debug directory
                SavePath = "../Saves".Replace('/', Path.DirectorySeparatorChar);
            }
            else
            {
                // Save to SDL2 directory
                SavePath = SDL.SDL_GetPrefPath(App.Info.DeveloperName, App.Info.Title);
                App.CheckSDLResult("SDL_GetPrefPath", SavePath);
                SavePath = SavePath.Replace(App.Info.DeveloperName + Path.DirectorySeparatorChar, "");
                Directory.CreateDirectory(SavePath);
            }
        }

        private static void StartLogging()
        {
            // Setup logging
            try
            {
                // Build the path
                string logFilePath = Path.Combine(SavePath, "logs");
                logFilePath = Path.Combine(logFilePath, DateTime.Now.ToString("s").Replace(":", "-") + ".txt");

                // Prepare the directory
                var logDirectory = Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(logDirectory))
                {
                    // Create the log file directory
                    Directory.CreateDirectory(logDirectory);
                }
                else
                {
                    // Delete old log files from the directory
                    var directoryInfo = new DirectoryInfo(logDirectory);
                    var oldFiles = directoryInfo.EnumerateFiles()
                        .Where(file => file.Extension == ".txt")
                        .OrderByDescending(file => file.CreationTime)
                        .Skip(MAX_LOG_FILES - 1);
                    foreach (var file in oldFiles.ToList())
                    {
                        file.Delete();
                    }
                }

                // Open the log file, log early messages
                LogFilePath = logFilePath;
                Log("Logging to {0}", LogFilePath);
                LogFile = new StreamWriter(logFilePath);
                for (int i = 0; i < EarlyLogText.Count; ++i)
                {
                    LogFile.WriteLine(EarlyLogText[i]);
                }
                EarlyLogText.Clear();
                LogFile.Flush();
            }
            catch (IOException)
            {
                Log("Failed to open log file");
            }
        }

        private static void RegisterAssetTypes()
        {
            // Engine
            Assets.Assets.RegisterType<AnimSet>("animset");
            Assets.Assets.RegisterType<Effect>("effect");
            Assets.Assets.RegisterType<Font>("fnt");
            Assets.Assets.RegisterType<Language>("lang");
            Assets.Assets.RegisterType<LuaScript>("lua");
            Assets.Assets.RegisterType<Model>("obj");
            Assets.Assets.RegisterType<MaterialFile>("mtl");
            Assets.Assets.RegisterType<ParticleStyle>("pfx");
            Assets.Assets.RegisterType<Texture>("png");
            Assets.Assets.RegisterType<SoundSet>("soundset");
            Assets.Assets.RegisterType<TextAsset>("txt");
            Assets.Assets.RegisterType<BinaryAsset>("bin");
            if (App.Arguments.GetBool("nosound"))
            {
                Assets.Assets.RegisterType<NullMusic>("ogg");
                Assets.Assets.RegisterType<NullSound>("wav");
            }
            else
            {
                Assets.Assets.RegisterType<OpenALMusic>("ogg");
                Assets.Assets.RegisterType<OpenALSound>("wav");
            }

            // Game
            Assets.Assets.RegisterType<Game.Game.Campaign>("campaign");
            Assets.Assets.RegisterType<Game.Level.LevelData>("level");
            Assets.Assets.RegisterType<Game.Level.Tile>("tile");
            Assets.Assets.RegisterType<Game.Level.Sky>("sky");
            Assets.Assets.RegisterType<Game.Arcade.ArcadeDisk>("disk");
        }

        public static void Run<TGame>(GameInfo info, string[] args) where TGame : IGame, new()
        {
            bool sdlInitialised = false;
            bool sdlImageInitialised = false;
            bool steamworksInitialised = false;
            try
            {
                // Store info
                Info = info;

                // Get commandline arguments
                Arguments = new ProgramArguments(args);

                // Determine platform
                string platformString = SDL.SDL_GetPlatform();
                switch (platformString)
                {
                    case "Windows":
                        {
                            Platform = Platform.Windows;
                            break;
                        }
                    case "Mac OS X":
                        {
                            Platform = Platform.OSX;
                            break;
                        }
                    case "Linux":
                        {
                            Platform = Platform.Linux;
                            break;
                        }
                    default:
                        {
                            Platform = Platform.Unknown;
                            break;
                        }
                }

                // Print App Info
                if (App.Debug && App.Steam)
                {
                    Log("{0} {1} (Steam Debug build)", Info.Title, Info.Version);
                }
                else if (App.Debug)
                {
                    Log("{0} {1} (Debug build)", Info.Title, Info.Version);
                }
                else if (App.Steam)
                {
                    Log("{0} {1} (Steam build)", Info.Title, Info.Version);
                }
                else
                {
                    Log("{0} {1}", Info.Title, Info.Version);
                }
                if (!Arguments.IsEmpty)
                {
                    Log("Command Line Arguments: {0}", Arguments);
                }
                Log("Developed by {0} ({1})", Info.DeveloperName, Info.DeveloperEmail);
                Log("Platform: {0} ({1})", Platform, platformString);

                // Setup Steamworks
                if (App.Steam)
                {
                    // Relaunch game under Steam if launched externally
                    if (!App.Debug)
                    {
                        if (File.Exists("steam_appid.txt"))
                        {
                            File.Delete("steam_appid.txt");
                        }

                        AppId_t appID = (Info.SteamAppID > 0) ? new AppId_t(Info.SteamAppID) : AppId_t.Invalid;
                        if (SteamAPI.RestartAppIfNecessary(appID))
                        {
                            Log("Relaunching game in Steam");
                            return;
                        }
                    }

                    // Initialise Steamworks
                    if( !SteamAPI.Init() )
                    {
                        throw new SteamworksException("SteamAPI_Init", "If you have just installed " + App.Info.Title + " for the first time, try restarting Steam");
                    }
                    Log("Steamworks initialised");
                    steamworksInitialised = true;
                }

                // Print SDL version
                SDL.SDL_version version;
                SDL.SDL_GetVersion(out version);
                Log("SDL version: {0}.{1}.{2}", version.major, version.minor, version.patch);

                // Setup SDL
                SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_MAC_FULLSCREEN_SPACES, "1");
                SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
                CheckSDLResult("SDL_Init", SDL.SDL_Init(
                    SDL.SDL_INIT_VIDEO |
                    SDL.SDL_INIT_AUDIO |
                    SDL.SDL_INIT_GAMECONTROLLER |
                    SDL.SDL_INIT_JOYSTICK |
                    SDL.SDL_INIT_HAPTIC
                ));
                Log("SDL2 Initialised");
                sdlInitialised = true;

                CheckSDLResult("IMG_Init", SDL_image.IMG_Init(
                    SDL_image.IMG_InitFlags.IMG_INIT_PNG
                ));
                sdlImageInitialised = true;
                Log("SDL2_Image initialised");

                // Determine save path
                InitSavePath();
                StartLogging();

                // Determine asset directory
                if (App.Debug)
                {
                    AssetPath = "../../assets";
                }
                else
                {
                    if (App.Platform == Platform.OSX)
                    {
                        AssetPath = "assets";
                    }
                    else
                    {
                        string basePath = SDL.SDL_GetBasePath();
                        AssetPath = Path.Combine(basePath, "assets");
                    }
                }
                if (!Directory.Exists(AssetPath))
                {
                    throw new IOException
                    (
                        "Could not locate assets directory (" + AssetPath + ")" + Environment.NewLine +
                        "This must be provided from a legal copy of " + App.Info.Title + ". Visit " + App.Info.Website + " for more info."
                    );
                }

                // Register asset types
                RegisterAssetTypes();

                // Load controller database
                var gameControllerDBPath = Path.Combine(AssetPath, "gamecontrollerdb.txt");
                if (File.Exists(gameControllerDBPath))
                {
                    try
                    {
                        CheckSDLResult("SDL_GameControllerAddMappingsFromFile", SDL.SDL_GameControllerAddMappingsFromFile(gameControllerDBPath));
                        App.Log("Loaded gamepad mappings from gamecontrollerdb.txt");
                    }
                    catch (SDLException)
                    {
                        App.Log("Error: Failed to load gamepad mappings from gamecontrollerdb.txt");
                    }
                }
                else
                {
                    App.Log("Error: gamecontrollerdb.txt not found");
                }

                // Create game
                using (var game = new Game.Game.Game())
                {
                    App.CheckOpenGLError();

                    // Main loop
                    uint lastFrameStart = SDL.SDL_GetTicks();
                    while (!game.Over)
                    {
                        // Get time
                        uint frameStart = SDL.SDL_GetTicks();
                        uint delta = frameStart - lastFrameStart;
                        FPS = 0.8f * FPS + 0.2f * ((delta > 0) ? (1000.0f / (float)delta) : 1000.0f);
                        float dt = Math.Max((float)delta / 1000.0f, 0.0f);

                        // Handle SDL events
                        SDL.SDL_Event e;
                        while (!game.Over && SDL.SDL_PollEvent(out e) != 0)
                        {
                            // Pass event to game
                            game.HandleEvent(ref e);
                        }

                        // Handle Steamworks events
                        if (!game.Over && App.Steam)
                        {
                            SteamAPI.RunCallbacks();
                        }

                        // Update game
                        if (!game.Over)
                        {
                            game.Update(dt);
                        }

                        // Render game
                        if (!game.Over)
                        {
                            game.Render();
                            App.CheckOpenGLError();
                            RenderStats.EndFrame();
                        }

                        // Sleep if necessary
                        if (!game.Over)
                        {
                            uint minFrameTime = 1000 / MAX_FPS;
                            uint frameTime = (SDL.SDL_GetTicks() - frameStart);
                            if (frameTime < minFrameTime)
                            {
                                // SDL.SDL_Delay(minFrameTime - frameTime);
                            }
                        }

                        // Update timer for next frame
                        lastFrameStart = frameStart;
                    }
                }
            }
#if !DEBUG
			catch( Exception e )
			{
                // Log error to console
				string message = string.Format( "Game crashed with {0}: {1}", e.GetType().FullName, e.Message );
                Log( message );
                Log( e.StackTrace );
                while( (e = e.InnerException) != null )
                {
                    Log( "Caused by {0}: {1}", e.GetType().FullName, e.Message );
                    Log( e.StackTrace );
                }

                // Open an emergency log file if necessary
                if( LogFile == null )
                {
                    try
                    {
                        LogFilePath = "log.txt";
                        Log( "Logging to " + LogFilePath );
                        LogFile = new StreamWriter( "log.txt" );
                        for( int i=0; i<EarlyLogText.Count; ++i )
                        {
                            LogFile.WriteLine( EarlyLogText[i] );
                        }
                        EarlyLogText.Clear();
                        LogFile.Flush();
                    }
                    catch( IOException )
                    {
                        Log( "Failed to open log file" );
                        LogFilePath = null;
                    }
                }

                // Pop up the message box
                //if( sdlInitialised ) // The docs say SDL doesn't need to be inited to call ShowSimpleMessageBox
                {
    				try
    				{
                        if( LogFilePath != null )
                        {
                            message += Environment.NewLine;
                            message += Environment.NewLine;
                            message += "Callstack written to " + LogFilePath;
                        }

    					CheckSDLResult( "SDL_ShowSimpleMessageBox", SDL.SDL_ShowSimpleMessageBox(
    						SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
    						Info.Title + " " + Info.Version,
                            message,
    						IntPtr.Zero
    					) );
    				}
    				catch( SDLException e2 )
    				{
        				Log( e2.Message );
    				}
                }
			}
#endif
            finally
            {
                // Shutdown SDL
                if (sdlImageInitialised)
                {
                    SDL_image.IMG_Quit();
                    Log("SDL2_Image shut down");
                }
                if (sdlInitialised)
                {
                    SDL.SDL_Quit();
                    Log("SDL2 shut down");
                }

                // Shutdown Steamworks
                if (steamworksInitialised)
                {
                    SteamAPI.Shutdown();
                    Log("Steamworks shut down");
                }

                // Close the log file
                if (LogFile != null)
                {
                    Log("Closing log file");
                    LogFile.Dispose();
                    LogFile = null;
                    Log("Log file closed");
                }

                // Quit
                Log("Quitting");
            }
        }
    }
}
