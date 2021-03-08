using Dan200.Core.Assets;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Script;
using Dan200.Game.Game;
using System.Collections.Generic;

namespace Dan200.Game.Script
{
    public class ScriptController
    {
        private string m_scriptPath;
        private LevelState m_state;

        private LuaMachine m_machine;
        private List<LuaCoroutine> m_activeCoroutines;

        public ScriptController(LevelState state)
        {
            m_state = state;
            m_machine = new LuaMachine();
            m_machine.AllowByteCodeLoading = false;
            m_machine.EnforceTimeLimits = true;
            m_machine.RemoveUnsafeGlobals();

            m_activeCoroutines = new List<LuaCoroutine>();

            // Install APIs and Globals
            m_machine.SetGlobal("io", new IOAPI(m_state).ToTable());
            m_machine.SetGlobal("level", new LevelAPI(m_state).ToTable());
            if (m_state is InGameState)
            {
                m_machine.SetGlobal("game", new GameAPI((InGameState)m_state).ToTable());
                m_machine.SetGlobal("campaign", new CampaignAPI(m_state).ToTable());

                m_machine.DoString(@"do
                    local game_showDialogue = game.showDialogue
                    function game.showDialogue( character, text, modal, returnImmediately )
                        -- Create dialog
                        game_showDialogue( character, text, modal )
                        if modal ~= false and returnImmediately ~= true then
                            while not game.isDialogueReadyToContinue() do
                                yield()
                            end
                        end
                    end

                    local game_hideDialogue = game.hideDialogue
                    function game.hideDialogue()
                        -- Create dialog
                        game_hideDialogue()
                        while game.isDialogueVisible() do
                            yield()
                        end
                    end
                end", "=ScriptController.ctor");
            }
            else if (m_state is CutsceneState)
            {
                m_machine.SetGlobal("cutscene", new CutsceneAPI((CutsceneState)m_state).ToTable());
                m_machine.SetGlobal("campaign", new CampaignAPI(m_state).ToTable());
            }

            m_machine.SetGlobal("dofile", (LuaCFunction)DoFile);
            m_machine.DoString(@"do
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

				print = io.print
				yield = coroutine.yield

				function sleep( t )
	                expect( t, ""number"", 1 )
	                local l = io.getTime() + t
	                repeat
	                    yield()
	                until io.getTime() >= l
	            end				
            end", "=ScriptController.ctor");

            // Start the main script
            m_scriptPath = m_state.Level.Info.ScriptPath;
            if (m_scriptPath != null)
            {
                if (Assets.Exists<LuaScript>(m_scriptPath))
                {
                    try
                    {
                        var script = LuaScript.Get(m_scriptPath);
                        m_machine.DoString(script.Code, "@" + AssetPath.GetFileName(m_scriptPath));
                    }
                    catch (LuaError e)
                    {
                        App.Log("Lua Error: {0}", e.Message);
                    }
                }
                else
                {
                    App.Log("Error: Script {0} not found", m_scriptPath);
                }
            }
        }

        public void Dispose()
        {
            m_machine.Dispose();
            m_machine = null;
        }

        public void Update(float dt)
        {
            // Step the co-routines
            for (int i = 0; i < m_activeCoroutines.Count; ++i)
            {
                var coroutine = m_activeCoroutines[i];
                try
                {
                    coroutine.Resume(LuaArgs.Empty);
                    if (coroutine.IsFinished)
                    {
                        m_activeCoroutines.RemoveAt(i);
                        --i;
                    }
                }
                catch (LuaError e)
                {
                    App.Log("Lua Error: {0}", e.Message);
                    m_activeCoroutines.RemoveAt(i);
                    --i;
                }
            }
        }

        public bool HasFunction(string functionName)
        {
            return m_machine.GetGlobal(functionName).IsFunction();
        }

        public LuaArgs CallFunction(string functionName, LuaArgs args)
        {
            var function = m_machine.GetGlobal(functionName).GetFunction();
            return function.Call(args);
        }

        public void StartFunction(string functionName, LuaArgs args)
        {
            try
            {
                var function = m_machine.GetGlobal(functionName).GetFunction();
                var coroutine = m_machine.CreateCoroutine(function);
                coroutine.Resume(args);
                if (!coroutine.IsFinished)
                {
                    m_activeCoroutines.Add(coroutine);
                }
            }
            catch (LuaError e)
            {
                App.Log("Lua Error: {0}", e.Message);
            }
        }

        private LuaArgs DoFile(LuaArgs args)
        {
            var path = args.GetString(0);
            if (Assets.Exists<LuaScript>(path))
            {
                var script = LuaScript.Get(path);
                return m_machine.DoString(script.Code, "@" + AssetPath.GetFileName(path));
            }
            throw new LuaError("Script not found: " + path);
        }
    }
}

