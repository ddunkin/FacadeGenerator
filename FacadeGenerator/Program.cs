using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace FacadeGenerator
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			string inputFile = args[0];
			string outputFile = args[1];

			ModuleDefinition module = ModuleDefinition.ReadModule(inputFile);

			module.Types.RemoveAll(x => !x.IsPublic);

			foreach (var type in module.Types)
				ProcessType(type);

			module.Write(outputFile);
		}

		static void ProcessType(TypeDefinition type)
		{
			type.Interfaces.RemoveAll(x => !x.Resolve().IsPublic);
			type.Fields.RemoveAll(x => !x.IsPublic && !x.IsFamilyOrAssembly && !x.IsFamily);
			type.Methods.RemoveAll(x => !x.IsPublic && !x.IsFamilyOrAssembly && !x.IsFamily);

			foreach (var method in type.Methods)
				ProcessMethod(method);

			type.NestedTypes.RemoveAll(x => !x.IsNestedPublic && !x.IsNestedFamilyOrAssembly && !x.IsNestedFamily);
			foreach (var nestedType in type.NestedTypes)
				ProcessType(nestedType);
		}

		static void ProcessMethod(MethodDefinition method)
		{
			if (!method.HasBody)
				return;

			method.Body = new MethodBody(method);

			var exceptionCtor = typeof(InvalidOperationException).GetConstructor(new Type[]{});
			var constructorReference = method.Module.Import(exceptionCtor);

			var ilProcessor = method.Body.GetILProcessor();
			ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, constructorReference));
			ilProcessor.Append(ilProcessor.Create(OpCodes.Throw));
		}
	}

	static class CollectionUtility
	{
		public static void RemoveAll<T>(this ICollection<T> collection, Func<T, bool> condition)
		{
			var toRemove = collection.Where(condition).ToList();
			foreach (var x in toRemove)
				collection.Remove(x);
		}
	}
}
