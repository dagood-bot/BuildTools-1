﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using NuGet;
using NuGetPackageVerifier.Logging;

namespace NuGetPackageVerifier.Rules
{
    public class AssemblyHasVersionAttributesRule : IPackageVerifierRule
    {
        public IEnumerable<PackageVerifierIssue> Validate(
            IPackageRepository packageRepo,
            IPackage package,
            IPackageVerifierLogger logger)
        {
            foreach (var currentFile in package.GetFiles())
            {
                var extension = Path.GetExtension(currentFile.Path);
                if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var assemblyPath = Path.ChangeExtension(
                        Path.Combine(Path.GetTempPath(), Path.GetTempFileName()), extension);

                    try
                    {
                        using (var packageFileStream = currentFile.GetStream())
                        using (var fileStream = new FileStream(assemblyPath, FileMode.Create))
                        {
                            packageFileStream.CopyTo(fileStream);
                        }

                        if (AssemblyHelpers.IsAssemblyManaged(assemblyPath))
                        {
                            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);

                            var asmAttrs = assemblyDefinition.CustomAttributes;

                            if (!HasAttrWithArg(asmAttrs, typeof(AssemblyFileVersionAttribute).FullName))
                            {
                                yield return PackageIssueFactory.AssemblyMissingFileVersionAttribute(currentFile.Path);
                            }

                            if (!HasAttrWithArg(asmAttrs, typeof(AssemblyInformationalVersionAttribute).FullName))
                            {
                                yield return PackageIssueFactory.AssemblyMissingInformationalVersionAttribute(currentFile.Path);
                            }
                        }
                    }
                    finally
                    {
                        if (File.Exists(assemblyPath))
                        {
                            File.Delete(assemblyPath);
                        }
                    }
                }
            }
            yield break;
        }

        private static bool HasAttrWithArg(Mono.Collections.Generic.Collection<CustomAttribute> asmAttrs, string attrTypeName)
        {
            var foundAttr = asmAttrs.SingleOrDefault(attr => attr.AttributeType.FullName == attrTypeName);
            if (foundAttr == null)
            {
                return false;
            }
            var foundAttrArg = foundAttr.ConstructorArguments.SingleOrDefault();
            var attrValue = foundAttrArg.Value as string;

            return !string.IsNullOrEmpty(attrValue);
        }
    }
}
