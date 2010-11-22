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

        DependencyRelation currentDependencyRelation;

        public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
        {
            var previous = currentDependencyRelation;

            if (typeDeclaration.Type == ClassType.Class)
            {
                var dependsOn = FindDependencyDeclaration(typeDeclaration);
                if (dependsOn != null)
                {
                    currentDependencyRelation = new DependencyRelation(typeDeclaration, dependsOn.GenericTypes);
                    DependencyRelations.Add(currentDependencyRelation);
                }
            }

            var result = base.VisitTypeDeclaration(typeDeclaration, data);
            currentDependencyRelation = previous;
            return result;
        }

        public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
        {
            currentDependencyRelation.RecordConstructor(constructorDeclaration);
            return base.VisitConstructorDeclaration(constructorDeclaration, data);
        }

        TypeReference FindDependencyDeclaration(TypeDeclaration typeDeclaration)
        {
            return typeDeclaration.BaseTypes.FirstOrDefault(t => t.Type.StartsWith("IDependOn"));
        }
    }
}
