using System;

namespace PCLExt.Lua
{
    /// <summary>
    /// 
    /// </summary>
    public static class Lua
    {
        private static Exception NotImplementedInReferenceAssembly() =>
            new NotImplementedException(@"This functionality is not implemented in the portable version of this assembly.
You should reference the PCLExt.Lua NuGet package from your main application project in order to reference the platform-specific implementation.");


        public static LuaScript CreateLuaScript(string luaScriptName = "")
        {
#if DESKTOP || ANDROID || __IOS__ || MAC
            return new MoonLua(luaScriptName);
#endif

            throw NotImplementedInReferenceAssembly();
        }

        public static LuaTable CreateTable(LuaScript luaScript, string tableName)
        {
#if DESKTOP || ANDROID || __IOS__ || MAC
            return new MoonLuaTable(luaScript, tableName);
#endif

            throw NotImplementedInReferenceAssembly();
        }

        public static LuaTable ToLuaTable(object obj)
        {
#if DESKTOP || ANDROID || __IOS__ || MAC
            var table = obj as MoonSharp.Interpreter.Table;
            return table != null ? new MoonLuaTable(table) : null;
#endif

            throw NotImplementedInReferenceAssembly();
        }

        public static void RegisterCustomFunc(string name, object function)
        {
#if DESKTOP || ANDROID || __IOS__ || MAC
            LuaScript.RegisterCustomFunc(name, function);
#endif

            throw NotImplementedInReferenceAssembly();
        }

        public static void RegisterModule(string name)
        {
#if DESKTOP || ANDROID || __IOS__ || MAC
            LuaScript.RegisterModule(name);
#endif

            throw NotImplementedInReferenceAssembly();
        }
    }
}
