using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Genuilder.Extensibility;
using Genuilder.Extensibility.NRefactory;
using System.IO;
using ICSharpCode.NRefactory.Ast;
using Genuilder.Extensibility.Utilities;

namespace Magic
{
    public class ValueObjectGenerator : IPlugin
    {
        public void Initialize(ICodeRepository repository)
        {
            repository.CodeItemCreated += repository_CodeItemCreated;
        }

        void repository_CodeItemCreated(ICodeRepository sender, CodeItem item)
        {
            if (!item.Name.EndsWith(".cs")) return;
            if (item.Name.EndsWith(".vogen.cs")) return;

            var valueObjects = FindValueObjects(item);
            if (valueObjects.Count == 0) return;

            valueObjects = SkipAndWarnValueObjectsWithNoFields(item, valueObjects);

            var generatedItems = CreateTopLevelCode(valueObjects);
            var dependency = item.SourceOf(Path.GetFileNameWithoutExtension(item.Name) + ".vogen.cs");
            SaveGeneratedCodeToDependency(generatedItems, dependency);
        }

        List<ValueObjectDescriptor> SkipAndWarnValueObjectsWithNoFields(CodeItem item, List<ValueObjectDescriptor> valueObjects)
        {
            var skipped = valueObjects.Where(v => v.FieldDeclarations.Count == 0);
            foreach (var valueObject in skipped)
            {
                item.Logger.Send(
                    "Cannot generate value type because no fields are declared.",
                    new Location(valueObject.TypeDeclaration.StartLocation.Line, valueObject.TypeDeclaration.StartLocation.Column),
                    new Location(valueObject.TypeDeclaration.EndLocation.Line, valueObject.TypeDeclaration.EndLocation.Column),
                    Genuilder.Extensibility.Utilities.LogType.Error
                );
            }
            return valueObjects.Except(skipped).ToList();
        }

        List<ValueObjectDescriptor> FindValueObjects(CodeItem item)
        {
            var e = item.GetExtension<CompilationUnitExtension>();
            var visitor = new ValueObjectFinder();
            e.CompilationUnit.AcceptVisitor(visitor, null);
            return visitor.ValueObjectInfos;
        }

        IEnumerable<INode> CreateTopLevelCode(IEnumerable<ValueObjectDescriptor> valueObjects)
        {
            return
                from valueObject in valueObjects
                let originalNamespace = valueObject.TypeDeclaration.Parent as NamespaceDeclaration
                where originalNamespace != null
                // TODO: Add support for classes not in a namespace
                select CreateNamespace(valueObject, originalNamespace);
        }

        NamespaceDeclaration CreateNamespace(ValueObjectDescriptor valueObject, NamespaceDeclaration originalNamespace)
        {
            var ns = new NamespaceDeclaration(originalNamespace.Name);
            ns.Children.AddRange(originalNamespace.Children.OfType<UsingDeclaration>());
            ns.Children.Add(CreatePartialClass(valueObject));
            return ns;
        }

        INode CreatePartialClass(ValueObjectDescriptor valueObject)
        {
            var partialClass = new TypeDeclaration(
                valueObject.TypeDeclaration.Modifier,
                new List<AttributeSection>()
            );
            partialClass.Name = valueObject.TypeDeclaration.Name;
            partialClass.Children.Add(CreateConstructor(valueObject));
            partialClass.Children.AddRange(CreateProperties(valueObject));
            partialClass.Children.Add(CreateEqualsMethod(valueObject));
            partialClass.Children.Add(CreateGetHashCodeMethod(valueObject));
            partialClass.Children.Add(CreateEqualityOperator(valueObject));
            partialClass.Children.Add(CreateInequalityOperator(valueObject));
            return partialClass;
        }

        ConstructorDeclaration CreateConstructor(ValueObjectDescriptor valueObject)
        {
            return new ConstructorDeclaration(
                valueObject.TypeDeclaration.Name,
                Modifiers.Public,
                CreateConstructorParameters(valueObject).ToList(),
                new List<AttributeSection>()
            )
            {
                Body = new BlockStatement
                {
                    Children = (from fd in valueObject.FieldDeclarations
                                from f in fd.Fields
                                // this.[field] = [parameter];
                                select (INode)new ExpressionStatement(new AssignmentExpression(
                                    new MemberReferenceExpression(new ThisReferenceExpression(), f.Name),
                                    AssignmentOperatorType.Assign,
                                    new IdentifierExpression(f.Name)
                                ))).ToList()
                }
            };
        }

        IEnumerable<ParameterDeclarationExpression> CreateConstructorParameters(ValueObjectDescriptor valueObject)
        {
            return
                from fd in valueObject.FieldDeclarations
                from f in fd.Fields
                select new ParameterDeclarationExpression(fd.TypeReference, f.Name);
        }

        IEnumerable<PropertyDeclaration> CreateProperties(ValueObjectDescriptor valueObject)
        {
            return from fd in valueObject.FieldDeclarations
                   from f in fd.Fields
                   select new PropertyDeclaration(Modifiers.Public, new List<AttributeSection>(), PropertyNameFromField(f), new List<ParameterDeclarationExpression>())
                   {
                       TypeReference = fd.TypeReference,
                       GetRegion = new PropertyGetRegion(new BlockStatement {
                           Children = new List<INode> 
                           {
                               new ReturnStatement(new MemberReferenceExpression(
                                   new ThisReferenceExpression(),
                                   f.Name
                               ))
                           }
                       }, new List<AttributeSection>())
                   };
        }

        string PropertyNameFromField(VariableDeclaration f)
        {
            return f.Name.Substring(0, 1).ToUpperInvariant() + f.Name.Substring(1);
        }

        MethodDeclaration CreateEqualsMethod(ValueObjectDescriptor valueObject)
        {
            var tests = from fd in valueObject.FieldDeclarations
                        from f in fd.Fields
                        select new BinaryOperatorExpression(
                            new MemberReferenceExpression(new ThisReferenceExpression(), f.Name),
                            BinaryOperatorType.Equality,
                            new MemberReferenceExpression(new IdentifierExpression("typed"), f.Name)
                        );
            var test = tests.Aggregate((t1, t2) => new BinaryOperatorExpression(t1, BinaryOperatorType.LogicalAnd, t2));

            return new MethodDeclaration
            {
                Modifier = Modifiers.Public | Modifiers.Override,
                TypeReference = new TypeReference("global::System.Boolean"),
                Name = "Equals",
                Parameters = new List<ParameterDeclarationExpression> { new ParameterDeclarationExpression(new TypeReference("global::System.Object"), "obj") },
                Body = new BlockStatement
                {
                    Children = new List<INode>
                    {
                        // var typed = obj as <ValueObjectType>;
                        new LocalVariableDeclaration(new VariableDeclaration(
                            "typed", 
                            new CastExpression(
                                new TypeReference(valueObject.TypeDeclaration.Name), 
                                new IdentifierExpression("obj"), 
                                CastType.TryCast
                            ),
                            new TypeReference(valueObject.TypeDeclaration.Name)
                        )),
                        
                        // if (Object.ReferenceEquals(typed, null)) return false;
                        new IfElseStatement(new InvocationExpression(
                            new MemberReferenceExpression(new TypeReferenceExpression(new TypeReference("global::System.Object")), "ReferenceEquals"),
                            new List<Expression>
                            {
                                new IdentifierExpression("typed"),
                                new PrimitiveExpression(null) 
                            }
                        ), new ReturnStatement(new PrimitiveExpression(false))),

                        new ReturnStatement(test)
                    }
                    
                }
            };
        }

        MethodDeclaration CreateGetHashCodeMethod(ValueObjectDescriptor valueObject)
        {
            var hashCodes = from fd in valueObject.FieldDeclarations
                            from f in fd.Fields
                            select (Expression)new InvocationExpression(
                                new MemberReferenceExpression(
                                    new MemberReferenceExpression(new ThisReferenceExpression(), f.Name),
                                    "GetHashCode"
                                )
                            );

            return new MethodDeclaration
            {
                TypeReference = new TypeReference("global::System.Int32"),
                Modifier = Modifiers.Public | Modifiers.Override,
                Name = "GetHashCode",
                Body = new BlockStatement
                {
                    Children = new List<INode>
                    {
                        new ReturnStatement(hashCodes.Aggregate((c1,c2) => new BinaryOperatorExpression(c1, BinaryOperatorType.ExclusiveOr, c2)))
                    }
                }
            };
        }

        OperatorDeclaration CreateEqualityOperator(ValueObjectDescriptor valueObject)
        {
            return new OperatorDeclaration
            {
                OverloadableOperator = OverloadableOperatorType.Equality,
                TypeReference = new TypeReference("global::System.Boolean"),
                Modifier = Modifiers.Public | Modifiers.Static,
                Parameters = new List<ParameterDeclarationExpression>
                {
                    new ParameterDeclarationExpression(new TypeReference(valueObject.TypeDeclaration.Name), "x"),
                    new ParameterDeclarationExpression(new TypeReference(valueObject.TypeDeclaration.Name), "y"),
                },
                Body = new BlockStatement
                {
                    Children = new List<INode>
                    {
                        new ReturnStatement(
                            new InvocationExpression(
                                new MemberReferenceExpression(
                                    new IdentifierExpression("x"), "Equals"
                                ),
                                new List<Expression> {new IdentifierExpression("y")}
                            )
                        )
                    }
                }
            };
        }

        OperatorDeclaration CreateInequalityOperator(ValueObjectDescriptor valueObject)
        {
            return new OperatorDeclaration
            {
                OverloadableOperator = OverloadableOperatorType.InEquality,
                TypeReference = new TypeReference("global::System.Boolean"),
                Modifier = Modifiers.Public | Modifiers.Static,
                Parameters = new List<ParameterDeclarationExpression>
                {
                    new ParameterDeclarationExpression(new TypeReference(valueObject.TypeDeclaration.Name), "x"),
                    new ParameterDeclarationExpression(new TypeReference(valueObject.TypeDeclaration.Name), "y"),
                },
                Body = new BlockStatement
                {
                    Children = new List<INode>
                    {
                        new ReturnStatement(
                            new UnaryOperatorExpression(
                                new ParenthesizedExpression(
                                    new BinaryOperatorExpression(
                                        new IdentifierExpression("x"), 
                                        BinaryOperatorType.Equality, 
                                        new IdentifierExpression("y")
                                    )
                                ),
                                UnaryOperatorType.Not
                            )
                        )
                    }
                }
            };
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
