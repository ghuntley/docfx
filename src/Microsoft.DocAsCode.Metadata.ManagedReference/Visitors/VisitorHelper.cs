// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using Microsoft.CodeAnalysis;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Utility;

    public static class VisitorHelper
    {
        public static void FeedComments(MetadataItem item, ITripleSlashCommentParserContext context)
        {
            if (!string.IsNullOrEmpty(item.RawComment))
            {
                var commentModel = TripleSlashCommentModel.CreateModel(item.RawComment, item.Language, context);
                if (commentModel == null) return;
                item.Summary = commentModel.Summary;
                item.Remarks = commentModel.Remarks;
                item.Exceptions = commentModel.Exceptions;
                item.Sees = commentModel.Sees;
                item.SeeAlsos = commentModel.SeeAlsos;
                item.Examples = commentModel.Examples;
                item.CommentModel = commentModel;
            }
        }

        public static string GetId(ISymbol symbol)
        {
            if (symbol == null)
            {
                return null;
            }

            var assemblySymbol = symbol as IAssemblySymbol;
            if (assemblySymbol != null)
            {
                return assemblySymbol.MetadataName;
            }
            var dynamicSymbol = symbol as IDynamicTypeSymbol;
            if (dynamicSymbol != null)
            {
                return typeof(object).FullName;
            }
            string str = symbol.GetDocumentationCommentId();
            if (string.IsNullOrEmpty(str))
            {
                Debug.Fail("Cannot get documentation comment id");
                return symbol.MetadataName;
            }

            return str.ToString().Substring(2);
        }

        public static ApiParameter GetParameterDescription(ISymbol symbol, MetadataItem item, string id, bool isReturn, ITripleSlashCommentParserContext context)
        {
            string comment = isReturn ? item.CommentModel?.Returns : item.CommentModel?.GetParameter(symbol.Name);
            return new ApiParameter
            {
                Name = isReturn ? null : symbol.Name,
                Type = id,
                Description = comment,
            };
        }

        public static ApiParameter GetTypeParameterDescription(ITypeParameterSymbol symbol, MetadataItem item, ITripleSlashCommentParserContext context)
        {
            string comment = item.CommentModel?.GetTypeParameter(symbol.Name);
            return new ApiParameter
            {
                Name = symbol.Name,
                Description = comment,
            };
        }

        public static SourceDetail GetSourceDetail(ISymbol symbol)
        {
            // For namespace, definition is meaningless
            if (symbol == null || symbol.Kind == SymbolKind.Namespace)
            {
                return null;
            }

            var syntaxRef = symbol.DeclaringSyntaxReferences.LastOrDefault();
            if (symbol.IsExtern || syntaxRef == null)
            {
                return new SourceDetail
                {
                    IsExternalPath = true,
                    Path = symbol.ContainingAssembly?.Name,
                };
            }

            var syntaxNode = syntaxRef.GetSyntax();
            Debug.Assert(syntaxNode != null);
            if (syntaxNode != null)
            {
                var source = new SourceDetail
                {
                    StartLine = syntaxNode.SyntaxTree.GetLineSpan(syntaxNode.Span).StartLinePosition.Line,
                    Path = syntaxNode.SyntaxTree.FilePath,
                    Name = symbol.Name
                };

                source.Remote = GitUtility.GetGitDetail(source.Path);
                if (source.Remote != null)
                {
                    source.Path = source.Path.FormatPath(UriKind.Relative, source.Remote.LocalWorkingDirectory);
                }
                return source;
            }

            return null;
        }

        public static MemberType GetMemberTypeFromTypeKind(TypeKind typeKind)
        {
            switch (typeKind)
            {
                case TypeKind.Module:
                case TypeKind.Class:
                    return MemberType.Class;
                case TypeKind.Enum:
                    return MemberType.Enum;
                case TypeKind.Interface:
                    return MemberType.Interface;
                case TypeKind.Struct:
                    return MemberType.Struct;
                case TypeKind.Delegate:
                    return MemberType.Delegate;
                default:
                    return MemberType.Default;
            }
        }
    }
}
