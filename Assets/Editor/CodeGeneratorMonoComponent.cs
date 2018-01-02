using System.Collections.Generic;
using System.IO;
using System.Linq;
using Entitas.Utils;

namespace Entitas.CodeGeneration.Plugins {

    public class CodeGeneratorMonoComponent : ICodeGenerator, IConfigurable {

        public string name { get { return "ComponentMonoBehaviour"; } }
        public int priority { get { return 0; } }
        public bool isEnabledByDefault { get { return true; } }
        public bool runInDryMode { get { return true; } }
        
        public Dictionary<string, string> defaultProperties { get { return _ignoreNamespacesConfig.defaultProperties; } }

        readonly IgnoreNamespacesConfig _ignoreNamespacesConfig = new IgnoreNamespacesConfig();

        const string COMPONENT_TEMPLATE =
            @"
using UnityEngine;
using Entitas;

namespace Entitas.Generated.${Context}.ComponentsMonoBehaviours {

    public class ${Context}${ComponentType}MonoBehaviour : BaseComponentMonoBehaviour {
    
        ${memberArgs}
    
        public override IComponent Component
        {
            get 
            { 
                return new ${ComponentType}
                {
                    ${memberAssignment}
                }; 
            }
        }
    }
}
";
        
        const string MEMBER_ARGS_TEMPLATE =
            @"public ${MemberType} ${MemberName};";

        const string MEMBER_ASSIGNMENT_TEMPLATE =
            @"${memberName} = ${memberName},";

        public void Configure(Preferences preferences) {
            _ignoreNamespacesConfig.Configure(preferences);
        }
        
        public CodeGenFile[] Generate(CodeGeneratorData[] data) {
            return data
                .OfType<ComponentData>()
                .Where(d => d.ShouldGenerateMethods())
                .SelectMany(d => generateComponentMonoBehaviours(d))
                .ToArray();
        }

        CodeGenFile[] generateComponentMonoBehaviours(ComponentData data) {
            return data.GetContextNames()
                .Select(contextName => generateComponentMonoBehaviousClass(contextName, data))
                .ToArray();
        }

        CodeGenFile generateComponentMonoBehaviousClass(string contextName, ComponentData data) {

            MemberData[] memberData = (MemberData[]) data["component_memberInfos"];
            var componentType = data.GetFullTypeName().ShortTypeName();
            
            return new CodeGenFile(
                contextName + Path.DirectorySeparatorChar + "ComponentsMonoBehaviours" + Path.DirectorySeparatorChar + contextName + componentType + "MonoBehaviour.cs",
                    COMPONENT_TEMPLATE
                    .Replace("${Context}", contextName)
                    .Replace("${ComponentType}", componentType)
                    .Replace("${memberArgs}", getMemberArgs(memberData))
                    .Replace("${memberAssignment}", getMemberAssignment(memberData)),
                GetType().FullName
            );
        }
        
        string getMemberArgs(MemberData[] memberData) {
            var args = memberData
                .Select(info => MEMBER_ARGS_TEMPLATE
                    .Replace("${MemberType}", info.type)
                    .Replace("${MemberName}", info.name)
                )
                .ToArray();

            return string.Join("\n\t", args);
        }

        string getMemberAssignment(MemberData[] memberData) {
            var assignments = memberData
                .Select(info => MEMBER_ASSIGNMENT_TEMPLATE
                    .Replace("${MemberType}", info.type)
                    .Replace("${memberName}", info.name)
                    .Replace("${MemberName}", info.name)
                )
                .ToArray();

            return string.Join("\n\t\t\t\t", assignments).TrimEnd(',');
        }        
    }
}
