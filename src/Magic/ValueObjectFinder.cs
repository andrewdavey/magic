using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Magic
{
    class ValueObjectFinder : AbstractAstVisitor
    {
        ValueObjectDescriptor currentInfo;
        public readonly List<ValueObjectDescriptor> ValueObjectInfos = new List<ValueObjectDescriptor>();

        public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
        {
            var previous = currentInfo;

            if (typeDeclaration.Type == ClassType.Class)
            {
                var isValueObject = typeDeclaration.BaseTypes.Any(t => t.Type == "IValueObject");
                if (isValueObject)
                {
                    currentInfo = new ValueObjectDescriptor(typeDeclaration);
                    ValueObjectInfos.Add(currentInfo);
                }
            }
            
            var result = base.VisitTypeDeclaration(typeDeclaration, data);
            currentInfo = previous;
            return result;
        }

        public override object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
        {
            if (currentInfo != null)
            {
                currentInfo.AddField(fieldDeclaration);
            }
            return base.VisitFieldDeclaration(fieldDeclaration, data);
        }
    }
}
