using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;

namespace Magic
{
    class DependencyRelation
    {
        public readonly TypeDeclaration DependentType;
        public readonly IEnumerable<TypeReference> Dependencies;

        public DependencyRelation(TypeDeclaration dependentType, IEnumerable<TypeReference> dependencies)
        {
            this.DependentType = dependentType;
            this.Dependencies = dependencies;
        }

    }
}
