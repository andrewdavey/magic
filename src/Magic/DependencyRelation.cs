using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;

namespace Magic
{
    class DependencyRelation
    {
        public readonly TypeDeclaration DependentType;
        public readonly IEnumerable<TypeReference> Dependencies;
        public readonly List<ConstructorDeclaration> ExistingConstructors;

        public DependencyRelation(TypeDeclaration dependentType, IEnumerable<TypeReference> dependencies)
        {
            this.DependentType = dependentType;
            this.Dependencies = dependencies;
            ExistingConstructors = new List<ConstructorDeclaration>();
        }

        public void RecordConstructor(ConstructorDeclaration constructorDeclaration)
        {
            ExistingConstructors.Add(constructorDeclaration);
        }
    }
}
