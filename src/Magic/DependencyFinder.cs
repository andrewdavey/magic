using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Magic
{
    class DependencyFinder : AbstractAstVisitor
    {
        public DependencyFinder()
        {
            DependencyRelations = new List<DependencyRelation>();
        }

        public List<DependencyRelation> DependencyRelations { get; private set; }

        public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
        {
            if (typeDeclaration.Type == ClassType.Class)
            {
                var dependsOn = FindDependencyDeclaration(typeDeclaration);
                if (dependsOn != null)
                {
                    DependencyRelations.Add(new DependencyRelation(typeDeclaration, dependsOn.GenericTypes));
                }
            }

            return base.VisitTypeDeclaration(typeDeclaration, data);
        }

        TypeReference FindDependencyDeclaration(TypeDeclaration typeDeclaration)
        {
            return typeDeclaration.BaseTypes.FirstOrDefault(t => t.Type.StartsWith("IDependOn"));
        }
    }
}
