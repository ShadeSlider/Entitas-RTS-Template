//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.CodeGeneratorMonoComponent.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using UnityEngine;
using Entitas;

namespace Entitas.Generated.Meta.ComponentsMonoBehaviours {

    public class MetaGameConfigComponentMonoBehaviour : BaseComponentMonoBehaviour {
    
        public Configs.SO.GameConfig value;
    
        public override IComponent Component
        {
            get 
            { 
                return new GameConfigComponent
                {
                    value = value
                }; 
            }
        }
    }
}
