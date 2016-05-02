using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

using PCLExt.FileStorage;


namespace PCLExt.Lua
{
    internal class FileSystemScriptLoader : IScriptLoader
    {
        private static IFolder Modules => FileSystem.Current.BaseStorage.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result.CreateFolderAsync("modules", CreationCollisionOption.OpenIfExists).Result;

        public object LoadFile(string file, Table globalContext)
        {
            if (file.StartsWith("m_"))
            {
                if (FileSystem.Current.BaseStorage.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result.CheckExistsAsync(file).Result == ExistenceCheckResult.FileExists)
                    using (var stream = FileSystem.Current.BaseStorage.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result.GetFileAsync(file).Result.OpenAsync(PCLExt.FileStorage.FileAccess.Read).Result)
                    using (var reader = new StreamReader(stream))
                        return reader.ReadToEnd();
            }
            else
            {
                if (Modules.CheckExistsAsync(file).Result == ExistenceCheckResult.FileExists)
                    using (var stream = Modules.GetFileAsync(file).Result.OpenAsync(PCLExt.FileStorage.FileAccess.Read).Result)
                    using (var reader = new StreamReader(stream))
                        return reader.ReadToEnd();
            }

            return null;
        }

        public string ResolveFileName(string filename, Table globalContext) => $"{filename}.lua";

        public string ResolveModuleName(string modname, Table globalContext) => $"m_{modname}";
    }


    public class MoonLua : LuaScript
    {
        static MoonLua()
        {
            UserData.RegisterType<CultureInfo>();
        }

        private string LuaName { get; }

        private Script LuaScript { get; }

        
        public override object this[string fullPath]
        {
            get { return LuaScript.Globals[fullPath]; }
            set
            {
                var type = value.GetType();
                if (!UserData.IsTypeRegistered(type))
                    UserData.RegisterType(type);

                LuaScript.Globals[fullPath] = value;
            }
        }

        public MoonLua(string luaName = "", bool instantInit = false)
        {
            LuaName = luaName;

            LuaScript = new Script();
            LuaScript.Options.ScriptLoader = new FileSystemScriptLoader();


            // Register custom modules that we allow to use.
            foreach (var module in CustomModules)
                RegisterModule(module);
            
            RegisterCustom(LuaScript.Globals);


            if (instantInit)
                ReloadFile();
        }
        private Table CompileFile(string path)
        {
            var modules = CoreModules.Preset_SoftSandbox;
            var folder = FileSystem.Current.BaseStorage.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result;

            var dirs = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Reverse().Skip(1).Reverse();
            foreach (var dir in dirs)
                folder = folder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists).Result;

            var file = Path.GetFileName(path);
            var text = folder.GetFileAsync(file).Result.ReadAllTextAsync().Result;

            var table = new Table(LuaScript);
            table.RegisterCoreModules(modules);
            RegisterCustom(table);
            LuaScript.DoString(text, table);

            return table;
        }
        private static string[] GetFiles(string path)
        {
            var folder = FileSystem.Current.BaseStorage.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result;

            var dirs = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Reverse().Skip(1).Reverse();
            foreach (var dir in dirs)
                folder = folder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists).Result;

            var files = folder.GetFilesAsync().Result;

            return files.Select(file => file.Name).ToArray();
        }
        private void RegisterModule(string moduleName, string tableName = "", CoreModules modules = CoreModules.Preset_SoftSandbox)
        {
            if (string.IsNullOrEmpty(tableName))
                tableName = moduleName;

            var table = new Table(LuaScript);
            table.RegisterCoreModules(modules);
            RegisterCustom(table);
            LuaScript.DoFile(moduleName, table);

            LuaScript.Globals[tableName] = table;
        }
        private void RegisterCustom(Table table)
        {
            foreach (var context in CustomContext)
                table[context.Key] = context.Value;

            table["CompileFile"] = (Func<string, Table>) CompileFile;
            table["GetFiles"] = (Func<string, string[]>) GetFiles;
        }


        public override bool ReloadFile()
        {
            if (FileSystem.Current.BaseStorage.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result.CheckExistsAsync(LuaName).Result == ExistenceCheckResult.FileExists)
                using (var stream = FileSystem.Current.BaseStorage.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result.GetFileAsync(LuaName).Result.OpenAsync(PCLExt.FileStorage.FileAccess.Read).Result)
                using (var reader = new StreamReader(stream))
                {
                    var code = reader.ReadToEnd();
                    LuaScript.DoString(code);
                    return true;
                }

            return false;
        }

        public override object[] CallFunction(string functionName, params object[] args) => LuaScript.Call(LuaScript.Globals[functionName], args).Tuple;
    }

    public class MoonLuaTable : LuaTable
    {
        private Table TableScript { get; }

        public MoonLuaTable(Table tableScript) { TableScript = tableScript; }
        public MoonLuaTable(LuaScript luaScript, string tableName) { TableScript = luaScript[tableName] as Table; }

        public override object this[object field]
        {
            get { return TableScript[field] is Table ? new MoonLuaTable((Table) TableScript[field]) : TableScript[field]; }
            set { TableScript[field] = value; }
        }
        public override object this[string field]
        {
            get { return TableScript[field] is Table ? new MoonLuaTable((Table) TableScript[field]) : TableScript[field]; }
            set { TableScript[field] = value; }
        }

        public override object[] CallFunction(string functionName, params object[] args) => TableScript.OwnerScript.Call(TableScript[functionName], args).Tuple;

        public override Dictionary<object, object> ToDictionary() => TableScript.Pairs.ToDictionary<TablePair, object, object>(pair => pair.Key, pair => RecursiveParse(pair.Value));
        private static object RecursiveParse(object value)
        {
            if (value is Table)
                return RecursiveParse(new MoonLuaTable((Table) value).ToDictionary());

            return value;
        }

        public override List<object> ToList() => TableScript.Values.Cast<object>().ToList();
        public override object[] ToArray() => TableScript.Values.Cast<object>().ToArray();
    }
}
