//
// CodeDomFileDescriptionTemplate.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;

using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Templates
{
	public class CodeDomFileDescriptionTemplate: SingleFileDescriptionTemplate
	{
		XmlElement domContent;
		
		public override void Load (XmlElement filenode, FilePath baseDirectory)
		{
			base.Load (filenode, baseDirectory);
			domContent = filenode ["CompileUnit"];
			if (domContent == null)
				throw new InvalidOperationException ("Invalid CodeDom template. CompileUnit element not found.");
			
			//this is a code template, so unless told otherwise, default to adding the standard header
			if (string.IsNullOrEmpty (filenode.GetAttribute ("AddStandardHeader")))
				AddStandardHeader = true;
		}
		
		public override string CreateContent (Project project, Dictionary<string, string> tags, string language)
		{
			if (language == null || language == "")
				throw new InvalidOperationException ("Language not defined in CodeDom based template.");
			
			IDotNetLanguageBinding binding = GetLanguageBinding (language) as IDotNetLanguageBinding;
			
			CodeDomProvider provider = null;
			if (binding != null)
				provider = binding.GetCodeDomProvider ();
			
			if (provider == null)
				throw new InvalidOperationException ("The language '" + language + "' does not have support for CodeDom.");

			var xcd = new XmlCodeDomReader ();
			var cu = xcd.ReadCompileUnit (domContent);
			
			foreach (CodeNamespace cns in cu.Namespaces)
				cns.Name = StripImplicitNamespace (project, tags, cns.Name);
			
			CodeGeneratorOptions options = new CodeGeneratorOptions ();
			options.IndentString = TextEditorProperties.IndentString;
			options.BracingStyle = "C";
			
			StringWriter sw = new StringWriter ();
			provider.GenerateCodeFromCompileUnit (cu, sw, options);
			sw.Close ();
			
			return StripHeaderAndBlankLines (sw.ToString (), provider);
		}
		
		static string StripHeaderAndBlankLines (string text, CodeDomProvider provider)
		{
			Mono.TextEditor.TextDocument doc = new Mono.TextEditor.TextDocument ();
			doc.Text = text;
			int realStartLine = 0;
			for (int i = 1; i <= doc.LineCount; i++) {
				string lineText = doc.GetTextAt (doc.GetLine (i));
				// Microsoft.NET generates "auto-generated" tags where Mono generates "autogenerated" tags.
				if (lineText.Contains ("</autogenerated>") || lineText.Contains ("</auto-generated>")) {
					realStartLine = i + 2;
					break;
				}
			}
			
			// The Mono provider inserts additional blank lines, so strip them out
			// But blank lines might actually be significant in other languages.
			// We reformat the C# generated output to the user's coding style anyway, but the reformatter preserves blank lines
			if (provider is Microsoft.CSharp.CSharpCodeProvider) {
				for (int i = 1; i <= doc.LineCount; i++) {
					Mono.TextEditor.LineSegment line = doc.GetLine (i);
					if (IsBlankLine (doc, line) && line.Length > 0) {
						((Mono.TextEditor.IBuffer)doc).Remove (line.Offset, line.Length);
						i--;
						continue;
					}
				}
			}
			
			int offset = doc.GetLine (realStartLine).Offset;
			return doc.GetTextAt (offset, doc.Length - offset);
		}

		static bool IsBlankLine (Mono.TextEditor.TextDocument doc, Mono.TextEditor.LineSegment line)
		{
			for (int i = 0; i < line.EditableLength; i++) {
				if (!Char.IsWhiteSpace (doc.GetCharAt (line.Offset + i)))
					return false;
			}
			return true;
		}
		
		internal static string StripImplicitNamespace (Project project, Dictionary<string,string> tags, string ns)
		{
			// If the project has an implicit namespace, remove it from the namespace for the file
			DotNetProject netProject = project as DotNetProject;
			if (netProject != null) {
				ns = StringParserService.Parse (ns, tags);
				return netProject.StripImplicitNamespace (ns);
			} else
				return ns;
		}
	}
}
