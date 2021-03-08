using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using LUA = Lua.Lua;

namespace Dan200.Core.Lua
{
    public class LuaMachine : IDisposable
    {
        public static Version Version
        {
            get
            {
                return new Version(LUA.LUA_VERSION_MAJOR, LUA.LUA_VERSION_MINOR);
            }
        }

        private const int s_hookInterval = 1000;
        private const int s_firstTimeLimit = 5000000; // The number of instructions until the first timeout error is emitted
        private const int s_secondTimeLimit = 2000000; // The number of instructions in which the CPU must yield after the first timeout

        private static int s_nextMemoryLookupID = 0;
        private static Dictionary<IntPtr, LuaMachine> s_memoryLookup = new Dictionary<IntPtr, LuaMachine>();
        private static Dictionary<IntPtr, LuaMachine> s_machineLookup = new Dictionary<IntPtr, LuaMachine>();

        private static LUA.lua_Hook s_hookDelegate;
        private static LUA.lua_Alloc s_allocDelegate;
        private static LUA.lua_KFunction s_continueDelegate;
        private static Dictionary<string, LUA.lua_CFunction> s_cFunctionDelegates;

        static LuaMachine()
        {
            s_hookDelegate = new LUA.lua_Hook(Debug_Hook);
            s_allocDelegate = new LUA.lua_Alloc(Alloc);
            s_continueDelegate = new LUA.lua_KFunction(LuaFunction_Continue);
            s_cFunctionDelegates = new Dictionary<string, LUA.lua_CFunction>();
            s_cFunctionDelegates.Add("Load", Load);
            s_cFunctionDelegates.Add("LuaObject_ToString", LuaObject_ToString);
            s_cFunctionDelegates.Add("LuaObject_GC", LuaObject_GC);
            s_cFunctionDelegates.Add("LuaFunction_Call", LuaFunction_Call);
            s_cFunctionDelegates.Add("LuaFunction_GC", LuaFunction_GC);
        }

        private class ObjectLookup
        {
            private LuaMachine m_parent;
            private Dictionary<object, int> m_objectToID;
            private Dictionary<int, object> m_IDToObject;
            private List<int> m_releasedObjects;
            private int m_nextID;

            public LuaMachine Machine
            {
                get
                {
                    return m_parent;
                }
            }

            public IEnumerable<object> KnownObjects
            {
                get
                {
                    return m_objectToID.Keys;
                }
            }

            public ObjectLookup(LuaMachine parent, IntPtr state)
            {
                // Create ID table
                m_parent = parent;
                m_objectToID = new Dictionary<object, int>();
                m_IDToObject = new Dictionary<int, object>();
                m_releasedObjects = new List<int>();
                m_nextID = 1;

                try
                {
                    // Prevent OOM
                    m_parent.PreventOOM();

                    // Create anchor table
					LUA.lua_pushlstring(state, "anchors"); // 1
	                LUA.lua_createtable(state, 0, 0); // 2
					{
						LUA.lua_createtable(state, 0, 1); // 3
						LUA.lua_pushlstring(state, "__mode"); // 4
						LUA.lua_pushlstring(state, "v"); // 54
						LUA.lua_rawset(state, -3); // 3
						LUA.lua_setmetatable(state, -2); // 2
					}
					LUA.lua_rawset(state, LUA.LUA_REGISTRYINDEX); // 0

                    // Create permanent anchor table
					LUA.lua_pushlstring(state, "strong_anchors"); // 1
                    LUA.lua_createtable(state, 0, 0); // 2
                    LUA.lua_rawset(state, LUA.LUA_REGISTRYINDEX); // 0
                }
                finally
                {
                    // Allow OOM
                    parent.AllowOOM();
                }
            }

            public int StoreObjectAndValue(IntPtr state, object obj, int valueIndex, bool permanent)
            {
                var id = NewID();

                // Store object in ID table
                m_objectToID[obj] = id;
                m_IDToObject[id] = obj;

                // Store ID and value
                StoreValue(state, valueIndex, id, permanent);
                return id;
            }

            public int StoreValueOnly(IntPtr state, int valueIndex, bool permanent)
            {
                var id = NewID();
                StoreValue(state, valueIndex, id, permanent);
                return id;
            }

            private void StoreValue(IntPtr state, int valueIndex, int id, bool permanent)
            {
                try
                {
                    // Prevent OOM
                    m_parent.PreventOOM();

                    // Store value in anchor table
                    LUA.lua_pushvalue(state, valueIndex); // 11

					LUA.lua_pushlstring(state, "anchors"); // 2
					LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 2
                    LUA.lua_pushvalue(state, -2); // 3
                    LUA.lua_rawseti(state, -2, id); // 2
                    LUA.lua_pop(state, 1); // 1

                    if (permanent)
                    {
                        // Store value in permanent anchor table
						LUA.lua_pushlstring(state, "strong_anchors"); // 2
						LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 2
                        LUA.lua_pushvalue(state, -2); // 3
                        LUA.lua_rawseti(state, -2, id); // 2
                        LUA.lua_pop(state, 1); // 1
                    }

                    LUA.lua_pop(state, 1); // 0
                }
                finally
                {
                    // Allow OOM
                    m_parent.AllowOOM();
                }
            }

            public void RemoveObjectAndValue(IntPtr state, int id)
            {
                // Remove object from ID table
                if (m_IDToObject.ContainsKey(id))
                {
                    var obj = m_IDToObject[id];
                    m_IDToObject.Remove(id);
                    m_objectToID.Remove(obj);
                }

                try
                {
                    // Prevent OOM
                    m_parent.PreventOOM();

                    // Remove value from anchor table
					LUA.lua_pushlstring(state, "anchors"); // 1
					LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 1
                    LUA.lua_pushnil(state); // 2
                    LUA.lua_rawseti(state, -2, id); // 1
                    LUA.lua_pop(state, 1); // 0

                    // Remove value from permanent anchor table
					LUA.lua_pushlstring(state, "strong_anchors"); // 1
					LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 1
                    LUA.lua_pushnil(state); // 2
                    LUA.lua_rawseti(state, -2, id); // 1
                    LUA.lua_pop(state, 1); // 0
                }
                finally
                {
                    // Allow OOM
                    m_parent.AllowOOM();
                }
            }

            public object GetObjectForID(int id)
            {
                object obj;
                if (m_IDToObject.TryGetValue(id, out obj))
                {
                    return obj;
                }
                else
                {
                    return null;
                }
            }

            public bool PushValueForObject(IntPtr state, object obj) // +1|0
            {
                // Get ID from ID table
                int id;
                if (m_objectToID.TryGetValue(obj, out id))
                {
                    return PushValueForID(state, id); // 1|0
                }
                return false; // 0
            }

            public bool PushValueForID(IntPtr state, int id) // +1|0
            {
                try
                {
                    // Prevent OOM
                    m_parent.PreventOOM();

                    // Push value from anchor table
					LUA.lua_pushlstring(state, "anchors"); // 1
					LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 1
                    LUA.lua_rawgeti(state, -1, id); // 2
                    var type = LUA.lua_type(state, -1);
                    if (type != LUA.LUA_TNIL)
                    {
                        LUA.lua_remove(state, -2); // 1
                        return true;
                    }
                    else
                    {
                        LUA.lua_pop(state, 2); // 0
                        return false;
                    }
                }
                finally
                {
                    // Allow OOM
                    m_parent.AllowOOM();
                }
            }

            public void RemoveReleasedObjects(IntPtr state)
            {
                lock (m_releasedObjects)
                {
                    foreach (var id in m_releasedObjects)
                    {
                        RemoveObjectAndValue(state, id);
                    }
                    m_releasedObjects.Clear();
                }
            }

            public void ReleaseObject(int id)
            {
                lock (m_releasedObjects)
                {
                    m_releasedObjects.Add(id);
                }
            }

            // Misc

            private int NewID()
            {
                return m_nextID++;
            }
        }

        private IntPtr m_mainState;
        private IntPtr m_runningState;
        private ObjectLookup m_objectLookup;

        private Dictionary<int, LuaContinuation> m_pendingContinuations;
        private int m_nextContinuationID;

        private int m_instructionsExecutedThisTimeout;
        private bool m_firstTimeoutEmitted;

        private MemoryTracker m_memoryTracker;
        private int m_forceAllocations;
        private IntPtr m_memoryTrackerID;
        private int m_instructionsExecuted;

        public bool AllowByteCodeLoading = true;
        public bool EnforceTimeLimits = false;

        public bool Disposed
        {
            get
            {
                return m_mainState == IntPtr.Zero;
            }
        }

        public int InstructionsExecuted
        {
            get
            {
                return m_instructionsExecuted;
            }
        }

        public LuaMachine(MemoryTracker memory = null)
        {
            // Create machine
            m_memoryTracker = memory;
            m_forceAllocations = 0;
            if (m_memoryTracker != null)
            {
                var id = new IntPtr(s_nextMemoryLookupID++);
                s_memoryLookup.Add(id, this);
                m_memoryTrackerID = id;
                try
                {
                    PreventOOM();
                    m_mainState = LUA.lua_newstate(s_allocDelegate, id);
                }
                finally
                {
                    AllowOOM();
                }
            }
            else
            {
                m_memoryTrackerID = IntPtr.Zero;
                m_mainState = LUA.luaL_newstate();
            }
            s_machineLookup.Add(m_mainState, this);
            m_runningState = IntPtr.Zero;

            m_objectLookup = new ObjectLookup(this, m_mainState);
            m_pendingContinuations = new Dictionary<int, LuaContinuation>();
            m_nextContinuationID = 0;

            try
            {
                // Prevent OOM during init
                PreventOOM();

                // Install standard library
                LUA.luaL_openlibs(m_mainState);

				// Copy important globals into the registry
				// Copy load
				LUA.lua_pushlstring(m_mainState, "native_load"); // 1
				{
					LUA.lua_pushglobaltable(m_mainState); // 2
					LUA.lua_pushlstring(m_mainState, "load"); // 3
					LUA.lua_rawget(m_mainState, -2); // 3
					LUA.lua_remove(m_mainState, -2); // 2
				}
				LUA.lua_rawset(m_mainState, LUA.LUA_REGISTRYINDEX); // 0

                // Replace load with our wrapped version
                LUA.lua_pushglobaltable(m_mainState); // 1
                LUA.lua_pushlstring(m_mainState, "load"); // 2
                PushStaticCFunction(m_mainState, "Load"); // 3
				LUA.lua_rawset(m_mainState, -3); // 1
                LUA.lua_pop(m_mainState, 1); // 0

                // Create the function metatable
				LUA.lua_pushlstring(m_mainState, "luafunction_metatable"); // 1
                LUA.lua_createtable(m_mainState, 0, 1); // 2
                {
					LUA.lua_pushlstring(m_mainState, "__gc"); // 3
                    PushStaticCFunction(m_mainState, "LuaFunction_GC"); // 4
					LUA.lua_rawset(m_mainState, -3); // 2
                }
                LUA.lua_rawset(m_mainState, LUA.LUA_REGISTRYINDEX); // 0

                // Install hook function
                ResetTimeoutTimer();
                m_instructionsExecuted = 0;
                LUA.lua_sethook(m_mainState, s_hookDelegate, LUA.LUA_MASKCOUNT | LUA.LUA_MASKCALL, s_hookInterval);
            }
            finally
            {
                // Allow OOM again
                AllowOOM();
            }
        }

        public void CollectGarbage()
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }
                m_objectLookup.RemoveReleasedObjects(m_runningState);
                LUA.lua_gc(m_runningState, LUA.LUA_GCCOLLECT, 0);
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        public void RemoveUnsafeGlobals()
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Clear globals
                ClearGlobal("collectgarbage");
                DoString("debug = { traceback = debug.traceback }", "=LuaMachine.RemoveUnsafeGlobals");
                ClearGlobal("dofile");
                ClearGlobal("io");
                ClearGlobal("loadfile");
                ClearGlobal("package");
                ClearGlobal("require");
                ClearGlobal("os");
                ClearGlobal("print");
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        public void Dispose()
        {
            CheckNotDisposed();
            if (m_runningState != IntPtr.Zero)
            {
                throw new InvalidOperationException("Attempt to Dispose LuaMachine during a CFunction");
            }

            // Clear variables (prevents thread safety problems)
            var mainState = m_mainState;
            m_mainState = IntPtr.Zero;
            m_runningState = IntPtr.Zero;

            // Close the state
            LUA.lua_close(mainState);
            if (m_memoryTracker != null)
            {
                s_memoryLookup.Remove(m_memoryTrackerID);
                m_memoryTracker = null;
                m_memoryTrackerID = IntPtr.Zero;
            }
            s_machineLookup.Remove(mainState);

            // GC any dangling LuaObjects (if close did it's job, there shouldn't be any)
            foreach (var obj in m_objectLookup.KnownObjects)
            {
                if (obj is LuaObject)
                {
                    var luaObject = (LuaObject)obj;
                    if (luaObject.UnRef() == 0)
                    {
                        luaObject.Dispose();
                    }
                }
            }
            m_objectLookup = null;
        }

        public LuaCoroutine CreateCoroutine(LuaFunction function)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Create the coroutine
                var thread = LUA.lua_newthread(m_runningState); // 1

                // Push the function onto the coroutine stack, ready for resuming
                if (function.Machine != this || !m_objectLookup.PushValueForID(thread, function.ID)) // 1,1|0
                {
                    LUA.lua_pop(m_runningState, 1); // 0,0
                    throw new Exception("Could not find function");
                }

                // Store the coroutine in the registry
                var id = m_objectLookup.StoreValueOnly(m_runningState, -1, true); // This will be removed when LuaCoroutine is collected
                var coroutine = new LuaCoroutine(this, id);
                LUA.lua_pop(m_runningState, 1); // 0,1

                return coroutine;
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        internal bool IsFinished(LuaCoroutine co)
        {
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Get the coroutine
                if (co.Machine != this || !m_objectLookup.PushValueForID(m_runningState, co.ID)) // 1|0
                {
                    throw new Exception("Could not find coroutine");
                }

                // Get the status
                var thread = LUA.lua_tothread(m_runningState, -1);
                var status = LUA.lua_status(thread);
                switch (status)
                {
                    case LUA.LUA_OK:
                        {
                            // Running or finished
                            var finished = (LUA.lua_gettop(thread) == 0);
                            LUA.lua_pop(m_runningState, 1); // 0
                            return finished;
                        }
                    case LUA.LUA_YIELD:
                        {
                            // Suspended
                            LUA.lua_pop(m_runningState, 1); // 0
                            return false;
                        }
                    default:
                        {
                            // Errored
                            LUA.lua_pop(m_runningState, 1); // 0
                            return true;
                        }
                }
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        internal LuaArgs Resume(LuaCoroutine co, LuaArgs args)
        {
            var oldState = m_runningState;
            try
            {
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                IntPtr thread;
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Get the coroutine
                    if (co.Machine != this || !m_objectLookup.PushValueForID(m_runningState, co.ID)) // 1
                    {
                        LUA.lua_pop(m_runningState, 1); // 0
                        throw new Exception("Could not find coroutine");
                    }

                    // Push the arguments onto the coroutine stack
                    thread = LUA.lua_tothread(m_runningState, -1);
                    PushValues(thread, args, m_objectLookup); // 1, 1 + args.Length
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }

                // Resume the coroutine
                int resumeResult = LUA.lua_resume(thread, m_runningState, args.Length); // 1, numResults|??? + 1

                try
                {
                    // Prevent OOM
                    PreventOOM();

                    if (resumeResult == LUA.LUA_OK || resumeResult == LUA.LUA_YIELD)
                    {
                        // Return the results
                        var numResults = LUA.lua_gettop(thread);
                        var results = PopValues(thread, numResults, m_objectLookup); // 1, 0
                        LUA.lua_pop(m_runningState, 1); // 0, 0
                        return results;
                    }
                    else
                    {
                        // Throw the error
                        var e = PopValue(thread, m_objectLookup); // 1, ???
                        LUA.lua_settop(thread, 0); // 1, 0
                        LUA.lua_pop(m_runningState, 1); // 0, 0
                        throw new LuaError(e, 0);
                    }
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }
            }
            finally
            {
                m_runningState = oldState;
            }
        }

        public LuaFunction LoadString(string lua, string chunkName)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Load the function
                int loadError = LUA.luaL_loadbufferx(m_runningState, lua, chunkName, "t"); // 1
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Check for errors
                    if (loadError != LUA.LUA_OK)
                    {
                        var e = PopValue(m_runningState, m_objectLookup); // 0
                        throw new LuaError(e, 0);
                    }

                    // Return the function
                    return PopValue(m_runningState, m_objectLookup).GetFunction(); // 0
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }
            }
            finally
            {
                m_runningState = oldState;
            }
        }

        internal void Release(LuaCoroutine coroutine)
        {
            var lookup = m_objectLookup;
            if (lookup != null)
            {
                // Release the coroutine
                lookup.ReleaseObject(coroutine.ID);
            }
        }

        internal LuaArgs Call(LuaFunction function, LuaArgs args)
        {
            var oldState = m_runningState;
            try
            {
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                int top = LUA.lua_gettop(m_runningState);
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Get the function
                    if (function.Machine != this || !m_objectLookup.PushValueForID(m_runningState, function.ID)) // 1|0
                    {
                        throw new Exception("Could not find function");
                    }

                    // Push the arguments
                    PushValues(m_runningState, args, m_objectLookup); // 1 + args.Length
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }

                // Call the function
                int callError = LUA.lua_pcall(m_runningState, args.Length, LUA.LUA_MULTRET, 0); // numResults|1

                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Check for errors
                    if (callError != LUA.LUA_OK)
                    {
                        var e = PopValue(m_runningState, m_objectLookup); // 0
                        throw new LuaError(e, 0);
                    }

                    // Return the results
                    int numResults = LUA.lua_gettop(m_runningState) - top;
                    return PopValues(m_runningState, numResults, m_objectLookup); // 0
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }
            }
            finally
            {
                m_runningState = oldState;
            }
        }

        internal LuaArgs CallAsync(LuaFunction function, LuaArgs args, LuaContinuation continuation)
        {
			if (m_runningState == IntPtr.Zero)
            {
                throw new InvalidOperationException("Attempt to call a function asynchronously from outside a CFunction");
            }
            throw new LuaAsyncCall(function, args, continuation);
        }

        internal void Release(LuaFunction function)
        {
            var lookup = m_objectLookup;
            if (lookup != null)
            {
                // Release the function
                lookup.ReleaseObject(function.ID);
            }
        }

        public LuaArgs DoString(string lua, string chunkName)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Load the function
                int top = LUA.lua_gettop(m_runningState);
                int loadError = LUA.luaL_loadbufferx(m_runningState, lua, chunkName, "t"); // 1
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Handle errors
                    if (loadError != LUA.LUA_OK)
                    {
                        var e = PopValue(m_runningState, m_objectLookup); // 0
                        throw new LuaError(e, 0);
                    }
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }

                // Call the function
                int callError = LUA.lua_pcall(m_runningState, 0, LUA.LUA_MULTRET, 0); // numResults|1
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Handle errors
                    if (callError != LUA.LUA_OK)
                    {
                        var e = PopValue(m_runningState, m_objectLookup); // 0
                        throw new LuaError(e, 0);
                    }

                    // Return the results
                    int numResults = LUA.lua_gettop(m_runningState) - top;
                    return PopValues(m_runningState, numResults, m_objectLookup); // 0
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }
            }
            finally
            {
                m_runningState = oldState;
            }
        }

        public void SetGlobal(string name, LuaValue value)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

				// Set the global
				LUA.lua_pushglobaltable(m_runningState); // 1
				LUA.lua_pushlstring(m_runningState, name); // 2
                PushValue(m_runningState, value, m_objectLookup); // 3
				LUA.lua_rawset(m_runningState, -3); // 1
				LUA.lua_pop(m_runningState, 1); // 0
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        public void ClearGlobal(string name)
        {
            SetGlobal(name, LuaValue.Nil);
        }

        public LuaValue GetGlobal(string name)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

				// Get the global
				LUA.lua_pushglobaltable(m_runningState); // 1
				LUA.lua_pushlstring(m_runningState, name); // 2
                LUA.lua_rawget(m_runningState, -2); // 2
				LUA.lua_remove(m_runningState, -2); // 1
                return PopValue(m_runningState, m_objectLookup); // 0
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        private void CheckNotDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("LuaMachine");
            }
        }

        private static void PushNil(IntPtr state)
        {
            LUA.lua_pushnil(state); // 1
        }

        private static void PushBool(IntPtr state, bool value)
        {
            LUA.lua_pushboolean(state, value); // 1
        }

#if LUA_32BITS
		private static void PushNumber (IntPtr state, float value)
		{
			LUA.lua_pushnumber (state, value); // 1
		}

		private static void PushInteger (IntPtr state, int value)
		{
			LUA.lua_pushinteger (state, value); // 1
		}
#else
        private static void PushNumber(IntPtr state, double value)
        {
            LUA.lua_pushnumber(state, value); // 1
        }

        private static void PushInteger(IntPtr state, long value)
        {
            LUA.lua_pushinteger(state, value); // 1
        }
#endif

        private static void PushString(IntPtr state, string value)
        {
            if (value != null)
            {
                LUA.lua_pushlstring(state, value); // 1
            }
            else
            {
                LUA.lua_pushnil(state); // 1
            }
        }

        private static void PushByteString(IntPtr state, byte[] value)
        {
            if (value != null)
            {
                LUA.lua_pushlstring(state, value); // 1
            }
            else
            {
                LUA.lua_pushnil(state); // 1
            }
        }

        private static void PushTable(IntPtr state, LuaTable value, ObjectLookup objectLookup)
        {
            if (value != null)
            {
                var keys = value.Keys;
                LUA.lua_createtable(state, 0, value.Count); // 1
                foreach (var k in keys)
                {
                    var v = value[k];
                    PushValue(state, k, objectLookup); // 2
                    PushValue(state, v, objectLookup); // 3
                    LUA.lua_rawset(state, -3); // 1
                }
            }
            else
            {
                LUA.lua_pushnil(state); // 1
            }
        }

        private static void PushUserdata(IntPtr state, IntPtr value)
        {
            LUA.lua_pushlightuserdata(state, value); // 1
        }

        private class LuaObjectMethodCaller
        {
            private Type m_type;
            private MethodInfo m_method;
            private object[] m_args;

            public LuaObjectMethodCaller(Type type, MethodInfo method)
            {
                m_type = type;
                m_method = method;
                m_args = new object[1];
            }

            public LuaArgs CallMethod(LuaArgs args)
            {
                var o = args.GetObject(0, m_type);
                try
                {
                    m_args[0] = args.Select(1); // Saves an allocation
                    return (LuaArgs)m_method.Invoke(o, m_args);
                }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }
            }
        }

        private static void PushTypeMetatable(IntPtr state, Type type, ObjectLookup objectLookup) // +1
        {
            // See if the metatable is already registered
            if (objectLookup.PushValueForObject(state, type)) // 1|0
            {
                return;
            }

            // Get the type attribute
            string typeName;
            bool exposeType;
            object[] attributes = type.GetCustomAttributes(typeof(LuaTypeAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                var attribute = (LuaTypeAttribute)attributes[0];
                if (attribute.CustomName != null)
                {
                    typeName = attribute.CustomName;
                }
                else
                {
                    typeName = type.Name;
                }
                exposeType = attribute.ExposeType;
            }
            else
            {
                throw new InvalidDataException("Type " + type.Name + " is missing LuaTypeAttribute");
            }

            // Create the metatable
            LUA.lua_createtable(state, 0, exposeType ? 4 : 3); // 1

            if (exposeType)
            {
                // Setup __type
                LUA.lua_pushlstring(state, "__type"); // 2
                LUA.lua_pushlstring(state, typeName); // 3
				LUA.lua_rawset(state, -3); // 1
            }

            // Setup __index
			LUA.lua_pushlstring(state, "__index"); // 2
            LUA.lua_createtable(state, 0, 0); // 3
            {
                // Add methods
                MethodInfo[] methods = type.GetMethods();
                for (int i = 0; i < methods.Length; ++i)
                {
                    var method = methods[i];
                    var name = method.Name;
                    object[] methodAttributes = method.GetCustomAttributes(typeof(LuaMethodAttribute), true);
                    if (methodAttributes != null && methodAttributes.Length > 0)
                    {
                        var attribute = (LuaMethodAttribute)methodAttributes[0];
                        if (attribute.CustomName != null)
                        {
                            name = attribute.CustomName;
                        }
                        if (exposeType || name != "getType")
                        {
                            var caller = new LuaObjectMethodCaller(type, method);
							LUA.lua_pushlstring(state, name); // 4
                            PushCFunction(state, (LuaCFunction)caller.CallMethod, objectLookup); // 5
                            LUA.lua_rawset(state, -3); // 3
                        }
                    }
                }
            }
			LUA.lua_rawset(state, -3); // 1

			// Setup __tostring
			LUA.lua_pushlstring(state, "__tostring"); // 2
            PushStaticCFunction(state, "LuaObject_ToString"); // 3
			LUA.lua_rawset(state, -3); // 1

			// Setup __gc
			LUA.lua_pushlstring(state, "__gc"); // 2
            PushStaticCFunction(state, "LuaObject_GC"); // 3
            LUA.lua_rawset(state, -3); // 1

            // Store the metatable in the registry
            objectLookup.StoreObjectAndValue(state, type, -1, true);
        }

        private static void PushLuaObject(IntPtr state, LuaObject obj, ObjectLookup objectLookup) // +1
        {
            if (obj == null)
            {
                LUA.lua_pushnil(state); // 1
            }
            else if (!objectLookup.PushValueForObject(state, obj)) // 1|0
            {
                // Reference the object
                obj.Ref();

                // Create a new userdata
                var ud = LUA.lua_newuserdata(state, new IntPtr(sizeof(int))); // 1

                // Set the type metatable on the userdata
                PushTypeMetatable(state, obj.GetType(), objectLookup); // 2
                LUA.lua_setmetatable(state, -2); // 1

                // Store the userdata and store the ID in the userdata
                var id = objectLookup.StoreObjectAndValue(state, obj, -1, false);
                Marshal.WriteInt32(ud, id);
            }
        }

        private static void PushCFunction(IntPtr state, LuaCFunction function, ObjectLookup objectLookup) // +1
        {
            if (function == null)
            {
                LUA.lua_pushnil(state); // 1
            }
            else if (!objectLookup.PushValueForObject(state, function)) // 1|0
            {
                // Create a userdata
                var ud = LUA.lua_newuserdata(state, new IntPtr(sizeof(int))); // 1

                // Set the luafunction metatable on the userdata
				LUA.lua_pushlstring(state, "luafunction_metatable"); // 2
				LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 2
                LUA.lua_setmetatable(state, -2); // 1

                // Create a closure
                PushStaticCClosure(state, "LuaFunction_Call", 1); // 1

                // Store the closure and store the ID in the userdata
                var id = objectLookup.StoreObjectAndValue(state, function, -1, false);
                Marshal.WriteInt32(ud, id);
            }
        }

        private static void PushFunction(IntPtr state, LuaFunction function, ObjectLookup objectLookup) // +1
        {
            if (function == null)
            {
                LUA.lua_pushnil(state); // 1
            }
            else if (function.Machine != objectLookup.Machine || !objectLookup.PushValueForID(state, function.ID)) // 1|0
            {
                LUA.lua_pushnil(state); // 1 (should never happen)
            }
        }

        private static void PushCoroutine(IntPtr state, LuaCoroutine coroutine, ObjectLookup objectLookup) // +1
        {
            if (coroutine == null)
            {
                LUA.lua_pushnil(state); // 1
            }
            else if (coroutine.Machine != objectLookup.Machine || !objectLookup.PushValueForID(state, coroutine.ID)) // 1|0
            {
                LUA.lua_pushnil(state); // 1
            }
        }

        private static LUA.lua_CFunction GetStaticCFunction(string name)
        {
            LUA.lua_CFunction result;
            if (s_cFunctionDelegates.TryGetValue(name, out result))
            {
                return result;
            }
            else
            {
                throw new Exception("Static method " + name + " is not registered!");
            }
        }

        private static void PushStaticCFunction(IntPtr state, string name)
        {
            LUA.lua_pushcfunction(state, GetStaticCFunction(name));
        }

        private static void PushStaticCClosure(IntPtr state, string name, int count)
        {
            LUA.lua_pushcclosure(state, GetStaticCFunction(name), count);
        }

        private static void PushValue(IntPtr state, LuaValue value, ObjectLookup objectLookup)
        {
            if (value.IsBool())
            {
                PushBool(state, value.GetBool());
            }
            else if (value.IsNumber())
            {
#if LUA_32BITS
				if (value.IsInteger ()) {
					PushInteger (state, value.GetInt ());
				} else {
					PushNumber (state, value.GetFloat ());
				}
#else
                if (value.IsInteger())
                {
                    PushInteger(state, value.GetLong());
                }
                else
                {
                    PushNumber(state, value.GetDouble());
                }
#endif
            }
            else if (value.IsString())
            {
				if (value.IsByteString())
				{
					PushByteString(state, value.GetByteString());
				}
				else
				{
					PushString(state, value.GetString());
				}
            }
            else if (value.IsTable())
            {
                PushTable(state, value.GetTable(), objectLookup);
            }
            else if (value.IsObject())
            {
                PushLuaObject(state, value.GetObject(), objectLookup);
            }
            else if (value.IsCFunction())
            {
                PushCFunction(state, value.GetCFunction(), objectLookup);
            }
            else if (value.IsUserdata())
            {
                PushUserdata(state, value.GetUserdata());
            }
            else if (value.IsFunction())
            {
                PushFunction(state, value.GetFunction(), objectLookup);
            }
            else if (value.IsCoroutine())
            {
                PushCoroutine(state, value.GetCoroutine(), objectLookup);
            }
            else
            {
                PushNil(state);
            }
        }

        private static void PushValues(IntPtr state, LuaArgs args, ObjectLookup lookup)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                var value = args[i];
                PushValue(state, value, lookup); // i+1
            } // args.Length
        }

        private static LuaValue PopValue(IntPtr state, ObjectLookup objectLookup)
        {
            // 1
            var result = GetValue(state, -1, objectLookup);
            LUA.lua_pop(state, 1); // 0
            return result;
        }

        private static LuaValue GetValue(IntPtr state, int index, ObjectLookup objectLookup)
        {
            // Get the value
            bool tableTrackerCreated = false;
            var result = GetValueImpl(state, index, objectLookup, ref tableTrackerCreated);

            if (tableTrackerCreated)
            {
				// Flush the tracker table
				LUA.lua_pushlstring(state, "tables_seen"); // 1
                LUA.lua_pushnil(state); // 2
				LUA.lua_rawset(state, LUA.LUA_REGISTRYINDEX); // 0
            }
            return result;
        }

        private static LuaValue GetValueImpl(IntPtr state, int index, ObjectLookup objectLookup, ref bool io_tableTrackerCreated)
        {
            int type = LUA.lua_type(state, index);
            switch (type)
            {
                case LUA.LUA_TNIL:
                case LUA.LUA_TNONE:
                default:
                    {
                        return LuaValue.Nil;
                    }
                case LUA.LUA_TBOOLEAN:
                    {
                        var b = LUA.lua_toboolean(state, index);
                        return b ? LuaValue.True : LuaValue.False;
                    }
                case LUA.LUA_TNUMBER:
                    {
                        if (LUA.lua_isinteger(state, index))
                        {
                            var l = LUA.lua_tointeger(state, index);
                            return new LuaValue(l);
                        }
                        else
                        {
                            var d = LUA.lua_tonumber(state, index);
                            return new LuaValue(d);
                        }
                    }
                case LUA.LUA_TSTRING:
                    {
                        var ptr = LUA.lua_tostring(state, index);
                        return new LuaValue(ptr.GetBytes());
                    }
                case LUA.LUA_TTABLE:
                    {
                        // Get the table
                        LUA.lua_pushvalue(state, index); // 1

                        // Get or create a table to track tables seen
                        if (io_tableTrackerCreated)
                        {
							LUA.lua_pushlstring(state, "tables_seen"); // 2
							LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 2
                        }
                        else
                        {
                            LUA.lua_createtable(state, 0, 1); // 2
							LUA.lua_pushlstring(state, "tables_seen"); // 3
                            LUA.lua_pushvalue(state, -2); // 4
                            LUA.lua_rawset(state, LUA.LUA_REGISTRYINDEX); // 2
                            io_tableTrackerCreated = true;
                        }

                        // Check the table hasn't already been seen
                        LUA.lua_pushvalue(state, -2); // 3
                        if (LUA.lua_rawget(state, -2) != LUA.LUA_TNIL)
                        { // 3
                          // Return nil for recursed tables
                            LUA.lua_pop(state, 3); // 0
                            return LuaValue.Nil;
                        }
                        else
                        {
                            LUA.lua_pop(state, 1); // 2
                        }

                        // Remember the table
                        LUA.lua_pushvalue(state, -2); // 3
                        LUA.lua_pushboolean(state, true); // 4
						LUA.lua_rawset(state, -3); // 2
                        LUA.lua_pop(state, 1); // 1

                        var table = new LuaTable();
                        LUA.lua_pushnil(state); // 2
                        while (LUA.lua_next(state, -2) != 0)
                        { // 3|1
                            var k = GetValueImpl(state, -2, objectLookup, ref io_tableTrackerCreated);
                            var v = GetValueImpl(state, -1, objectLookup, ref io_tableTrackerCreated);
                            if (!k.IsNil() && !v.IsNil())
                            {
                                table[k] = v;
                            }
                            LUA.lua_pop(state, 1); // 2
                        }
                        LUA.lua_pop(state, 1); // 0
                        return new LuaValue(table);
                    }
                case LUA.LUA_TUSERDATA:
                    {
                        var ud = LUA.lua_touserdata(state, index);
                        var id = Marshal.ReadInt32(ud);
                        var obj = objectLookup.GetObjectForID(id);
                        if (obj is LuaObject)
                        {
                            return new LuaValue((LuaObject)obj);
                        }
                        else
                        {
                            return new LuaValue(IntPtr.Zero); // Should never happen
                        }
                    }
                case LUA.LUA_TLIGHTUSERDATA:
                    {
                        var ud = LUA.lua_touserdata(state, index);
                        return new LuaValue(ud);
                    }
                case LUA.LUA_TFUNCTION:
                    {
                        var id = objectLookup.StoreValueOnly(state, index, true); // This will be removed when LuaFunction is collected
                        var function = new LuaFunction(objectLookup.Machine, id);
                        return new LuaValue(function);
                    }
                case LUA.LUA_TTHREAD:
                    {
                        var id = objectLookup.StoreValueOnly(state, index, true); // This will be removed when LuaCoroutine is collected
                        var coroutine = new LuaCoroutine(objectLookup.Machine, id);
                        return new LuaValue(coroutine);
                    }
            }
        }

        private static LuaArgs PopValues(IntPtr state, int count, ObjectLookup objectLookup)
        {
            if (count == 0)
            {
                return LuaArgs.Empty;
            }
            else if (count == 1)
            {
                var arg0 = PopValue(state, objectLookup);
                return new LuaArgs(arg0);
            }
            else if (count == 2)
            {
                var arg1 = PopValue(state, objectLookup);
                var arg0 = PopValue(state, objectLookup);
                return new LuaArgs(arg0, arg1);
            }
            else if (count == 3)
            {
                var arg2 = PopValue(state, objectLookup);
                var arg1 = PopValue(state, objectLookup);
                var arg0 = PopValue(state, objectLookup);
                return new LuaArgs(arg0, arg1, arg2);
            }
            else if (count == 4)
            {
                var arg3 = PopValue(state, objectLookup);
                var arg2 = PopValue(state, objectLookup);
                var arg1 = PopValue(state, objectLookup);
                var arg0 = PopValue(state, objectLookup);
                return new LuaArgs(arg0, arg1, arg2, arg3);
            }
            else
            {
                var extraArgs = new LuaValue[count - 4];
                for (int i = extraArgs.Length - 1; i >= 0; --i)
                {
                    extraArgs[i] = PopValue(state, objectLookup);
                }
                var arg3 = PopValue(state, objectLookup);
                var arg2 = PopValue(state, objectLookup);
                var arg1 = PopValue(state, objectLookup);
                var arg0 = PopValue(state, objectLookup);
                return new LuaArgs(arg0, arg1, arg2, arg3, extraArgs);
            }
        }

        private static LuaMachine LookupMachine(IntPtr state)
        {
            // Lookup the machine from the current state
            LuaMachine result;
            if (s_machineLookup.TryGetValue(state, out result))
            {
                return result;
            }

            // If that fails, get the main state
            LUA.lua_rawgeti(state, LUA.LUA_REGISTRYINDEX, LUA.LUA_RIDX_MAINTHREAD); // 1
            var mainState = LUA.lua_tothread(state, -1);
            LUA.lua_pop(state, 1); // 0

            // Lookup the machine from the main state
            if (s_machineLookup.TryGetValue(mainState, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        //[MonoPInvokeCallback (typeof (KLua.lua_CFunction))]
        private static int Load(IntPtr state)
        {
            ObjectLookup objectLookup = null;
            try
            {
                // Get the machine
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                // Pass all arguments to the native function
                int argumentCount = LUA.lua_gettop(state);
                try
                {
                    // Prevent OOM
                    machine.PreventOOM();

                    // Repush arguments, with modifications
					LUA.lua_pushlstring(state, "native_load"); // argumentCount + 1
					LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // argumentCount + 1
                    if (argumentCount >= 1)
                    {
                        LUA.lua_pushvalue(state, 1); // argumentCount + 2
                    }
                    else
                    {
                        LUA.lua_pushnil(state); // argumentCount + 2
                    }
                    if (argumentCount >= 2)
                    {
                        LUA.lua_pushvalue(state, 2); // argumentCount + 3
                    }
                    else
                    {
                        LUA.lua_pushnil(state); // argumentCount + 3
                    }
                    if (argumentCount >= 3)
                    {
                        if (machine.AllowByteCodeLoading)
                        {
                            LUA.lua_pushvalue(state, 3); // argumentCount + 4
                        }
                        else
                        {
                            int type = LUA.lua_type(state, 3);
                            if (type == LUA.LUA_TNIL)
                            {
                                LUA.lua_pushlstring(state, "t"); // argumentCount + 4
                            }
                            else if (type == LUA.LUA_TSTRING)
                            {
                                var ptr = LUA.lua_tostring(state, 3);
                                var mode = ptr.Decode();
                                if (mode != "t")
                                {
                                    return LUA.luaL_error(state, "binary chunk loading prohibited");
                                }
                                LUA.lua_pushlstring(state, "t");  // argumentCount + 4
                            }
                            else
                            {
                                var typeName = LUA.lua_typename(state, type).Decode();
                                return LUA.luaL_error(state, "bad argument #3 to 'load' (string expected, got " + typeName + ")");
                            }
                        }
                    }
                    else
                    {
                        if (machine.AllowByteCodeLoading)
                        {
                            LUA.lua_pushnil(state); // argumentCount + 4
                        }
                        else
                        {
                            LUA.lua_pushlstring(state, "t"); // argumentCount + 4
                        }
                    }
                    if (argumentCount >= 4)
                    {
                        LUA.lua_pushvalue(state, 4); // argumentCount + 5
                    }
                }
                finally
                {
                    // Allow OOM
                    machine.AllowOOM();
                }

                // Call and propogate the error
                int loadError = LUA.lua_pcall(state, (argumentCount >= 4) ? 4 : 3, 2, 0); // argumentCount + 2|1
                if (loadError != LUA.LUA_OK)
                {
                    return LUA.lua_error(state); // 1
                }

                return 2;
            }
            catch (Exception e)
            {
                return EmitLuaError(state, e, objectLookup);
            }
        }

        //[MonoPInvokeCallback (typeof (KLua.lua_CFunction))]
        private static int LuaObject_ToString(IntPtr state)
        {
            ObjectLookup objectLookup = null;
            try
            {
                // Get the object
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                int type = LUA.lua_type(state, 1);
                if (type == LUA.LUA_TUSERDATA)
                {
                    var ud = LUA.lua_touserdata(state, 1);
                    var id = Marshal.ReadInt32(ud);
                    var obj = objectLookup.GetObjectForID(id);
                    if (obj != null && obj is LuaObject)
                    {
                        // Get and push the string
                        string result = null;
                        var luaObject = (LuaObject)obj;
                        var oldState = machine.m_runningState;
                        try
                        {
                            machine.m_runningState = state;
                            result = luaObject.ToString();
                        }
                        finally
                        {
                            machine.m_runningState = oldState;
                        }
                        try
                        {
                            machine.PreventOOM();
                            PushString(state, result); // 1
                        }
                        finally
                        {
                            machine.AllowOOM();
                        }
                        return 1;
                    }
                }
                throw new LuaError("Expected object, got " + LUA.lua_typename(state, type).Decode());
            }
            catch (Exception e)
            {
                return EmitLuaError(state, e, objectLookup);
            }
        }

        //[MonoPInvokeCallback (typeof (KLua.lua_CFunction))]
        private static int LuaObject_GC(IntPtr state)
        {
            ObjectLookup objectLookup = null;
            try
            {
                // Get the object
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                int type = LUA.lua_type(state, 1);
                if (type == LUA.LUA_TUSERDATA)
                {
                    var ud = LUA.lua_touserdata(state, 1);
                    var id = Marshal.ReadInt32(ud);
                    var obj = objectLookup.GetObjectForID(id);
                    if (obj != null && obj is LuaObject)
                    {
                        // Unref and possibly dispose the object
                        var luaObject = (LuaObject)obj;
                        if (luaObject.UnRef() == 0)
                        {
                            var oldState = machine.m_runningState;
                            try
                            {
                                machine.m_runningState = state;
                                luaObject.Dispose();
                            }
                            finally
                            {
                                machine.m_runningState = oldState;
                            }
                        }
                        objectLookup.RemoveObjectAndValue(state, id);
                        return 0;
                    }
                }
                throw new LuaError("Expected object, got " + LUA.lua_typename(state, type).Decode());
            }
            catch (Exception)
            {
                return 0;
            }
        }

        //[MonoPInvokeCallback (typeof (KLua.lua_CFunction))]
        private static int LuaFunction_Call(IntPtr state)
        {
            ObjectLookup objectLookup = null;
            try
            {
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                // Get the function
                int index = LUA.lua_upvalueindex(1);
                int type = LUA.lua_type(state, index);
                if (type == LUA.LUA_TUSERDATA)
                {
                    var ud = LUA.lua_touserdata(state, index);
                    var id = Marshal.ReadInt32(ud);
                    var obj = objectLookup.GetObjectForID(id);
                    if (obj != null && obj is LuaCFunction)
                    {
                        // Pop the arguments
                        var function = (LuaCFunction)obj;
                        LuaArgs arguments;
                        try
                        {
                            machine.PreventOOM();
                            int argumentCount = LUA.lua_gettop(state);
                            arguments = PopValues(state, argumentCount, objectLookup); // 0
                        }
                        finally
                        {
                            machine.AllowOOM();
                        }

                        LuaArgs results;
                        try
                        {
                            // Call the function
                            var oldState = machine.m_runningState;
                            try
                            {
                                machine.m_runningState = state;
                                results = function.Invoke(arguments);
                            }
                            finally
                            {
                                machine.m_runningState = oldState;
                            }
                        }
                        catch (LuaAsyncCall c)
                        {
                            // Get the function
                            if (c.Function.Machine != machine || !objectLookup.PushValueForID(state, c.Function.ID)) // 1|0
                            {
                                throw new Exception("Could not find function");
                            }

                            // Push the arguments
                            try
                            {
                                machine.PreventOOM();
                                PushValues(state, c.Arguments, objectLookup); // 1 + c.Arguments.Length
                            }
                            finally
                            {
                                machine.AllowOOM();
                            }

							// Store the continuationn
							int nextContinuationID;
							if (c.Continuation != null)
							{
								nextContinuationID = machine.m_nextContinuationID++;
								machine.m_pendingContinuations.Add(nextContinuationID, c.Continuation);
							}
							else
							{
								nextContinuationID = -1;
							}

                            // Call the function
                            int callResult = LUA.lua_pcallk(state, c.Arguments.Length, LUA.LUA_MULTRET, 0, new IntPtr(nextContinuationID), s_continueDelegate); // numResults
                            return LuaFunction_Continue(state, callResult, new IntPtr(nextContinuationID));
                        }
                        catch (LuaYield y)
                        {
                            // Push the results
                            try
                            {
                                machine.PreventOOM();
                                PushValues(state, y.Results, objectLookup); // y.results.Length
                            }
                            finally
                            {
                                machine.AllowOOM();
                            }

                            // Store the continuation
                            var continuationID = machine.m_nextContinuationID++;
                            machine.m_pendingContinuations.Add(continuationID, y.Continuation);

                            // Yield
                            return LUA.lua_yieldk(state, y.Results.Length, new IntPtr(continuationID), s_continueDelegate);
                        }

                        // Push the results
                        try
                        {
                            machine.PreventOOM();
                            PushValues(state, results, objectLookup); // results.Length
                            return results.Length;
                        }
                        finally
                        {
                            machine.AllowOOM();
                        }
                    }
                }
                throw new LuaError("Expected function, got " + LUA.lua_typename(state, type).Decode());
            }
            catch (Exception e)
            {
                return EmitLuaError(state, e, objectLookup);
            }
        }

        //[MonoPInvokeCallback (typeof (KLua.lua_KFunction))]
        private static int LuaFunction_Continue(IntPtr state, int status, IntPtr ctx)
        {
            ObjectLookup objectLookup = null;
            try
            {
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

				// Get the continuation
				var continuationID = ctx.ToInt32();
				LuaContinuation continuation;
				if (continuationID >= 0)
				{
					continuation = machine.m_pendingContinuations[continuationID];
					machine.m_pendingContinuations.Remove(continuationID);
				}
				else
				{
					continuation = null;
				}

                if (status == LUA.LUA_OK || status == LUA.LUA_YIELD)
                {
					int argumentCount = LUA.lua_gettop(state);
					if (continuation == null)
					{
						// Return the values directly
						return argumentCount;
					}
					else
					{
						// Pop the arguments
						LuaArgs arguments;
						try
						{
							machine.PreventOOM();
							arguments = PopValues(state, argumentCount, objectLookup); // 0
						}
						finally
						{
							machine.AllowOOM();
						}

						LuaArgs results;
						try
						{
							// Call the continuation
							var oldState = machine.m_runningState;
							try
							{
								machine.m_runningState = state;
								results = continuation.Invoke(arguments);
							}
							finally
							{
								machine.m_runningState = oldState;
							}
						}
						catch (LuaAsyncCall c)
						{
							// Get the function
							if (c.Function.Machine != machine || !objectLookup.PushValueForID(state, c.Function.ID)) // 1|0
							{
								throw new Exception("Could not find function");
							}

							// Push the arguments
							try
							{
								machine.PreventOOM();
								PushValues(state, c.Arguments, objectLookup); // 1 + c.Arguments.Length
							}
							finally
							{
								machine.AllowOOM();
							}

							// Store the next continuation
							int nextContinuationID;
							if (c.Continuation != null)
							{
								nextContinuationID = machine.m_nextContinuationID++;
								machine.m_pendingContinuations.Add(nextContinuationID, c.Continuation);
							}
							else
							{
								nextContinuationID = -1;
							}

							// Call the function
							int callResult = LUA.lua_pcallk(state, c.Arguments.Length, LUA.LUA_MULTRET, 0, new IntPtr(nextContinuationID), s_continueDelegate); // numResults|1
							return LuaFunction_Continue(state, callResult, new IntPtr(nextContinuationID));
						}
						catch (LuaYield y)
						{
							// Push the results
							try
							{
								machine.PreventOOM();
								PushValues(state, y.Results, objectLookup); // y.results.Length
							}
							finally
							{
								machine.AllowOOM();
							}

							// Store the next continuation
							int nextContinuationID = machine.m_nextContinuationID++;
							machine.m_pendingContinuations.Add(nextContinuationID, y.Continuation);

							// Yield
							return LUA.lua_yieldk(state, y.Results.Length, new IntPtr(nextContinuationID), s_continueDelegate);
						}

						// Push the results
						try
						{
							machine.PreventOOM();
							PushValues(state, results, objectLookup); // results.Length
							return results.Length;
						}
						finally
						{
							machine.AllowOOM();
						}
					}
                }
                else
                {
                    // Propogate the error
                    return LUA.lua_error(state); // 0
                }
            }
            catch (Exception e)
            {
                return EmitLuaError(state, e, objectLookup);
            }
        }

        //[MonoPInvokeCallback (typeof (KLua.lua_CFunction))]
        private static int LuaFunction_GC(IntPtr state)
        {
            ObjectLookup objectLookup = null;
            try
            {
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                // Get the function
                int type = LUA.lua_type(state, 1);
                if (type == LUA.LUA_TUSERDATA)
                {
                    var ud = LUA.lua_touserdata(state, 1);
                    var id = Marshal.ReadInt32(ud);
                    var obj = objectLookup.GetObjectForID(id);
                    if (obj != null && obj is LuaCFunction)
                    {
                        // Remove the function
                        objectLookup.RemoveObjectAndValue(state, id);
                        return 0;
                    }
                }
                throw new LuaError("Expected function, got " + LUA.lua_typename(state, type).Decode());
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static int EmitLuaError(IntPtr state, Exception e, ObjectLookup objectLookup)
        {
            if (e is LuaError)
            {
                var luaError = (LuaError)e;
                var value = luaError.Value;
                if (value.IsString())
                {
                    return LUA.luaL_error(state, value.GetString(), luaError.Level); // 0
                }
                else
                {
                    try
                    {
                        objectLookup.Machine.PreventOOM();
                        if (objectLookup != null || value.IsNumber() || value.IsBool() || value.IsNil())
                        {
                            PushValue(state, value, objectLookup); // 1
                        }
                        else
                        {
                            PushNil(state); // 1
                        }
                    }
                    finally
                    {
                        objectLookup.Machine.AllowOOM();
                    }
                    return LUA.lua_error(state); // 0
                }
            }
            else
            {
                var message = "C# Exception Thrown: " + e.GetType().FullName;
                if (e.Message != null)
                {
                    message += "\n" + e.Message;
                }
                //if ( e.StackTrace != null ) {
                //	message += "\n" + e.StackTrace;
                //}
                return LUA.luaL_error(state, message); // 0
            }
        }

        //[MonoPInvokeCallback (typeof (KLua.lua_Hook))]
        private static void Debug_Hook(IntPtr state, ref LUA.lua_Debug ar)
        {
            try
            {
                var machine = LookupMachine(state);
                if (machine != null)
                {
                    if (ar.eventCode == LUA.LUA_HOOKCOUNT)
                    {
                        machine.m_instructionsExecuted += s_hookInterval;
                        machine.m_instructionsExecutedThisTimeout += s_hookInterval;
                    }
                    if (machine.CheckTimeout())
                    {
                        LUA.luaL_error(state, "Timeout");
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void PreventOOM()
        {
            m_forceAllocations++;
        }

        private void AllowOOM()
        {
            m_forceAllocations--;
        }

        //[MonoPInvokeCallback (typeof (KLua.lua_Alloc))]
        private static IntPtr Alloc(IntPtr ud, IntPtr ptr, IntPtr osize, IntPtr nsize)
        {
            try
            {
                var machine = s_memoryLookup[ud];
                var memory = machine.m_memoryTracker;
                var forceAlloc = (machine.m_forceAllocations > 0);
                if (nsize == IntPtr.Zero)
                {
                    if (ptr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptr);
                        memory.Free(osize.ToInt64());
                    }
                    return IntPtr.Zero;
                }
                else
                {
                    if (ptr != IntPtr.Zero)
                    {
                        if (nsize.ToInt64() >= osize.ToInt64())
                        {
                            if (forceAlloc)
                            {
                                memory.ForceAlloc(nsize.ToInt64() - osize.ToInt64());
                                return Marshal.ReAllocHGlobal(ptr, nsize);
                            }
                            else if (memory.Alloc(nsize.ToInt64() - osize.ToInt64(), false))
                            {
                                return Marshal.ReAllocHGlobal(ptr, nsize);
                            }
                            else
                            {
                                return IntPtr.Zero;
                            }
                        }
                        else
                        {
                            var result = Marshal.ReAllocHGlobal(ptr, nsize);
                            memory.Free(osize.ToInt64() - nsize.ToInt64());
                            return result;
                        }
                    }
                    else
                    {
                        if (forceAlloc)
                        {
                            memory.ForceAlloc(nsize.ToInt64());
                            return Marshal.AllocHGlobal(nsize);
                        }
                        else if (memory.Alloc(nsize.ToInt64(), false))
                        {
                            return Marshal.AllocHGlobal(nsize);
                        }
                        else
                        {
                            return IntPtr.Zero;
                        }
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                return IntPtr.Zero;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        private void ResetTimeoutTimer()
        {
            m_instructionsExecutedThisTimeout = 0;
            m_firstTimeoutEmitted = false;
        }

        private bool CheckTimeout()
        {
            if (EnforceTimeLimits && m_forceAllocations == 0)
            {
                if (!m_firstTimeoutEmitted)
                {
                    if (m_instructionsExecutedThisTimeout >= s_firstTimeLimit)
                    {
                        m_instructionsExecutedThisTimeout = 0;
                        m_firstTimeoutEmitted = true;
                        return true;
                    }
                    return false;
                }
                else
                {
                    if (m_instructionsExecutedThisTimeout >= s_secondTimeLimit)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        private static void PrintStack(IntPtr state)
        {
            int top = LUA.lua_gettop(state);
            var builder = new System.Text.StringBuilder("Stack (top=" + top + "): ");
            for (int i = 1; i <= top; ++i)
            {
                var str = LUA.luaL_tostring(state, i).Decode(); // top + 1
                LUA.lua_pop(state, 1); // top

                builder.Append(str);
                if (i < top)
                {
                    builder.Append(", ");
                }
                else
                {
                    builder.Append(".");
                }
            }
            System.Diagnostics.Debug.Print(builder.ToString());
        }
    }
}

