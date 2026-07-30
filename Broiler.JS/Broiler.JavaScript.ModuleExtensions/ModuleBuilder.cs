using System;
using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Clr;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.ModuleExtensions
{
    public class ModuleBuilder
    {
        private readonly List<(string name, object value)> exportedObjects = [];
        private readonly string _moduleName;

        public ModuleBuilder(string moduleName)
        {
            _moduleName = moduleName;
        }

        public ModuleBuilder ExportType<T>(string? name = null)
        {
            exportedObjects.Add((name ?? typeof(T).Name, typeof(T)));
            return this;
        }

        public ModuleBuilder ExportType(Type type, string? name = null)
        {
            exportedObjects.Add((name ?? type.Name, type));
            return this;
        }

        /// <summary>
        /// Records a value for export.  The value is kept in its .NET form and
        /// marshalled in <see cref="AddModuleToContext"/>, so that the conversion
        /// happens against the context the module is registered with rather than
        /// against whatever engine state happened to exist when the builder ran.
        /// </summary>
        public ModuleBuilder ExportValue(string name, object value)
        {
            exportedObjects.Add((name, value));
            return this;
        }

        public ModuleBuilder ExportFunction(string name, JSFunctionDelegate func)
        {
            exportedObjects.Add((name, func));
            return this;
        }

        public void AddModuleToContext(JSModuleContext context)
        {
            JSObject globalExport = new JSObject();
            foreach ((string name, object value) in exportedObjects)
            {
                switch (value)
                {
                    case Type type:
                        globalExport[name] = ClrType.From(type);
                        break;
                    case JSFunctionDelegate @delegate:
                        globalExport[name] = new JSFunction(@delegate);
                        break;
                    case JSValue jsValue:
                        globalExport[name] = jsValue;
                        break;
                    default:
                        globalExport[name] = ClrProxy.Marshal(value);
                        break;
                }
            }

            globalExport[KeyStrings.@default] = globalExport;
            context.RegisterModule(_moduleName, globalExport);
        }
    }
}

