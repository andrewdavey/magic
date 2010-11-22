using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Genuilder.Extensibility;
using Genuilder.Extensibility.NRefactory;
using ICSharpCode.NRefactory.Ast;

namespace Magic
{
    public class DependencyConstructorGenerator : IPlugin
    {
        public void Initialize(ICodeRepository repository)
        {
            repository.CodeItemCreated += repository_CodeItemCreated;
        }

        void repository_CodeItemCreated(ICodeRepository sender, CodeItem item)
        {
            if (!item.Name.EndsWith(".cs")) return;
            if (item.Name.EndsWith(".ctorgen.cs")) return;

            var relations = FindDependencyRelations(item);
            if (relations.Count == 0) return;

            var generatedItems = CreateTopLevelCode(relations);
            var dependency = item.SourceOf(Path.GetFileNameWithoutExtension(item.Name) + ".ctorgen.cs");
            SaveGeneratedCodeToDependency(generatedItems, dependency);
        }

        IEnumerable<INode> CreateTopLevelCode(IEnumerable<DependencyRelation> relations)
        {
            return
                from relation in relations
                let originalNamespace = relation.DependentType.Parent as NamespaceDeclaration
                where originalNamespace != null
                // TODO: Add support for classes not in a namespace
                select new NamespaceDeclaration(originalNamespace.Name)
                {
                    Children = new List<INode>
                    {
                        CreatePartialClass(relation)
                    }
                };
        }

        INode CreatePartialClass(DependencyRelation relation)
        {
            var partialClass = new TypeDeclaration(
                relation.DependentType.Modifier, 
                new List<AttributeSection>()
            );
            partialClass.Name = relation.DependentType.Name;
            var fields = CreateFields(relation);
            var constructors = CreateConstructors(relation);

            partialClass.Children.AddRange(fields);
            partialClass.Children.AddRange(constructors);

            return partialClass;
        }

        IEnumerable<FieldDeclaration> CreateFields(DependencyRelation relation)
        {
            return from d in relation.Dependencies
                   select new FieldDeclaration(
                       new List<AttributeSection>(),
                       d,
                       Modifiers.Private
                   )
                   {
                       Fields = new List<VariableDeclaration>
                       {
                           new VariableDeclaration(CamelCaseName(d))
                       }
                   };
        }

        IEnumerable<ConstructorDeclaration> CreateConstructors(DependencyRelation relation)
        {
            if (relation.ExistingConstructors.Count > 0)
            {
                foreach (var c in relation.ExistingConstructors)
                {
                    yield return CreateConstructorFromExistingConstructor(relation, c);
                }
            }
            else
            {
                yield return CreateSimpleConstructor(relation);
            }
        }

        ConstructorDeclaration CreateConstructorFromExistingConstructor(DependencyRelation relation, ConstructorDeclaration c)
        {
            return new ConstructorDeclaration(
                relation.DependentType.Name,
                Modifiers.Public,
                c.Parameters.Concat(CreateParameters(relation)).ToList(),
                new List<AttributeSection>()
            )
            {
                Body = new BlockStatement
                {
                    Children = CreateAssignments(relation).Concat(c.Body.Children).ToList()
                }
            };
        }

        ConstructorDeclaration CreateSimpleConstructor(DependencyRelation relation)
        {
            return new ConstructorDeclaration(
                relation.DependentType.Name,
                Modifiers.Public,
                CreateParameters(relation).ToList(),
                new List<AttributeSection>()
            )
            {
                Body = new BlockStatement
                {
                    Children = CreateAssignments(relation).ToList()
                }
            };
        }

        IEnumerable<INode> CreateAssignments(DependencyRelation relation)
        {
            return from d in relation.Dependencies
                   let name = CamelCaseName(d)
                   select new ExpressionStatement(new AssignmentExpression(
                       new MemberReferenceExpression(
                           new ThisReferenceExpression(),
                           name
                       ),
                       AssignmentOperatorType.Assign,
                       new IdentifierExpression(name)
                   ));
        }

        IEnumerable<ParameterDeclarationExpression>CreateParameters(DependencyRelation relation)
        {
            return from d in relation.Dependencies
                   select new ParameterDeclarationExpression(d, CamelCaseName(d));
        }

        List<DependencyRelation> FindDependencyRelations(CodeItem item)
        {
            var extension = item.GetExtension<CompilationUnitExtension>();
            var dependencyFinder = new DependencyFinder();
            extension.CompilationUnit.AcceptVisitor(dependencyFinder, null);
            return dependencyFinder.DependencyRelations;
        }

        string CamelCaseName(TypeReference d)
        {
            var name = d.Type.Split('.').Last();
            if (Regex.IsMatch(name, "^I[A-Z]"))
            {
                name = name.Substring(1);
            }
            return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }

        void SaveGeneratedCodeToDependency(IEnumerable<INode> generatedItems, CodeDependency dependency)
        {
            var e = dependency.Target.GetExtension<CompilationUnitExtension>();
            e.CompilationUnit.Children.Clear();
            e.CompilationUnit.Children.AddRange(generatedItems);
            e.Save();
        }

    }
}
