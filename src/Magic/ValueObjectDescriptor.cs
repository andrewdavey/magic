using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.Ast;

namespace Magic
{
    class ValueObjectDescriptor
    {
        public readonly TypeDeclaration TypeDeclaration;
        public readonly List<FieldDeclaration> FieldDeclarations;

        public ValueObjectDescriptor(TypeDeclaration typeDeclaration)
        {
            TypeDeclaration = typeDeclaration;
            FieldDeclarations = new List<FieldDeclaration>();
        }

        public void AddField(FieldDeclaration fieldDeclaration)
        {
            FieldDeclarations.Add(fieldDeclaration);
        }
    }
}
